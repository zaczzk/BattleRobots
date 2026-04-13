using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match advisor panel that ranks all four <see cref="DamageType"/>s by their
    /// combined effectiveness ratio against the currently-selected opponent, surfacing
    /// the best damage types to use in a ranked label list.
    ///
    /// ── Computation ─────────────────────────────────────────────────────────────
    ///   For each DamageType:
    ///     combinedRatio = (1 − resistance) × vulnerabilityMultiplier
    ///   Both resistance and vulnerability default to neutral values (0 and 1
    ///   respectively) when their configs are null or no opponent is selected.
    ///   Types are sorted descending by combinedRatio via a zero-allocation
    ///   insertion sort over 4 fixed elements.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero-alloc after Awake).
    ///   OnEnable  → subscribes _onOpponentChanged → Refresh; calls Refresh once.
    ///   OnDisable → unsubscribes.
    ///   Refresh   → computes ratios, sorts descending, updates _rankingLabels
    ///               (up to _maxRankings rows, excess labels cleared to empty string).
    ///
    /// ── Label format ─────────────────────────────────────────────────────────────
    ///   With _effectivenessConfig:  "{rank}. {DamageType}: {OutcomeLabel}"
    ///   Without _effectivenessConfig: "{rank}. {DamageType}"
    ///
    /// ── Null-safety ─────────────────────────────────────────────────────────────
    ///   All inspector fields are optional.  Missing configs default to neutral.
    ///   Null Text slots in _rankingLabels are skipped without error.
    ///
    /// ── Architecture rules ───────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All referenced configs are Core SOs — safe cross-assembly reference.
    ///   • DisallowMultipleComponent — one advisor panel per canvas.
    ///   • No allocations inside Refresh() — fixed-size reuse buffer + insertion sort.
    ///
    /// Assign <c>_selectedOpponent</c>, <c>_effectivenessConfig</c>, and
    /// <c>_rankingLabels</c> in the Inspector; optionally wire <c>_onOpponentChanged</c>
    /// to the same VoidGameEvent raised by OpponentSelectionController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BestDamageTypeAdvisorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO carrying the currently-selected opponent profile. " +
                 "The profile's OpponentResistance and OpponentVulnerability configs are read. " +
                 "Leave null to rank against a neutral opponent (all types are equivalent).")]
        [SerializeField] private SelectedOpponentSO _selectedOpponent;

        [Header("Config")]
        [Tooltip("Thresholds and labels for classifying each type's outcome as " +
                 "Effective / Resisted / Neutral and choosing its display color. " +
                 "Leave null to show only the type name in each label.")]
        [SerializeField] private DamageTypeEffectivenessConfig _effectivenessConfig;

        [Header("Event Channels — In")]
        [Tooltip("Raised when the opponent selection changes. Triggers Refresh(). " +
                 "Leave null to refresh only at OnEnable time.")]
        [SerializeField] private VoidGameEvent _onOpponentChanged;

        [Header("UI — Ranked Labels (optional, up to 4)")]
        [Tooltip("Text labels for ranked matchup rows. Index 0 = best type. " +
                 "Labels beyond _maxRankings are cleared to empty string on each Refresh.")]
        [SerializeField] private Text[] _rankingLabels = Array.Empty<Text>();

        [Tooltip("Maximum number of ranking rows to populate per Refresh. " +
                 "Clamped at runtime to the _rankingLabels array length and 4 (total DamageType count).")]
        [SerializeField, Range(1, 4)] private int _maxRankings = 4;

        // ── Private state ─────────────────────────────────────────────────────

        // Pre-allocated sort buffer — avoids per-Refresh heap allocation.
        private readonly (DamageType type, float ratio)[] _sorted =
            new (DamageType, float)[4];

        private static readonly DamageType[] AllTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onOpponentChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onOpponentChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Recomputes per-type effectiveness ratios against the selected opponent,
        /// sorts them descending, and updates up to <see cref="_maxRankings"/> label rows.
        ///
        /// Safe to call at any time (e.g. from an Inspector button or opponent-changed event).
        /// Zero allocation — operates on the pre-allocated <c>_sorted</c> buffer.
        /// </summary>
        public void Refresh()
        {
            OpponentProfileSO profile = (_selectedOpponent != null &&
                                         _selectedOpponent.HasSelection)
                ? _selectedOpponent.Current
                : null;

            DamageResistanceConfig    resistCfg = profile?.OpponentResistance;
            DamageVulnerabilityConfig vulnCfg   = profile?.OpponentVulnerability;

            // Populate sort buffer with (type, combinedRatio) for all four types.
            for (int i = 0; i < AllTypes.Length; i++)
            {
                DamageType t     = AllTypes[i];
                float resistance = resistCfg != null ? resistCfg.GetResistance(t) : 0f;
                float vuln       = vulnCfg   != null ? vulnCfg.GetMultiplier(t)   : 1f;
                _sorted[i]       = (t, (1f - resistance) * vuln);
            }

            // Insertion sort descending by ratio — 4 elements, zero-alloc, stable.
            for (int i = 1; i < 4; i++)
            {
                var key = _sorted[i];
                int j   = i - 1;
                while (j >= 0 && _sorted[j].ratio < key.ratio)
                {
                    _sorted[j + 1] = _sorted[j];
                    j--;
                }
                _sorted[j + 1] = key;
            }

            if (_rankingLabels == null) return;

            int count = Mathf.Min(_maxRankings, Mathf.Min(_rankingLabels.Length, 4));

            for (int i = 0; i < _rankingLabels.Length; i++)
            {
                Text label = _rankingLabels[i];
                if (label == null) continue;

                if (i >= count)
                {
                    label.text = string.Empty;
                    continue;
                }

                DamageType t     = _sorted[i].type;
                float      ratio = _sorted[i].ratio;

                if (_effectivenessConfig != null)
                {
                    EffectivenessOutcome outcome = _effectivenessConfig.GetOutcome(ratio);
                    label.text  = $"{i + 1}. {t}: {_effectivenessConfig.GetLabel(outcome)}";
                    label.color = _effectivenessConfig.GetColor(outcome);
                }
                else
                {
                    label.text = $"{i + 1}. {t}";
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned SelectedOpponentSO. May be null.</summary>
        public SelectedOpponentSO SelectedOpponent => _selectedOpponent;

        /// <summary>The currently assigned DamageTypeEffectivenessConfig. May be null.</summary>
        public DamageTypeEffectivenessConfig EffectivenessConfig => _effectivenessConfig;

        /// <summary>Maximum ranking rows populated by each <see cref="Refresh"/> call.</summary>
        public int MaxRankings => _maxRankings;
    }
}

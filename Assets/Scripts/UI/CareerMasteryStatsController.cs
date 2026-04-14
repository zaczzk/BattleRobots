using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Career screen panel that displays per-type mastery statistics from
    /// <see cref="DamageTypeMasterySO"/>: cumulative damage accumulation labels,
    /// mastery progress bars, mastery badge GameObjects, and an optional total bonus
    /// multiplier sourced from <see cref="MasteryBonusCatalogSO"/>.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────
    ///   For each <see cref="DamageType"/> (Physical / Energy / Thermal / Shock):
    ///   • Accum label  — current accumulation formatted as an integer (e.g. "1 500").
    ///   • Progress bar — <c>Slider.value</c> set to <c>GetProgress(type)</c> ∈ [0, 1].
    ///   • Badge        — activated when the type is mastered; hidden otherwise.
    ///   Total bonus label shows "x{total:F2}" from <see cref="MasteryBonusCatalogSO.GetTotalMultiplier"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onMasteryUnlocked + _onMatchEnded → Refresh; calls Refresh().
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh() → reads DamageTypeMasterySO per-type state; updates all wired UI.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one career stats panel per canvas.
    ///   - All inspector fields are optional; unassigned refs are silently skipped.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _mastery               → DamageTypeMasterySO.
    ///   _catalog               → MasteryBonusCatalogSO (optional, for total bonus label).
    ///   _onMasteryUnlocked     → VoidGameEvent raised by DamageTypeMasterySO.
    ///   _onMatchEnded          → VoidGameEvent raised by MatchManager.
    ///   _physical/Energy/Thermal/ShockAccumLabel  → Text showing accumulation.
    ///   _physical/Energy/Thermal/ShockProgressBar → Slider showing progress [0,1].
    ///   _physical/Energy/Thermal/ShockBadge       → GameObject shown when mastered.
    ///   _totalBonusLabel       → Text showing combined catalog multiplier.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CareerMasteryStatsController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime mastery SO. Provides per-type accumulators, progress, and flags.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        [Tooltip("Optional catalog of mastery-gated bonuses. Used for total bonus label.")]
        [SerializeField] private MasteryBonusCatalogSO _catalog;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised by DamageTypeMasterySO when any type first reaches mastery. " +
                 "Triggers Refresh(). Leave null to disable auto-refresh on mastery unlock.")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        [Tooltip("Raised by MatchManager when a match ends. " +
                 "Triggers Refresh() to update accumulation stats. Leave null if not needed.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — Accum Labels (optional) ───────────────────────────────

        [Header("Accumulation Labels (optional)")]
        [SerializeField] private Text _physicalAccumLabel;
        [SerializeField] private Text _energyAccumLabel;
        [SerializeField] private Text _thermalAccumLabel;
        [SerializeField] private Text _shockAccumLabel;

        // ── Inspector — Progress Bars (optional) ──────────────────────────────

        [Header("Progress Bars (optional)")]
        [SerializeField] private Slider _physicalProgressBar;
        [SerializeField] private Slider _energyProgressBar;
        [SerializeField] private Slider _thermalProgressBar;
        [SerializeField] private Slider _shockProgressBar;

        // ── Inspector — Mastery Badges (optional) ─────────────────────────────

        [Header("Mastery Badges (optional)")]
        [SerializeField] private GameObject _physicalBadge;
        [SerializeField] private GameObject _energyBadge;
        [SerializeField] private GameObject _thermalBadge;
        [SerializeField] private GameObject _shockBadge;

        // ── Inspector — Total Bonus (optional) ────────────────────────────────

        [Header("Total Bonus Label (optional)")]
        [Tooltip("Text showing combined active multiplier from MasteryBonusCatalogSO " +
                 "(e.g. 'x1.50'). Only populated when _catalog is assigned.")]
        [SerializeField] private Text _totalBonusLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMasteryUnlocked?.RegisterCallback(_refreshDelegate);
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMasteryUnlocked?.UnregisterCallback(_refreshDelegate);
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads per-type state from <see cref="_mastery"/> and updates all wired
        /// accum labels, progress bars, and mastery badges. Also updates the total
        /// bonus label when <see cref="_catalog"/> is assigned.
        /// No-op when <see cref="_mastery"/> is null.
        /// </summary>
        public void Refresh()
        {
            if (_mastery == null) return;

            RefreshType(DamageType.Physical, _physicalAccumLabel, _physicalProgressBar, _physicalBadge);
            RefreshType(DamageType.Energy,   _energyAccumLabel,   _energyProgressBar,   _energyBadge);
            RefreshType(DamageType.Thermal,  _thermalAccumLabel,  _thermalProgressBar,  _thermalBadge);
            RefreshType(DamageType.Shock,    _shockAccumLabel,    _shockProgressBar,    _shockBadge);

            if (_totalBonusLabel != null && _catalog != null)
            {
                float total = _catalog.GetTotalMultiplier(_mastery);
                _totalBonusLabel.text = $"x{total:F2}";
            }
        }

        private void RefreshType(
            DamageType type,
            Text       accumLabel,
            Slider     progressBar,
            GameObject badge)
        {
            float accum    = _mastery.GetAccumulation(type);
            float progress = _mastery.GetProgress(type);
            bool  mastered = _mastery.IsTypeMastered(type);

            if (accumLabel != null)
                accumLabel.text = Mathf.RoundToInt(accum).ToString();

            if (progressBar != null)
                progressBar.value = progress;

            badge?.SetActive(mastered);
        }

        /// <summary>The assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;

        /// <summary>The assigned <see cref="MasteryBonusCatalogSO"/>. May be null.</summary>
        public MasteryBonusCatalogSO Catalog => _catalog;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match panel that scores the player's equipped loadout by averaging the
    /// upgrade tier of each part and mapping that average to a letter grade
    /// (S / A / B / C / D) via <see cref="BuildGradeConfig"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onMatchEnded → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → ComputeAverageTier → GetGrade / GetAdvice → update labels.
    ///
    /// ── ComputeAverageTier ────────────────────────────────────────────────────────
    ///   Iterates PlayerLoadout.EquippedPartIds and sums
    ///   PlayerPartUpgrades.GetTier(id) for each part; divides by count.
    ///   Returns 0 when either SO is null or the loadout is empty.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one grade panel per canvas.
    ///   • All UI fields are optional.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _playerUpgrades → shared PlayerPartUpgrades SO.
    ///   _playerLoadout  → shared PlayerLoadout SO.
    ///   _gradeConfig    → BuildGradeConfig SO with grade thresholds.
    ///   _onMatchEnded   → VoidGameEvent raised when a match ends.
    ///   _gradeLabel     → Text showing letter grade ("S" … "D").
    ///   _adviceLabel    → Text showing the advice string.
    ///   _gradePanel     → Root panel; activated on Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildGradeController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Runtime SO tracking part upgrade tiers.")]
        [SerializeField] private PlayerPartUpgrades _playerUpgrades;

        [Tooltip("Runtime SO holding the currently equipped part IDs.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Config SO mapping average tier to letter grade and advice.")]
        [SerializeField] private BuildGradeConfig _gradeConfig;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised when the match ends. Triggers a Refresh.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Text showing the letter grade (S / A / B / C / D).")]
        [SerializeField] private Text _gradeLabel;

        [Tooltip("Text showing the advice string for this grade.")]
        [SerializeField] private Text _adviceLabel;

        [Tooltip("Root panel. Activated by Refresh.")]
        [SerializeField] private GameObject _gradePanel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Averages the upgrade tier across all equipped parts.
        /// Returns 0 when <see cref="_playerLoadout"/> or
        /// <see cref="_playerUpgrades"/> is null, or the loadout is empty.
        /// Zero alloc.
        /// </summary>
        public float ComputeAverageTier()
        {
            if (_playerLoadout == null || _playerUpgrades == null) return 0f;

            var ids = _playerLoadout.EquippedPartIds;
            if (ids == null || ids.Count == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < ids.Count; i++)
                sum += _playerUpgrades.GetTier(ids[i]);

            return sum / ids.Count;
        }

        /// <summary>
        /// Computes the average tier, derives grade and advice, updates labels,
        /// and activates the panel.  Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_gradePanel == null) return;

            float  avgTier = ComputeAverageTier();
            string grade   = _gradeConfig != null ? _gradeConfig.GetGrade(avgTier) : "—";
            string advice  = _gradeConfig != null ? _gradeConfig.GetAdvice(grade)  : string.Empty;

            if (_gradeLabel  != null) _gradeLabel.text  = grade;
            if (_adviceLabel != null) _adviceLabel.text = advice;

            _gradePanel.SetActive(true);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PlayerPartUpgrades"/> SO. May be null.</summary>
        public PlayerPartUpgrades PlayerUpgrades => _playerUpgrades;

        /// <summary>The assigned <see cref="PlayerLoadout"/> SO. May be null.</summary>
        public PlayerLoadout PlayerLoadout => _playerLoadout;

        /// <summary>The assigned <see cref="BuildGradeConfig"/>. May be null.</summary>
        public BuildGradeConfig GradeConfig => _gradeConfig;
    }
}

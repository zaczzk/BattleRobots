using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays per-damage-type milestone progress sourced from
    /// <see cref="MasteryProgressMilestoneSO"/> and <see cref="DamageTypeMasterySO"/>.
    ///
    /// ── Row layout convention ─────────────────────────────────────────────────
    ///   For each type, one optional Text label and one optional Slider progress bar.
    ///
    ///   Label format:
    ///   • When a next milestone exists:  "{clearedCount}/{total} | Next: {nextMilestone:F0}"
    ///   • When all milestones cleared:   "{clearedCount}/{total} DONE"
    ///   • When no milestones configured: "" (empty)
    ///
    ///   Bar value: <see cref="MasteryProgressMilestoneSO.GetProgress"/> — 0 to 1.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onMasteryUnlocked + _onMatchEnded → Refresh(); Refresh().
    ///   OnDisable → unsubscribes both.
    ///   Refresh() → reads DamageTypeMasterySO.GetAccumulation per type; null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one milestone panel per canvas.
    ///   • All UI fields are optional.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _milestoneSO         → MasteryProgressMilestoneSO asset.
    ///   _mastery             → shared DamageTypeMasterySO.
    ///   _onMasteryUnlocked   → VoidGameEvent from DamageTypeMasterySO._onMasteryUnlocked.
    ///   _onMatchEnded        → VoidGameEvent from MatchManager.
    ///   _physical/energy/thermal/shockMilestoneLabel → Text per type.
    ///   _physical/energy/thermal/shockProgressBar    → Slider per type.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MasteryProgressMilestoneController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Milestone threshold config SO. Leave null to skip refresh.")]
        [SerializeField] private MasteryProgressMilestoneSO _milestoneSO;

        [Tooltip("Runtime mastery SO. Provides per-type accumulation. Leave null to skip refresh.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised when a damage type is first mastered. Triggers Refresh().")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        [Tooltip("Raised at match end. Triggers Refresh() to reflect updated accumulations.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI (optional) ─────────────────────────────────────────

        [Header("Physical")]
        [SerializeField] private Text   _physicalMilestoneLabel;
        [SerializeField] private Slider _physicalProgressBar;

        [Header("Energy")]
        [SerializeField] private Text   _energyMilestoneLabel;
        [SerializeField] private Slider _energyProgressBar;

        [Header("Thermal")]
        [SerializeField] private Text   _thermalMilestoneLabel;
        [SerializeField] private Slider _thermalProgressBar;

        [Header("Shock")]
        [SerializeField] private Text   _shockMilestoneLabel;
        [SerializeField] private Slider _shockProgressBar;

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

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes all four type milestone labels and progress bars from the live
        /// <see cref="_mastery"/> accumulation and <see cref="_milestoneSO"/> thresholds.
        /// Silently returns when either data SO is null. Fully null-safe on UI refs.
        /// </summary>
        public void Refresh()
        {
            if (_mastery == null || _milestoneSO == null) return;

            RefreshType(DamageType.Physical, _physicalMilestoneLabel, _physicalProgressBar);
            RefreshType(DamageType.Energy,   _energyMilestoneLabel,   _energyProgressBar);
            RefreshType(DamageType.Thermal,  _thermalMilestoneLabel,  _thermalProgressBar);
            RefreshType(DamageType.Shock,    _shockMilestoneLabel,    _shockProgressBar);
        }

        private void RefreshType(DamageType type, Text label, Slider bar)
        {
            float   accum      = _mastery.GetAccumulation(type);
            float[] milestones = _milestoneSO.GetMilestonesForType(type);
            int     total      = milestones != null ? milestones.Length : 0;
            int     cleared    = _milestoneSO.GetClearedCount(type, accum);
            float   progress   = _milestoneSO.GetProgress(type, accum);

            if (label != null)
            {
                if (total == 0)
                {
                    label.text = string.Empty;
                }
                else
                {
                    float? next = _milestoneSO.GetNextMilestone(type, accum);
                    label.text = next.HasValue
                        ? $"{cleared}/{total} | Next: {next.Value:F0}"
                        : $"{cleared}/{total} DONE";
                }
            }

            if (bar != null)
                bar.value = progress;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MasteryProgressMilestoneSO"/>. May be null.</summary>
        public MasteryProgressMilestoneSO MilestoneSO => _milestoneSO;

        /// <summary>The assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;
    }
}

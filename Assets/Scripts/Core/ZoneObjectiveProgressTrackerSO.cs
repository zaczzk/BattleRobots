using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Lightweight ScriptableObject that aggregates a <see cref="ZoneObjectiveSO"/>
    /// and a <see cref="ZoneDominanceSO"/> into a single, readable progress state for
    /// the "hold N zones to complete the objective" win condition.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Assign the two SO references; no runtime mutation is required.
    ///   • Call <see cref="Refresh"/> whenever the underlying data changes
    ///     (e.g. from a <c>ZoneObjectiveProgressHUDController</c> that subscribes
    ///     to <c>_onDominanceChanged</c>); this raises <see cref="_onProgressUpdated"/>
    ///     so that multiple HUD consumers can react without polling.
    ///   • <see cref="Reset"/> is a silent no-op — no state is owned here.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All values are computed on-the-fly from the referenced SOs — zero
    ///     runtime state owned by this object.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneObjectiveProgressTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneObjectiveProgressTracker", order = 21)]
    public sealed class ZoneObjectiveProgressTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The objective that defines RequiredZones. May be null.")]
        [SerializeField] private ZoneObjectiveSO _objectiveSO;

        [Tooltip("The live zone-ownership tracker. May be null.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by Refresh(). Wire to ZoneObjectiveProgressHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onProgressUpdated;

        // ── Properties (all computed, zero allocation) ────────────────────────

        /// <summary>Number of zones currently held by the player.</summary>
        public int HeldZones => _dominanceSO?.PlayerZoneCount ?? 0;

        /// <summary>Number of zones required to complete the objective.</summary>
        public int RequiredZones => _objectiveSO?.RequiredZones ?? 0;

        /// <summary>
        /// Normalised progress in [0, 1].
        /// 0 when <see cref="RequiredZones"/> is 0 (no objective configured).
        /// </summary>
        public float ProgressRatio =>
            RequiredZones > 0 ? Mathf.Clamp01((float)HeldZones / RequiredZones) : 0f;

        /// <summary>
        /// True when the player holds at least <see cref="RequiredZones"/> zones
        /// and the objective is configured (RequiredZones > 0).
        /// </summary>
        public bool IsObjectiveMet => RequiredZones > 0 && HeldZones >= RequiredZones;

        /// <summary>The referenced objective SO (may be null).</summary>
        public ZoneObjectiveSO ObjectiveSO => _objectiveSO;

        /// <summary>The referenced dominance SO (may be null).</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Raises <see cref="_onProgressUpdated"/> so that subscribers can rebuild
        /// their UI. Does not mutate any state.
        /// Null-safe; no-op when the channel is not assigned.
        /// Zero allocation.
        /// </summary>
        public void Refresh() => _onProgressUpdated?.Raise();

        /// <summary>
        /// Silent no-op — this SO owns no runtime state.
        /// Provided for API symmetry with other tracker SOs.
        /// </summary>
        public void Reset() { }
    }
}

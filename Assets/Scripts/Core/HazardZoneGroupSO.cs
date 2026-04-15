using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that represents a logical group of arena hazard zones.
    /// Activating or deactivating the group fires the corresponding event channel so that
    /// <see cref="BattleRobots.Physics.HazardZoneGroupController"/> can toggle all
    /// <see cref="BattleRobots.Physics.HazardZoneController"/> instances in the group
    /// atomically.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • This SO owns only state (IsGroupActive) and event channels.
    ///   • The Physics-layer controller owns the HazardZoneController[] array so that
    ///     BattleRobots.Core never references BattleRobots.Physics.
    ///   • All three mutating methods (Activate / Deactivate / Toggle) are null-safe.
    ///   • Reset() is silent — it does not fire events — so bootstrappers can call
    ///     it at match start without triggering spurious UI flashes.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ HazardZoneGroup.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/HazardZoneGroup", order = 12)]
    public sealed class HazardZoneGroupSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by Activate() when the group transitions from inactive to active.")]
        [SerializeField] private VoidGameEvent _onGroupActivated;

        [Tooltip("Raised by Deactivate() when the group transitions from active to inactive.")]
        [SerializeField] private VoidGameEvent _onGroupDeactivated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool _isGroupActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True when the group is currently active.</summary>
        public bool IsGroupActive => _isGroupActive;

        /// <summary>Event channel raised by <see cref="Activate"/>. May be null.</summary>
        public VoidGameEvent OnGroupActivated => _onGroupActivated;

        /// <summary>Event channel raised by <see cref="Deactivate"/>. May be null.</summary>
        public VoidGameEvent OnGroupDeactivated => _onGroupDeactivated;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Marks the group as active and raises <see cref="_onGroupActivated"/>.
        /// No-op guard: always sets IsGroupActive to true regardless of previous state.
        /// </summary>
        public void Activate()
        {
            _isGroupActive = true;
            _onGroupActivated?.Raise();
        }

        /// <summary>
        /// Marks the group as inactive and raises <see cref="_onGroupDeactivated"/>.
        /// No-op guard: always sets IsGroupActive to false regardless of previous state.
        /// </summary>
        public void Deactivate()
        {
            _isGroupActive = false;
            _onGroupDeactivated?.Raise();
        }

        /// <summary>
        /// Calls <see cref="Activate"/> when the group is inactive,
        /// or <see cref="Deactivate"/> when it is active.
        /// </summary>
        public void Toggle()
        {
            if (_isGroupActive)
                Deactivate();
            else
                Activate();
        }

        /// <summary>
        /// Resets runtime state to inactive without firing any event.
        /// Call at match start (or via OnEnable) to guarantee a clean baseline.
        /// </summary>
        public void Reset()
        {
            _isGroupActive = false;
        }
    }
}

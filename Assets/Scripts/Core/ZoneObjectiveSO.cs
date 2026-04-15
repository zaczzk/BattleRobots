using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject that defines a match win-by-zones objective: the player
    /// wins if they hold at least <see cref="RequiredZones"/> zones at the moment
    /// the match ends.
    ///
    /// ── State machine ─────────────────────────────────────────────────────────
    ///   Reset():
    ///     • Clears <see cref="IsComplete"/>.
    ///     • Safe to call at match start.
    ///   Evaluate(int playerZoneCount):
    ///     • No-op if already complete.
    ///     • If playerZoneCount ≥ RequiredZones → set IsComplete=true → raise
    ///       <see cref="_onObjectiveComplete"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on domain reload.
    ///   - Zero heap allocation on all hot-path methods (integer compare only).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneObjective.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneObjective", order = 19)]
    public sealed class ZoneObjectiveSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Objective Settings")]
        [Tooltip("Minimum number of zones the player must hold at match end to " +
                 "complete this objective.")]
        [SerializeField, Min(1)] private int _requiredZones = 1;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once when the player meets the zone-count requirement. " +
                 "Wire to win-flow controllers.")]
        [SerializeField] private VoidGameEvent _onObjectiveComplete;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _isComplete;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => _isComplete = false;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Zones the player must hold to complete this objective.</summary>
        public int RequiredZones => _requiredZones;

        /// <summary>True once <see cref="Evaluate"/> has found the condition met.</summary>
        public bool IsComplete => _isComplete;

        /// <summary>Event raised on objective completion. May be null.</summary>
        public VoidGameEvent OnObjectiveComplete => _onObjectiveComplete;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether <paramref name="playerZoneCount"/> meets or exceeds
        /// <see cref="RequiredZones"/>. If so, marks the objective complete and
        /// raises <see cref="_onObjectiveComplete"/> (once per reset).
        /// Zero allocation — integer compare only.
        /// </summary>
        /// <param name="playerZoneCount">Current number of zones held by the player.</param>
        public void Evaluate(int playerZoneCount)
        {
            if (_isComplete) return;

            if (playerZoneCount >= _requiredZones)
            {
                _isComplete = true;
                _onObjectiveComplete?.Raise();
            }
        }

        /// <summary>
        /// Clears <see cref="IsComplete"/>.
        /// Does NOT fire any events — safe to call at match start.
        /// </summary>
        public void Reset()
        {
            _isComplete = false;
        }
    }
}

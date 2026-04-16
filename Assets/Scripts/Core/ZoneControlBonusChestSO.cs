using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks bonus-chest milestones for zone-control
    /// matches.
    ///
    /// A bonus chest is "spawned" (i.e. <see cref="_onChestSpawned"/> fires) every
    /// <see cref="CaptureInterval"/> zone captures by the player.  Call
    /// <see cref="CheckChest"/> with the cumulative player capture count after each
    /// capture; the SO fires when a new milestone is crossed.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on CheckChest.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlBonusChest.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlBonusChest", order = 43)]
    public sealed class ZoneControlBonusChestSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Chest Settings")]
        [Tooltip("A bonus chest is spawned every N player zone captures.")]
        [Min(1)]
        [SerializeField] private int _captureInterval = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time the player crosses a capture-interval milestone.")]
        [SerializeField] private VoidGameEvent _onChestSpawned;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _totalChests;
        private int _lastMilestone;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total bonus chests spawned since the last Reset.</summary>
        public int TotalChests => _totalChests;

        /// <summary>Zone-capture count between consecutive bonus chests.</summary>
        public int CaptureInterval => _captureInterval;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether a new chest milestone has been crossed.
        /// Fires <see cref="_onChestSpawned"/> and increments <see cref="TotalChests"/>
        /// for each newly crossed interval milestone.
        /// Non-positive <paramref name="captures"/> values are ignored.
        /// </summary>
        /// <param name="captures">Cumulative player zone-capture count this match.</param>
        public void CheckChest(int captures)
        {
            if (captures <= 0 || _captureInterval <= 0) return;

            int currentMilestone = captures / _captureInterval;
            while (_lastMilestone < currentMilestone)
            {
                _lastMilestone++;
                _totalChests++;
                _onChestSpawned?.Raise();
            }
        }

        /// <summary>
        /// Resets all runtime state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalChests   = 0;
            _lastMilestone = 0;
        }
    }
}

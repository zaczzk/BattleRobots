using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks how many arena control zones the player
    /// currently holds and derives a dominance ratio and majority flag.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Wire each <c>ControlZoneSO._onCaptured</c> (player-side zones only)
    ///     → <see cref="AddPlayerZone"/> via a <c>ZoneDominanceController</c>.
    ///   • Wire each <c>ControlZoneSO._onLost</c> → <see cref="RemovePlayerZone"/>.
    ///   • <see cref="ZoneDominanceHUDController"/> subscribes to
    ///     <see cref="_onDominanceChanged"/> to refresh the HUD reactively.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path methods (integer arithmetic only).
    ///   - Runtime state is not serialised — zone count resets on domain reload.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneDominance.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneDominance", order = 18)]
    public sealed class ZoneDominanceSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Settings")]
        [Tooltip("Total number of capturable zones in the arena. " +
                 "Used to compute DominanceRatio and HasDominance.")]
        [SerializeField, Min(1)] private int _totalZones = 3;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by AddPlayerZone, RemovePlayerZone, and Reset. " +
                 "Wire to ZoneDominanceHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _playerZoneCount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => _playerZoneCount = 0;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of zones currently held by the player.</summary>
        public int PlayerZoneCount => _playerZoneCount;

        /// <summary>Total zones configured in the arena (serialised).</summary>
        public int TotalZones => _totalZones;

        /// <summary>
        /// Normalised dominance ratio in [0, 1].
        /// Suitable for Slider.value.
        /// </summary>
        public float DominanceRatio =>
            _totalZones > 0 ? Mathf.Clamp01((float)_playerZoneCount / _totalZones) : 0f;

        /// <summary>
        /// True when the player holds a strict majority of zones
        /// (PlayerZoneCount × 2 &gt; TotalZones).
        /// </summary>
        public bool HasDominance => _playerZoneCount * 2 > _totalZones;

        /// <summary>Event raised whenever zone count changes. May be null.</summary>
        public VoidGameEvent OnDominanceChanged => _onDominanceChanged;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the player zone count by one (clamped to <see cref="TotalZones"/>).
        /// Fires <see cref="_onDominanceChanged"/>.
        /// Zero allocation.
        /// </summary>
        public void AddPlayerZone()
        {
            _playerZoneCount = Mathf.Min(_playerZoneCount + 1, _totalZones);
            _onDominanceChanged?.Raise();
        }

        /// <summary>
        /// Decrements the player zone count by one (clamped to 0).
        /// Fires <see cref="_onDominanceChanged"/>.
        /// Zero allocation.
        /// </summary>
        public void RemovePlayerZone()
        {
            _playerZoneCount = Mathf.Max(0, _playerZoneCount - 1);
            _onDominanceChanged?.Raise();
        }

        /// <summary>
        /// Zeros the player zone count and fires <see cref="_onDominanceChanged"/>.
        /// Call at match start or from a match-ended handler.
        /// </summary>
        public void Reset()
        {
            _playerZoneCount = 0;
            _onDominanceChanged?.Raise();
        }
    }
}

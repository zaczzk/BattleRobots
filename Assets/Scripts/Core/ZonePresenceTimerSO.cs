using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that accumulates the total seconds each arena zone
    /// has been held by the player during the current match.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • A <see cref="ZonePresenceTimerController"/> calls
    ///     <see cref="AddPresenceTime"/> every frame for each captured zone.
    ///   • <see cref="ZonePresenceTimerHUDController"/> subscribes
    ///     <see cref="_onPresenceUpdated"/> to refresh the per-zone time panel.
    ///   • Call <see cref="Reset"/> at match start to wipe the previous match's data.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Internal array pre-allocated in Reset(); zero alloc on hot-path.
    ///   - Runtime state is not serialised — times reset on domain reload.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZonePresenceTimer.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZonePresenceTimer", order = 21)]
    public sealed class ZonePresenceTimerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Settings")]
        [Tooltip("Number of zones tracked. Must match the number of ControlZoneSOs " +
                 "managed by the companion ZonePresenceTimerController.")]
        [SerializeField, Min(1)] private int _maxZones = 3;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time AddPresenceTime writes a non-zero delta. " +
                 "Wire to ZonePresenceTimerHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onPresenceUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float[] _presenceTimes;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of zones tracked (serialised inspector value).</summary>
        public int MaxZones => _maxZones;

        /// <summary>Event raised on each <see cref="AddPresenceTime"/> call. May be null.</summary>
        public VoidGameEvent OnPresenceUpdated => _onPresenceUpdated;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="dt"/> seconds to zone <paramref name="zoneIndex"/>'s
        /// presence accumulator and fires <see cref="_onPresenceUpdated"/>.
        /// No-op when <paramref name="zoneIndex"/> is out of range or <paramref name="dt"/> ≤ 0.
        /// Zero allocation on the hot path.
        /// </summary>
        public void AddPresenceTime(int zoneIndex, float dt)
        {
            if (_presenceTimes == null) Reset();
            if (zoneIndex < 0 || zoneIndex >= _presenceTimes.Length) return;
            if (dt <= 0f) return;

            _presenceTimes[zoneIndex] += dt;
            _onPresenceUpdated?.Raise();
        }

        /// <summary>
        /// Returns the total presence time in seconds for zone <paramref name="zoneIndex"/>.
        /// Returns 0 when the index is out of range.
        /// Zero allocation.
        /// </summary>
        public float GetPresenceTime(int zoneIndex)
        {
            if (_presenceTimes == null || zoneIndex < 0 || zoneIndex >= _presenceTimes.Length)
                return 0f;

            return _presenceTimes[zoneIndex];
        }

        /// <summary>
        /// Silently zeros all presence-time accumulators.
        /// Does NOT fire <see cref="_onPresenceUpdated"/> — safe to call at match start.
        /// </summary>
        public void Reset()
        {
            if (_presenceTimes == null || _presenceTimes.Length != _maxZones)
                _presenceTimes = new float[_maxZones];
            else
                System.Array.Clear(_presenceTimes, 0, _presenceTimes.Length);
        }
    }
}

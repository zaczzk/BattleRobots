using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that stores the current network round-trip latency.
    ///
    /// Lifecycle:
    ///   • <see cref="PingMonitor"/> calls <see cref="SetPing"/> on a configurable
    ///     polling interval (not every frame) to update the value.
    ///   • Fires <c>_onPingChanged</c> (<see cref="IntGameEvent"/>) whenever the value
    ///     changes so UI and other listeners update without polling.
    ///   • Call <see cref="Reset"/> when disconnecting to zero-out the display.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Network ▶ PingSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/PingSO", order = 0)]
    public sealed class PingSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel")]
        [Tooltip("Fired whenever CurrentPingMs changes. Payload = new ping value in ms.")]
        [SerializeField] private IntGameEvent _onPingChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Most recent round-trip latency in milliseconds. 0 = unknown / offline.</summary>
        public int CurrentPingMs { get; private set; }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Update the ping value and fire <c>_onPingChanged</c>.
        /// Values below 0 are clamped to 0.
        /// </summary>
        public void SetPing(int pingMs)
        {
            int clamped = pingMs < 0 ? 0 : pingMs;
            CurrentPingMs = clamped;
            _onPingChanged?.Raise(clamped);
        }

        /// <summary>
        /// Resets the ping to 0 and fires the event.
        /// Call when the player disconnects so the display shows "—" or "0 ms".
        /// </summary>
        public void Reset()
        {
            CurrentPingMs = 0;
            _onPingChanged?.Raise(0);
        }
    }
}

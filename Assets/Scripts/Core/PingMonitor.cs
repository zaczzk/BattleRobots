using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that periodically polls the active <see cref="INetworkAdapter"/>
    /// for round-trip latency and writes the result to a <see cref="PingSO"/>.
    ///
    /// Polling is done on a configurable interval (default 2 s) rather than every
    /// frame to avoid wasting bandwidth on unnecessary ping requests.
    ///
    /// The only per-frame work is a float timer increment — no heap allocations.
    ///
    /// Wiring in the Inspector:
    ///   □ _pingSO            → PingSO asset
    ///   □ _networkBridge     → NetworkEventBridge (the bridge exposes the adapter)
    ///   □ _pollIntervalSeconds → polling frequency (default 2.0)
    ///
    /// BattleRobots.Core namespace — no UI or Physics references.
    /// </summary>
    public sealed class PingMonitor : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("PingSO asset updated with the latest ping value on each poll.")]
        [SerializeField] private PingSO _pingSO;

        [Header("Network")]
        [Tooltip("Bridge that holds the active INetworkAdapter (provides GetPingMs).")]
        [SerializeField] private NetworkEventBridge _networkBridge;

        [Header("Polling")]
        [Tooltip("How often (in seconds) to sample ping from the adapter.")]
        [SerializeField, Min(0.1f)] private float _pollIntervalSeconds = 2f;

        // ── Runtime state (no per-frame allocs) ───────────────────────────────

        private float _timeSinceLastPoll;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            // Poll immediately on enable so the display is populated straight away.
            _timeSinceLastPoll = _pollIntervalSeconds;
        }

        private void Update()
        {
            _timeSinceLastPoll += Time.deltaTime;

            if (_timeSinceLastPoll < _pollIntervalSeconds) return;

            _timeSinceLastPoll = 0f;
            PollPing();
        }

        private void OnDisable()
        {
            // Zero out the display when monitoring stops (e.g. disconnected).
            _pingSO?.Reset();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void PollPing()
        {
            if (_pingSO == null)
            {
                Debug.LogWarning("[PingMonitor] PingSO is not assigned.", this);
                return;
            }

            int pingMs = _networkBridge != null
                ? _networkBridge.GetAdapterPingMs()
                : 0;

            _pingSO.SetPing(pingMs);
        }
    }
}

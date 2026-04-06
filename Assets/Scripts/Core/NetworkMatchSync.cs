using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Drives two-way match-state synchronisation between local game state and remote peers.
    ///
    /// Responsibilities:
    ///   1. <b>Send</b>: every <see cref="_syncIntervalTicks"/> FixedUpdate ticks, read live
    ///      health and elapsed-time values from the Inspector-assigned sources, write them into
    ///      <see cref="_matchStateSO"/> via <see cref="MatchStateSO.SetLocalState"/>, serialise
    ///      them with <see cref="MatchStateSO.Snapshot"/> into a pre-allocated buffer, then
    ///      dispatch via <see cref="NetworkEventBridge.SendMatchState"/>.
    ///   2. <b>Receive</b>: subscribe to <see cref="NetworkEventBridge.OnMatchStateReceived"/>
    ///      and forward incoming payloads to <see cref="MatchStateSO.Apply"/>.
    ///
    /// Setup (Inspector):
    ///   • <c>_matchStateSO</c>    — shared MatchStateSO SO asset.
    ///   • <c>_bridge</c>          — NetworkEventBridge MB in the arena scene.
    ///   • <c>_playerHealth</c>    — HealthSO for the local player robot.
    ///   • <c>_opponentHealth</c>  — HealthSO for the AI / remote opponent robot.
    ///   • <c>_matchManager</c>    — optional MatchManager reference for ElapsedSeconds;
    ///                               if null, elapsed time is not written (sent as 0).
    ///   • <c>_syncIntervalTicks</c> — how many FixedUpdate ticks between sends (default 10
    ///                               ≈ 5 Hz at 50 Hz physics).
    ///
    /// Architecture rules:
    ///   • BattleRobots.Core namespace — no Physics / UI imports.
    ///   • <see cref="_sendBuffer"/> is allocated once in <see cref="Awake"/> — zero heap
    ///     allocation in <see cref="FixedUpdate"/>.
    ///   • Event subscription is O(1) C# delegate; no LINQ or boxing in hot path.
    /// </summary>
    public sealed class NetworkMatchSync : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Shared State")]
        [Tooltip("MatchStateSO that acts as the data bus for remote match state.")]
        [SerializeField] private MatchStateSO _matchStateSO;

        [Header("Network")]
        [Tooltip("NetworkEventBridge in the current scene. Provides SendMatchState + received callback.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Header("Live Data Sources")]
        [Tooltip("HealthSO for the local player robot. Read each sync tick.")]
        [SerializeField] private HealthSO _playerHealth;

        [Tooltip("HealthSO for the opponent robot. Read each sync tick.")]
        [SerializeField] private HealthSO _opponentHealth;

        [Tooltip("Optional MatchManager reference used to read ElapsedSeconds. " +
                 "Leave empty if MatchManager is not present; elapsed time will be sent as 0.")]
        [SerializeField] private MatchManager _matchManager;

        [Header("Sync Settings")]
        [Tooltip("Number of FixedUpdate ticks between outgoing state sends. " +
                 "10 ≈ 5 Hz at the default 50 Hz physics rate.")]
        [SerializeField, Min(1)] private int _syncIntervalTicks = 10;

        // ── Runtime ───────────────────────────────────────────────────────────

        private byte[] _sendBuffer;           // pre-allocated; zero alloc in FixedUpdate
        private int    _tickCounter;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _sendBuffer = new byte[MatchStateSO.PayloadSize];
        }

        private void OnEnable()
        {
            if (_bridge != null)
                _bridge.OnMatchStateReceived += HandleMatchStateReceived;
        }

        private void OnDisable()
        {
            if (_bridge != null)
                _bridge.OnMatchStateReceived -= HandleMatchStateReceived;
        }

        // ── FixedUpdate send loop ─────────────────────────────────────────────

        private void FixedUpdate()
        {
            _tickCounter++;
            if (_tickCounter < _syncIntervalTicks) return;
            _tickCounter = 0;

            SendLocalState();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Apply an incoming match-state payload to the shared <see cref="MatchStateSO"/>.
        /// Called automatically via the <see cref="NetworkEventBridge.OnMatchStateReceived"/>
        /// subscription, but also available for manual wiring via a
        /// <see cref="ByteArrayGameEventListener"/> UnityEvent in the Inspector.
        /// </summary>
        public void HandleMatchStateReceived(byte[] payload)
        {
            _matchStateSO?.Apply(payload);
        }

        /// <summary>
        /// Force an immediate state send, bypassing the tick-interval gate.
        /// Useful at match start / match end to ensure the remote peer is in sync.
        /// </summary>
        public void ForceSend()
        {
            _tickCounter = 0;
            SendLocalState();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void SendLocalState()
        {
            if (_matchStateSO == null || _bridge == null) return;

            float playerHp   = _playerHealth   != null ? _playerHealth.CurrentHp   : 0f;
            float opponentHp = _opponentHealth  != null ? _opponentHealth.CurrentHp  : 0f;
            float elapsed    = _matchManager    != null ? _matchManager.ElapsedSeconds   : 0f;

            _matchStateSO.SetLocalState(playerHp, opponentHp, elapsed);
            _matchStateSO.Snapshot(_sendBuffer);
            _bridge.SendMatchState(_sendBuffer);
        }
    }
}

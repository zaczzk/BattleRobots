using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour shim that bridges the SO event-channel system to a concrete
    /// <see cref="INetworkAdapter"/> (Photon, Mirror, or <see cref="StubNetworkAdapter"/>).
    ///
    /// Responsibilities:
    ///   1. Hold a reference to the active <see cref="NetworkSessionSO"/> and an
    ///      <see cref="INetworkAdapter"/> instance.
    ///   2. On Awake, register adapter callbacks that forward network events to
    ///      the appropriate <see cref="NetworkSessionSO"/> mutators.
    ///   3. Expose <c>public void</c> methods (<see cref="BeginConnect"/>,
    ///      <see cref="BeginHost"/>, <see cref="BeginJoin"/>,
    ///      <see cref="BeginDisconnect"/>) that UI or other MonoBehaviours can
    ///      call via Inspector UnityEvent wiring, keeping all adapter calls
    ///      out of UI code.
    ///   4. Allow the adapter to be replaced at runtime via
    ///      <see cref="SetAdapter"/> — used in tests to inject a
    ///      <see cref="StubNetworkAdapter"/>.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI must NOT reference BattleRobots.Physics.
    ///   • This bridge lives in BattleRobots.Core — safe to reference from UI.
    ///   • No per-frame heap allocations — callbacks registered once in Awake.
    ///   • NetworkLobbyUI calls public methods here via UnityEvent; it never
    ///     touches <see cref="INetworkAdapter"/> directly.
    /// </summary>
    public sealed class NetworkEventBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Session State")]
        [Tooltip("Shared NetworkSessionSO that holds the authoritative connection state. " +
                 "Assign the same asset referenced by NetworkLobbyUI.")]
        [SerializeField] private NetworkSessionSO _session;

        // ── Runtime adapter ───────────────────────────────────────────────────

        private INetworkAdapter _adapter;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Default to stub so the bridge is functional without a real backend.
            if (_adapter == null)
                _adapter = new StubNetworkAdapter();

            RegisterAdapterCallbacks(_adapter);
        }

        private void OnDestroy()
        {
            // Ensure the session is cleaned up if this GO is destroyed mid-match.
            if (_session != null && _session.IsConnected)
                _session.Disconnect();
        }

        // ── Public API — called by UI UnityEvents ─────────────────────────────

        /// <summary>
        /// Initiate a connection using the current role stored in
        /// <paramref name="role"/>.  Typically called by a "Connect" button.
        /// Transitions <see cref="NetworkSessionSO"/> to Connecting state and
        /// tells the adapter to begin the real (or stub) connection.
        /// </summary>
        public void BeginConnect(NetworkRole role)
        {
            if (_session == null)
            {
                Debug.LogError("[NetworkEventBridge] NetworkSessionSO is not assigned.", this);
                return;
            }

            _session.Connect(role);
            _adapter?.Connect();
        }

        /// <summary>
        /// Convenience overload: begin connecting as Host.
        /// Wire to a "Host" button's UnityEvent with no parameter.
        /// </summary>
        public void BeginConnectAsHost() => BeginConnect(NetworkRole.Host);

        /// <summary>
        /// Convenience overload: begin connecting as Client.
        /// Wire to a "Join" button's UnityEvent with no parameter.
        /// </summary>
        public void BeginConnectAsClient() => BeginConnect(NetworkRole.Client);

        /// <summary>
        /// Tell the adapter to create a room with <paramref name="roomCode"/>.
        /// The session must be in Connected state; call <see cref="BeginConnect"/>
        /// first and wait for the <c>onConnected</c> event before hosting.
        /// </summary>
        public void BeginHost(string roomCode)
        {
            if (_session == null) return;

            if (!_session.IsConnected)
            {
                Debug.LogWarning("[NetworkEventBridge] BeginHost called before Connected. Ignored.", this);
                return;
            }

            _adapter?.Host(roomCode);
        }

        /// <summary>
        /// Tell the adapter to join the room identified by <paramref name="roomCode"/>.
        /// The session must be in Connected state.
        /// </summary>
        public void BeginJoin(string roomCode)
        {
            if (_session == null) return;

            if (!_session.IsConnected)
            {
                Debug.LogWarning("[NetworkEventBridge] BeginJoin called before Connected. Ignored.", this);
                return;
            }

            _adapter?.Join(roomCode);
        }

        /// <summary>
        /// Disconnect from the backend and reset session state.
        /// Wire to a "Disconnect" or "Cancel" button's UnityEvent.
        /// </summary>
        public void BeginDisconnect()
        {
            _adapter?.Disconnect();
            // Session state is updated via the OnDisconnected callback registered in Awake.
        }

        // ── Adapter injection (tests / DI) ────────────────────────────────────

        /// <summary>
        /// Replace the current adapter with <paramref name="adapter"/> and
        /// re-register all callbacks.  Must be called before <see cref="BeginConnect"/>.
        ///
        /// Typical usage in tests:
        /// <code>
        /// bridge.SetAdapter(new StubNetworkAdapter());
        /// </code>
        /// </summary>
        public void SetAdapter(INetworkAdapter adapter)
        {
            if (adapter == null)
            {
                Debug.LogWarning("[NetworkEventBridge] SetAdapter called with null adapter. Ignored.", this);
                return;
            }

            _adapter = adapter;
            RegisterAdapterCallbacks(_adapter);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Wire the adapter's callback properties to forward network events
        /// to the <see cref="NetworkSessionSO"/> mutators.
        /// Called once in Awake and again when <see cref="SetAdapter"/> swaps adapters.
        /// </summary>
        private void RegisterAdapterCallbacks(INetworkAdapter adapter)
        {
            adapter.OnConnected = () =>
            {
                _session?.SetConnected();
            };

            adapter.OnDisconnected = () =>
            {
                _session?.Disconnect();
            };

            adapter.OnRoomJoined = (roomCode) =>
            {
                _session?.JoinRoom(roomCode);
            };

            adapter.OnRoomJoinFailed = (reason) =>
            {
                Debug.LogWarning($"[NetworkEventBridge] Room join failed: {reason}");
                // Stay in Connected state; UI can show the error via the session SO.
            };

            adapter.OnMatchStateReceived = (payload) =>
            {
                // Relay raw bytes via a future FloatGameEvent / byte-array channel.
                // For now log receipt — full relay wired in a future sprint.
                Debug.Log($"[NetworkEventBridge] Match-state received ({payload?.Length ?? 0} bytes).");
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_session == null)
                Debug.LogWarning("[NetworkEventBridge] NetworkSessionSO is not assigned.", this);
        }
#endif
    }
}

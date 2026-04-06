using System;
using System.Collections.Generic;
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

        [Header("Match-State Channel")]
        [Tooltip("SO event channel raised when a match-state payload is received from a remote peer. " +
                 "Wire a ByteArrayGameEventListener to forward payloads to NetworkMatchSync or MatchStateSO.")]
        [SerializeField] private ByteArrayGameEvent _onMatchStateReceivedChannel;

        [Header("Room List")]
        [Tooltip("Runtime SO that stores the current network room list. " +
                 "RequestRoomList() will push adapter results into this SO so RoomListUI updates automatically.")]
        [SerializeField] private RoomListSO _roomList;

        // ── Runtime adapter ───────────────────────────────────────────────────

        private INetworkAdapter _adapter;

        // ── Code-subscription surface (for NetworkMatchSync) ──────────────────

        /// <summary>
        /// Raised on the main thread whenever a match-state payload arrives from a
        /// remote peer. <see cref="NetworkMatchSync"/> subscribes here in OnEnable to
        /// receive payloads without requiring a sibling MonoBehaviour listener.
        /// </summary>
        public event Action<byte[]> OnMatchStateReceived;

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
        /// Broadcast a serialised match-state payload to all peers in the current room.
        /// Delegates directly to the adapter; does nothing if no adapter is set or if
        /// the payload is null.
        /// Call from <see cref="NetworkMatchSync"/> — the caller pre-allocates the buffer.
        /// </summary>
        public void SendMatchState(byte[] payload)
        {
            if (payload == null) return;
            _adapter?.SendMatchState(payload);
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

        // ── Room discovery ────────────────────────────────────────────────────

        /// <summary>
        /// Ask the adapter for the current room list. The adapter invokes its
        /// <see cref="INetworkAdapter.OnRoomListReceived"/> callback (wired in
        /// <see cref="RegisterAdapterCallbacks"/>) which calls
        /// <see cref="RoomListSO.SetRooms"/> — triggering the SO event channel so
        /// that <see cref="RoomListUI"/> rebuilds automatically.
        ///
        /// Safe to call when no adapter is set (no-op) or when <c>_roomList</c> is
        /// unassigned (adapter result will be silently discarded).
        /// </summary>
        public void RequestRoomList()
        {
            _adapter?.RequestRoomList();
        }

        // ── Diagnostics ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current round-trip latency from the active adapter in milliseconds.
        /// Returns 0 when no adapter is set or when the adapter is not connected.
        /// Allocation-free — delegates directly to <see cref="INetworkAdapter.GetPingMs"/>.
        /// </summary>
        public int GetAdapterPingMs() => _adapter != null ? _adapter.GetPingMs() : 0;

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
                // Raise the SO event channel (Inspector-wired listeners).
                _onMatchStateReceivedChannel?.Raise(payload);
                // Raise the C# event (code-registered subscribers, e.g. NetworkMatchSync).
                OnMatchStateReceived?.Invoke(payload);
            };

            adapter.OnRoomListReceived = (rooms) =>
            {
                // Push the result into the SO; this fires _onRoomsUpdated so RoomListUI rebuilds.
                _roomList?.SetRooms(rooms);
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_session == null)
                Debug.LogWarning("[NetworkEventBridge] NetworkSessionSO is not assigned.", this);
            if (_roomList == null)
                Debug.LogWarning("[NetworkEventBridge] RoomListSO is not assigned — " +
                                 "RequestRoomList() results will be discarded.", this);
        }
#endif
    }
}

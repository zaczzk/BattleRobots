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

        [Header("Failure Feedback")]
        [Tooltip("(Optional) SO event channel raised when the adapter fires OnRoomJoinFailed. " +
                 "Wire a StringGameEventListener on the JoinFailureUI GameObject to this channel " +
                 "and point its Response at JoinFailureUI.ShowFailure.")]
        [SerializeField] private StringGameEvent _onRoomJoinFailedChannel;

        [Header("Recent Rooms")]
        [Tooltip("(Optional) RecentRoomsSO to record a visit each time a room is successfully joined. " +
                 "Leave unassigned to disable recent-rooms tracking.")]
        [SerializeField] private RecentRoomsSO _recentRooms;

        [Header("Chat")]
        [Tooltip("(Optional) ChatSO ring-buffer. Incoming chat messages are forwarded here via AddMessage. " +
                 "Leave unassigned to disable chat history buffering.")]
        [SerializeField] private ChatSO _chat;

        [Tooltip("(Optional) SO event channel raised when a chat message is received from the adapter. " +
                 "Wire a StringGameEventListener on ChatUI to ChatUI.AppendMessage.")]
        [SerializeField] private StringGameEvent _onChatReceivedChannel;

        [Header("Moderation")]
        [Tooltip("(Optional) SO event channel raised when the adapter fires OnPlayerKicked. " +
                 "Wire a StringGameEventListener on the KickedUI GameObject to this channel " +
                 "and point its Response at KickedUI.ShowKicked. " +
                 "The payload is the kicked player's display name.")]
        [SerializeField] private StringGameEvent _onPlayerKickedChannel;

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
        /// Tell the adapter to create a public room with <paramref name="roomCode"/>
        /// and the default capacity (2 players).
        /// The session must be in Connected state; call <see cref="BeginConnect"/>
        /// first and wait for the <c>onConnected</c> event before hosting.
        /// </summary>
        public void BeginHost(string roomCode)
        {
            BeginHost(roomCode, maxPlayers: 2, isPrivate: false, password: string.Empty);
        }

        /// <summary>
        /// Tell the adapter to create a public room with an explicit player
        /// <paramref name="maxPlayers"/> capacity. The session must be in Connected state.
        /// Values less than 1 are clamped to 2 by the adapter.
        /// </summary>
        public void BeginHost(string roomCode, int maxPlayers)
        {
            BeginHost(roomCode, maxPlayers, isPrivate: false, password: string.Empty);
        }

        /// <summary>
        /// Tell the adapter to create a room with full control over capacity and privacy.
        /// When <paramref name="isPrivate"/> is true, <paramref name="password"/> is stored
        /// server-side; clients must supply it via <see cref="BeginJoin(string,string)"/>.
        /// The session must be in Connected state.
        /// </summary>
        public void BeginHost(string roomCode, int maxPlayers, bool isPrivate, string password)
        {
            if (_session == null) return;

            if (!_session.IsConnected)
            {
                Debug.LogWarning("[NetworkEventBridge] BeginHost called before Connected. Ignored.", this);
                return;
            }

            _adapter?.Host(roomCode, maxPlayers, isPrivate, password);
        }

        /// <summary>
        /// Tell the adapter to join the room identified by <paramref name="roomCode"/>
        /// without a password (suitable for public rooms).
        /// The session must be in Connected state.
        /// </summary>
        public void BeginJoin(string roomCode)
        {
            BeginJoin(roomCode, password: string.Empty);
        }

        /// <summary>
        /// Tell the adapter to join the room with an optional <paramref name="password"/>.
        /// Use this overload when joining a room that may be private.
        /// The session must be in Connected state.
        /// </summary>
        public void BeginJoin(string roomCode, string password)
        {
            if (_session == null) return;

            if (!_session.IsConnected)
            {
                Debug.LogWarning("[NetworkEventBridge] BeginJoin called before Connected. Ignored.", this);
                return;
            }

            _adapter?.Join(roomCode, password);
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

        // ── Chat ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Broadcast a chat message to all players in the current room.
        ///
        /// The message is formatted as "<paramref name="senderName"/>: <paramref name="text"/>"
        /// before being passed to the adapter, so the transport layer stays format-agnostic.
        /// Null or empty <paramref name="senderName"/> is replaced with "Unknown".
        /// Null or whitespace-only <paramref name="text"/> is silently ignored.
        ///
        /// Wire a UI button's OnClick to this method via a UnityEvent that supplies the
        /// text from an InputField — or call it directly from <see cref="BattleRobots.UI.ChatUI"/>.
        /// </summary>
        public void SendChat(string senderName, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            string sender    = string.IsNullOrWhiteSpace(senderName) ? "Unknown" : senderName;
            string formatted = $"{sender}: {text}";
            _adapter?.SendChatMessage(formatted);
        }

        // ── Moderation ────────────────────────────────────────────────────────

        /// <summary>
        /// Ask the adapter to remove <paramref name="playerName"/> from
        /// <paramref name="roomCode"/>. Only the host should call this.
        ///
        /// Guards:
        ///   • No-op (with warning) if the session is not currently in a match room.
        ///   • No-op if <paramref name="playerName"/> is null or whitespace.
        ///
        /// Wire to a per-player "Kick" button's onClick in the Inspector, passing the
        /// player's display name as a static parameter or from a script.
        /// </summary>
        public void BeginKick(string roomCode, string playerName)
        {
            if (_session == null) return;

            if (!_session.IsInMatch)
            {
                Debug.LogWarning("[NetworkEventBridge] BeginKick called while not in a match room. Ignored.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(playerName))
            {
                Debug.LogWarning("[NetworkEventBridge] BeginKick called with null/empty playerName. Ignored.", this);
                return;
            }

            _adapter?.KickPlayer(roomCode, playerName);
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
                _recentRooms?.RecordVisit(roomCode);
            };

            adapter.OnRoomJoinFailed = (reason) =>
            {
                Debug.LogWarning($"[NetworkEventBridge] Room join failed: {reason}");
                // Raise the SO channel so JoinFailureUI (or any listener) can respond.
                _onRoomJoinFailedChannel?.Raise(reason);
                // Stay in Connected state; player may retry with a different room.
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

            adapter.OnRoomUpdated = (entry) =>
            {
                // Patch the in-memory list for the changed room without a full refresh.
                _roomList?.UpdateRoom(entry);
            };

            adapter.OnChatMessageReceived = (message) =>
            {
                // Buffer in the ChatSO ring buffer (if assigned).
                _chat?.AddMessage(message);
                // Raise the SO event channel so Inspector-wired listeners react.
                _onChatReceivedChannel?.Raise(message);
            };

            adapter.OnPlayerKicked = (playerName) =>
            {
                // Raise the SO channel so KickedUI (or any listener) can respond.
                _onPlayerKickedChannel?.Raise(playerName);
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

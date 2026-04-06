using System;
using UnityEngine;

namespace BattleRobots.Core
{
    // ── Connection-state enum ─────────────────────────────────────────────────

    /// <summary>
    /// Represents the network connection lifecycle.
    /// </summary>
    public enum NetworkConnectionState
    {
        /// <summary>No connection attempt has been made.</summary>
        Disconnected,
        /// <summary>A connection or join request is in-flight.</summary>
        Connecting,
        /// <summary>Connected to the lobby server; no active room.</summary>
        Connected,
        /// <summary>Inside an active match room.</summary>
        InMatch,
    }

    // ── Host / client role enum ───────────────────────────────────────────────

    /// <summary>
    /// Whether the local player is hosting or joining a room.
    /// </summary>
    public enum NetworkRole
    {
        /// <summary>No role — player has not yet chosen Host or Join.</summary>
        None,
        /// <summary>Player is hosting a room and waiting for a peer.</summary>
        Host,
        /// <summary>Player is joining an existing room by code.</summary>
        Client,
    }

    // ── NetworkSessionSO ──────────────────────────────────────────────────────

    /// <summary>
    /// Runtime ScriptableObject that holds the authoritative network-session state:
    /// connection status, role (Host/Client), and the current room code.
    ///
    /// All mutation goes through the designated mutators
    /// (<see cref="Connect"/>, <see cref="SetConnected"/>, <see cref="JoinRoom"/>,
    /// <see cref="Disconnect"/>). Read-only properties expose state to UI and
    /// game systems without coupling them to a particular network backend.
    ///
    /// Fires SO event channels on each state transition so listeners (UI labels,
    /// <see cref="NetworkEventBridge"/>, etc.) react without polling.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI must NOT reference Physics.
    ///   • This SO lives in BattleRobots.Core — safe to reference from UI.
    ///   • Asset is reset via <see cref="Disconnect"/> between play sessions;
    ///     it is not persisted to disk (session-scoped state only).
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Network ▶ NetworkSessionSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/NetworkSessionSO", order = 0)]
    public sealed class NetworkSessionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Raised when ConnectionState enters Connecting.")]
        [SerializeField] private VoidGameEvent _onConnecting;

        [Tooltip("Raised when ConnectionState enters Connected (lobby ready).")]
        [SerializeField] private VoidGameEvent _onConnected;

        [Tooltip("Raised when ConnectionState enters InMatch.")]
        [SerializeField] private VoidGameEvent _onMatchJoined;

        [Tooltip("Raised when ConnectionState returns to Disconnected.")]
        [SerializeField] private VoidGameEvent _onDisconnected;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Current lifecycle state of the network session.</summary>
        public NetworkConnectionState ConnectionState { get; private set; } =
            NetworkConnectionState.Disconnected;

        /// <summary>Local player role: Host, Client, or None.</summary>
        public NetworkRole Role { get; private set; } = NetworkRole.None;

        /// <summary>
        /// Four-character room code used to identify the current match room.
        /// Empty string when no room is active.
        /// </summary>
        public string RoomCode { get; private set; } = string.Empty;

        /// <summary>
        /// True when <see cref="ConnectionState"/> is
        /// <see cref="NetworkConnectionState.Connected"/> or
        /// <see cref="NetworkConnectionState.InMatch"/>.
        /// </summary>
        public bool IsConnected =>
            ConnectionState == NetworkConnectionState.Connected ||
            ConnectionState == NetworkConnectionState.InMatch;

        /// <summary>
        /// True when <see cref="ConnectionState"/> is
        /// <see cref="NetworkConnectionState.InMatch"/>.
        /// </summary>
        public bool IsInMatch => ConnectionState == NetworkConnectionState.InMatch;

        // ── Designated mutators ───────────────────────────────────────────────

        /// <summary>
        /// Begin a connection attempt with the specified <paramref name="role"/>.
        /// Transitions state to <see cref="NetworkConnectionState.Connecting"/>
        /// and raises <see cref="_onConnecting"/>.
        /// No-op (with warning) if already Connecting or InMatch.
        /// </summary>
        public void Connect(NetworkRole role)
        {
            if (ConnectionState == NetworkConnectionState.Connecting ||
                ConnectionState == NetworkConnectionState.InMatch)
            {
                Debug.LogWarning($"[NetworkSessionSO] Connect() called while in state " +
                                 $"{ConnectionState}. Ignored.", this);
                return;
            }

            Role            = role;
            ConnectionState = NetworkConnectionState.Connecting;
            _onConnecting?.Raise();

            Debug.Log($"[NetworkSessionSO] Connecting as {role}.");
        }

        /// <summary>
        /// Acknowledge that the transport layer is now connected to the lobby server.
        /// Transitions state to <see cref="NetworkConnectionState.Connected"/>
        /// and raises <see cref="_onConnected"/>.
        /// Expected to be called by <see cref="NetworkEventBridge"/> once the
        /// adapter's connect callback fires.
        /// </summary>
        public void SetConnected()
        {
            if (ConnectionState != NetworkConnectionState.Connecting)
            {
                Debug.LogWarning($"[NetworkSessionSO] SetConnected() called from state " +
                                 $"{ConnectionState}. Expected Connecting. Ignored.", this);
                return;
            }

            ConnectionState = NetworkConnectionState.Connected;
            _onConnected?.Raise();

            Debug.Log("[NetworkSessionSO] Connected to lobby.");
        }

        /// <summary>
        /// Enter a match room identified by <paramref name="roomCode"/>.
        /// Transitions state to <see cref="NetworkConnectionState.InMatch"/>
        /// and raises <see cref="_onMatchJoined"/>.
        /// Can only be called from <see cref="NetworkConnectionState.Connected"/>.
        /// </summary>
        public void JoinRoom(string roomCode)
        {
            if (ConnectionState != NetworkConnectionState.Connected)
            {
                Debug.LogWarning($"[NetworkSessionSO] JoinRoom() called from state " +
                                 $"{ConnectionState}. Must be Connected first. Ignored.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                Debug.LogWarning("[NetworkSessionSO] JoinRoom() called with empty room code. Ignored.", this);
                return;
            }

            RoomCode        = roomCode.Trim().ToUpperInvariant();
            ConnectionState = NetworkConnectionState.InMatch;
            _onMatchJoined?.Raise();

            Debug.Log($"[NetworkSessionSO] Joined room '{RoomCode}' as {Role}.");
        }

        /// <summary>
        /// Tear down the session from any state.
        /// Transitions to <see cref="NetworkConnectionState.Disconnected"/>,
        /// clears role and room code, raises <see cref="_onDisconnected"/>.
        /// Safe to call from any state.
        /// </summary>
        public void Disconnect()
        {
            Role            = NetworkRole.None;
            RoomCode        = string.Empty;
            ConnectionState = NetworkConnectionState.Disconnected;
            _onDisconnected?.Raise();

            Debug.Log("[NetworkSessionSO] Disconnected.");
        }
    }
}

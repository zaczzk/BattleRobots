using System;
using System.Collections.Generic;

namespace BattleRobots.Core
{
    /// <summary>
    /// Abstraction over the underlying network transport (Photon, Mirror, stub, etc.).
    ///
    /// <see cref="NetworkEventBridge"/> holds one <c>INetworkAdapter</c> reference and
    /// delegates all network operations to it.  Swapping adapters (e.g. replacing
    /// <see cref="StubNetworkAdapter"/> with a real Photon adapter) requires no
    /// changes to game code.
    ///
    /// ARCHITECTURE RULES:
    ///   • Lives in BattleRobots.Core — no Unity types in this interface so it
    ///     can be implemented and tested without MonoBehaviour overhead.
    ///   • Callbacks are plain <see cref="Action"/> / <see cref="Action{T}"/>
    ///     delegates; implementors invoke them on the main thread.
    ///   • All methods are allocation-free on the call site (no params arrays).
    /// </summary>
    public interface INetworkAdapter
    {
        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Initiate a connection to the network backend.
        /// Calls <see cref="OnConnected"/> on success, <see cref="OnDisconnected"/>
        /// on failure.
        /// </summary>
        void Connect();

        /// <summary>
        /// Gracefully disconnect from the backend.
        /// Must invoke <see cref="OnDisconnected"/> when the operation completes.
        /// </summary>
        void Disconnect();

        // ── Room operations ───────────────────────────────────────────────────

        /// <summary>
        /// Create and host a new match room with the given <paramref name="roomCode"/>.
        /// Calls <see cref="OnRoomJoined"/> with the room code on success.
        /// </summary>
        void Host(string roomCode);

        /// <summary>
        /// Create and host a room with an explicit player capacity.
        /// <paramref name="maxPlayers"/> specifies the maximum number of players
        /// (including the host) allowed in the room. Values &lt; 1 are clamped to 2.
        /// Calls <see cref="OnRoomJoined"/> with the confirmed room code on success.
        /// </summary>
        void Host(string roomCode, int maxPlayers);

        /// <summary>
        /// Create and host a private room with an explicit player capacity and password.
        /// When <paramref name="isPrivate"/> is true, clients must supply the matching
        /// <paramref name="password"/> via <see cref="Join(string,string)"/> to enter.
        /// The password is never broadcast to room-list clients — only the
        /// <see cref="RoomEntry.isPrivate"/> flag is visible publicly.
        /// Values of <paramref name="maxPlayers"/> &lt; 1 are clamped to 2.
        /// Calls <see cref="OnRoomJoined"/> with the confirmed room code on success.
        /// </summary>
        void Host(string roomCode, int maxPlayers, bool isPrivate, string password);

        /// <summary>
        /// Join an existing match room identified by <paramref name="roomCode"/>.
        /// For public rooms the password is ignored. For private rooms an empty or
        /// mismatched password results in <see cref="OnRoomJoinFailed"/>.
        /// Calls <see cref="OnRoomJoined"/> with the confirmed code on success,
        /// or <see cref="OnRoomJoinFailed"/> if the room does not exist, is full,
        /// or the password is wrong.
        /// </summary>
        void Join(string roomCode);

        /// <summary>
        /// Join an existing room with an optional <paramref name="password"/>.
        /// Use this overload when joining a room that may be private.
        /// For public rooms the password argument is ignored.
        /// Calls <see cref="OnRoomJoined"/> on success; <see cref="OnRoomJoinFailed"/>
        /// on any failure (not found, full, wrong password).
        /// </summary>
        void Join(string roomCode, string password);

        // ── Room discovery ────────────────────────────────────────────────────

        /// <summary>
        /// Ask the backend for the current list of available rooms.
        /// The adapter invokes <see cref="OnRoomListReceived"/> asynchronously (or
        /// synchronously for in-process stubs) with the fetched list.
        /// Implementors must never allocate on the call site; the result list is
        /// passed through <see cref="OnRoomListReceived"/> and owned by the callee.
        /// </summary>
        void RequestRoomList();

        // ── Match-state messaging ─────────────────────────────────────────────

        /// <summary>
        /// Broadcast a serialised match-state payload to all peers in the current room.
        /// The adapter is responsible for framing and delivery; game code only provides
        /// raw bytes.
        /// </summary>
        void SendMatchState(byte[] payload);

        // ── Chat messaging ────────────────────────────────────────────────────

        /// <summary>
        /// Broadcast a chat message to all players in the current room.
        ///
        /// The adapter is responsible for delivery. The message string is pre-formatted
        /// by the caller (e.g. "Alice: Hello!") so the adapter stays transport-agnostic.
        ///
        /// Implementors must never allocate on the call site.
        /// </summary>
        void SendChatMessage(string message);

        // ── Callbacks (set by NetworkEventBridge before calling Connect) ──────

        /// <summary>Invoked by the adapter when the connection to the backend succeeds.</summary>
        Action OnConnected { get; set; }

        /// <summary>Invoked by the adapter when the connection drops or an error occurs.</summary>
        Action OnDisconnected { get; set; }

        /// <summary>
        /// Invoked by the adapter after a successful <see cref="Host"/> or <see cref="Join"/>.
        /// Passes back the confirmed room code.
        /// </summary>
        Action<string> OnRoomJoined { get; set; }

        /// <summary>
        /// Invoked by the adapter when <see cref="Join"/> fails (room not found, full, etc.).
        /// Passes back a human-readable reason.
        /// </summary>
        Action<string> OnRoomJoinFailed { get; set; }

        /// <summary>
        /// Invoked by the adapter whenever a <see cref="SendMatchState"/> payload is
        /// received from a remote peer.  Passes back the raw byte array.
        /// </summary>
        Action<byte[]> OnMatchStateReceived { get; set; }

        /// <summary>
        /// Invoked by the adapter whenever the state of a room changes — for example,
        /// when a player joins or leaves. Passes the fully-updated <see cref="RoomEntry"/>
        /// so subscribers can update their UI without re-fetching the full room list.
        ///
        /// Adapters that do not support real-time room updates may leave this unimplemented
        /// (null callback is acceptable; callers must guard against null).
        /// </summary>
        Action<RoomEntry> OnRoomUpdated { get; set; }

        /// <summary>
        /// Invoked by the adapter when the spectator count for a room changes.
        /// Parameters: roomCode (string), new spectator count (int ≥ 0).
        ///
        /// Adapters that do not support spectator tracking may leave this unimplemented
        /// (null callback is acceptable; callers must guard against null).
        /// </summary>
        Action<string, int> OnSpectatorCountChanged { get; set; }

        /// <summary>
        /// Invoked by the adapter when a chat message is received from a remote player.
        /// The payload is the pre-formatted string delivered by the sender
        /// (e.g. "Alice: Hello!").
        ///
        /// Adapters that do not support in-room chat may leave this unimplemented
        /// (null callback is acceptable; callers must guard against null).
        /// </summary>
        Action<string> OnChatMessageReceived { get; set; }

        /// <summary>
        /// Invoked by the adapter in response to <see cref="RequestRoomList"/>.
        /// Passes the current list of available <see cref="RoomEntry"/> values.
        /// The adapter owns the list allocation; the callee must not hold a long-lived
        /// reference to it.
        /// </summary>
        Action<List<RoomEntry>> OnRoomListReceived { get; set; }

        // ── Diagnostics ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current round-trip latency to the backend server in milliseconds.
        /// Returns 0 when not connected or when the transport does not support ping.
        /// Must be allocation-free (read a cached value; never allocate on each call).
        /// </summary>
        int GetPingMs();
    }
}

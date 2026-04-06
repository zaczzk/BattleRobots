using System;

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
        /// Join an existing match room identified by <paramref name="roomCode"/>.
        /// Calls <see cref="OnRoomJoined"/> with the confirmed code on success,
        /// or <see cref="OnRoomJoinFailed"/> if the room does not exist.
        /// </summary>
        void Join(string roomCode);

        // ── Match-state messaging ─────────────────────────────────────────────

        /// <summary>
        /// Broadcast a serialised match-state payload to all peers in the current room.
        /// The adapter is responsible for framing and delivery; game code only provides
        /// raw bytes.
        /// </summary>
        void SendMatchState(byte[] payload);

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
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// In-memory, no-real-networking implementation of <see cref="INetworkAdapter"/>.
    ///
    /// Used in EditMode/PlayMode tests and as a development stand-in before a real
    /// Photon/Mirror adapter is integrated.
    ///
    /// Behaviour contract:
    ///   • <see cref="Connect"/>       — immediately calls <see cref="OnConnected"/>.
    ///   • <see cref="Disconnect"/>    — immediately calls <see cref="OnDisconnected"/>.
    ///   • <see cref="Host"/>          — stores the room code and calls <see cref="OnRoomJoined"/>.
    ///   • <see cref="Join"/>          — succeeds if the code is in <see cref="s_ActiveRooms"/>;
    ///                                   calls <see cref="OnRoomJoinFailed"/> otherwise.
    ///   • <see cref="SendMatchState"/>— appends the payload to <see cref="SentPayloads"/> for
    ///                                   inspection in tests; does NOT broadcast to a real peer.
    ///
    /// Typical test pattern:
    /// <code>
    /// var stub = new StubNetworkAdapter();
    /// bridge.SetAdapter(stub);
    /// stub.Connect();
    /// stub.Host("AAAA");
    /// Assert.AreEqual(NetworkConnectionState.InMatch, session.ConnectionState);
    /// </code>
    ///
    /// ARCHITECTURE RULES:
    ///   • No MonoBehaviour — pure C# class, no Unity lifecycle.
    ///   • No heap allocations on the hot path (list uses pre-sized capacity).
    ///   • Lives in BattleRobots.Core, compiled into the main assembly.
    /// </summary>
    public sealed class StubNetworkAdapter : INetworkAdapter
    {
        // ── Shared state (simulates a lobby server for two-player local tests) ─

        /// <summary>
        /// Room codes that exist in the "server". Tests or host adapters register
        /// a room via <see cref="Host"/> and a client adapter may join it.
        /// Cleared between test runs by calling <see cref="ClearRooms"/>.
        /// </summary>
        private static readonly HashSet<string> s_ActiveRooms =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Remove all simulated rooms (call in TearDown).</summary>
        public static void ClearRooms() => s_ActiveRooms.Clear();

        // ── INetworkAdapter callbacks ─────────────────────────────────────────

        /// <inheritdoc/>
        public Action OnConnected { get; set; }

        /// <inheritdoc/>
        public Action OnDisconnected { get; set; }

        /// <inheritdoc/>
        public Action<string> OnRoomJoined { get; set; }

        /// <inheritdoc/>
        public Action<string> OnRoomJoinFailed { get; set; }

        /// <inheritdoc/>
        public Action<byte[]> OnMatchStateReceived { get; set; }

        /// <inheritdoc/>
        public Action<List<RoomEntry>> OnRoomListReceived { get; set; }

        // ── Test-inspection surface ───────────────────────────────────────────

        /// <summary>All payloads passed to <see cref="SendMatchState"/>, in order.</summary>
        public List<byte[]> SentPayloads { get; } = new List<byte[]>(8);

        /// <summary>Number of times <see cref="Connect"/> has been called.</summary>
        public int ConnectCallCount { get; private set; }

        /// <summary>Number of times <see cref="Disconnect"/> has been called.</summary>
        public int DisconnectCallCount { get; private set; }

        /// <summary>Number of times <see cref="RequestRoomList"/> has been called.</summary>
        public int RequestRoomListCallCount { get; private set; }

        /// <summary>Last room code passed to <see cref="Host"/> or <see cref="Join"/>.</summary>
        public string LastRoomCode { get; private set; } = string.Empty;

        // ── INetworkAdapter implementation ────────────────────────────────────

        /// <summary>
        /// Simulate a successful connection. Immediately invokes <see cref="OnConnected"/>.
        /// </summary>
        public void Connect()
        {
            ConnectCallCount++;
            OnConnected?.Invoke();
        }

        /// <summary>
        /// Simulate a disconnect. Immediately invokes <see cref="OnDisconnected"/>.
        /// </summary>
        public void Disconnect()
        {
            DisconnectCallCount++;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Register <paramref name="roomCode"/> in the shared room set and invoke
        /// <see cref="OnRoomJoined"/> with the normalised code.
        /// </summary>
        public void Host(string roomCode)
        {
            string code = Normalise(roomCode);
            LastRoomCode = code;
            s_ActiveRooms.Add(code);
            OnRoomJoined?.Invoke(code);
        }

        /// <summary>
        /// Join the room if it exists in <see cref="s_ActiveRooms"/>; otherwise invoke
        /// <see cref="OnRoomJoinFailed"/> with a descriptive reason.
        /// </summary>
        public void Join(string roomCode)
        {
            string code = Normalise(roomCode);
            LastRoomCode = code;

            if (s_ActiveRooms.Contains(code))
            {
                OnRoomJoined?.Invoke(code);
            }
            else
            {
                string reason = $"Room '{code}' not found.";
                Debug.Log($"[StubNetworkAdapter] Join failed: {reason}");
                OnRoomJoinFailed?.Invoke(reason);
            }
        }

        /// <summary>
        /// Record the payload for test inspection. Does not transmit anything.
        /// </summary>
        public void SendMatchState(byte[] payload)
        {
            if (payload == null) return;
            SentPayloads.Add(payload);
        }

        /// <summary>
        /// Simulate a room-list response. Converts the current <see cref="s_ActiveRooms"/>
        /// set to a <see cref="List{RoomEntry}"/> (playerCount = 1 per room as a stub default)
        /// and immediately invokes <see cref="OnRoomListReceived"/>.
        /// </summary>
        public void RequestRoomList()
        {
            RequestRoomListCallCount++;

            var rooms = new List<RoomEntry>(s_ActiveRooms.Count);
            foreach (string code in s_ActiveRooms)
                rooms.Add(new RoomEntry(code, 1));

            OnRoomListReceived?.Invoke(rooms);
        }

        // ── Diagnostics ───────────────────────────────────────────────────────

        /// <summary>
        /// Configurable simulated ping value returned by <see cref="GetPingMs"/>.
        /// Set in tests to exercise ping-display thresholds.  Defaults to 0.
        /// </summary>
        public int FakePingMs { get; set; } = 0;

        /// <inheritdoc/>
        public int GetPingMs() => FakePingMs;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Normalise(string code) =>
            string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Trim().ToUpperInvariant();
    }
}

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
        /// Rooms that exist in the "server", keyed by normalised room code.
        /// Value holds the full <see cref="RoomEntry"/> so capacity and player
        /// count can be queried by tests and by <see cref="RequestRoomList"/>.
        ///
        /// Tests or host adapters register rooms via <see cref="Host"/> overloads.
        /// Cleared between test runs by calling <see cref="ClearRooms"/>.
        /// </summary>
        private static readonly Dictionary<string, RoomEntry> s_ActiveRooms =
            new Dictionary<string, RoomEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Server-side password store for private rooms. Never exposed to clients.
        /// Keyed by normalised room code; only populated for rooms hosted with
        /// <see cref="Host(string,int,bool,string)"/> where isPrivate = true.
        /// </summary>
        private static readonly Dictionary<string, string> s_RoomPasswords =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Server-side simulated ping store, keyed by normalised room code.
        /// Populated via <see cref="SetRoomPing"/>; used by <see cref="RequestRoomList"/>
        /// to populate <see cref="RoomEntry.pingMs"/> for each returned room.
        /// </summary>
        private static readonly Dictionary<string, int> s_RoomPings =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Server-side simulated creation timestamp store, keyed by normalised room code.
        /// Populated via <see cref="SetRoomCreatedAt"/>; used by <see cref="RequestRoomList"/>
        /// to populate <see cref="RoomEntry.createdAt"/> for each returned room.
        /// 0 (default) means creation time is unknown.
        /// </summary>
        private static readonly Dictionary<string, long> s_RoomCreatedAt =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Server-side list of player display names per room, keyed by normalised room code.
        /// Index 0 is always the host's name; subsequent entries are joiners in join order.
        /// Populated by <see cref="Host"/> and extended by <see cref="Join(string,string)"/>.
        /// Cleared by <see cref="ClearRooms"/>.
        /// </summary>
        private static readonly Dictionary<string, List<string>> s_RoomPlayerNames =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Server-side simulated spectator count store, keyed by normalised room code.
        /// Populated via <see cref="SetSpectatorCount"/>; used by <see cref="RequestRoomList"/>
        /// to populate <see cref="RoomEntry.spectatorCount"/> for each returned room.
        /// 0 (default) means no spectators (or not tracked).
        /// </summary>
        private static readonly Dictionary<string, int> s_SpectatorCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Server-side set of muted player names per room, keyed by normalised room code.
        /// Muted players' chat messages are not forwarded to other room members.
        /// Populated by <see cref="MutePlayer"/>; entries removed by <see cref="UnmutePlayer"/>
        /// and cleared by <see cref="ClearRooms"/>.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> s_MutedPlayers =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Remove all simulated rooms, passwords, pings, creation times, player names, spectator counts, and muted-player sets (call in TearDown).</summary>
        public static void ClearRooms()
        {
            s_ActiveRooms.Clear();
            s_RoomPasswords.Clear();
            s_RoomPings.Clear();
            s_RoomCreatedAt.Clear();
            s_RoomPlayerNames.Clear();
            s_SpectatorCounts.Clear();
            s_MutedPlayers.Clear();
        }

        /// <summary>
        /// Assign a simulated spectator count to an existing room so that the next
        /// <see cref="RequestRoomList"/> call returns it in <see cref="RoomEntry.spectatorCount"/>.
        ///
        /// <paramref name="count"/> is clamped to ≥ 0. Call after <see cref="Host"/> has
        /// registered the room. Pass 0 to clear a previously-set value.
        /// </summary>
        public static void SetSpectatorCount(string roomCode, int count)
        {
            string code = string.IsNullOrWhiteSpace(roomCode)
                ? string.Empty
                : roomCode.Trim().ToUpperInvariant();

            int clamped = Math.Max(0, count);
            if (clamped == 0)
                s_SpectatorCounts.Remove(code);
            else
                s_SpectatorCounts[code] = clamped;
        }

        // ── Host identity ─────────────────────────────────────────────────────

        /// <summary>
        /// Display name stored as <see cref="RoomEntry.hostName"/> when
        /// <see cref="Host(string)"/> (or any overload) is called on this instance.
        /// Defaults to <c>"Host"</c>. Tests can change this between Host calls to
        /// simulate different room owners.
        /// </summary>
        public string HostPlayerName { get; set; } = "Host";

        /// <summary>
        /// Display name appended to a room's <c>playerNames</c> list when
        /// <see cref="Join(string,string)"/> succeeds.
        /// Defaults to <c>"Player"</c>. Tests can override this to simulate
        /// specific player identities joining a room.
        /// </summary>
        public string JoinPlayerName { get; set; } = "Player";

        /// <summary>
        /// Assign a simulated latency to an existing room so that the next
        /// <see cref="RequestRoomList"/> call returns it in <see cref="RoomEntry.pingMs"/>.
        ///
        /// <paramref name="pingMs"/> is clamped to ≥ 0. Call after <see cref="Host"/>
        /// has registered the room.
        /// </summary>
        public static void SetRoomPing(string roomCode, int pingMs)
        {
            string code = string.IsNullOrWhiteSpace(roomCode)
                ? string.Empty
                : roomCode.Trim().ToUpperInvariant();

            s_RoomPings[code] = Math.Max(0, pingMs);
        }

        /// <summary>
        /// Assign a simulated creation timestamp (UTC ticks) to an existing room so that
        /// the next <see cref="RequestRoomList"/> call returns it in
        /// <see cref="RoomEntry.createdAt"/>. Pass 0 to clear a previously-set value.
        /// Call after <see cref="Host"/> has registered the room.
        /// </summary>
        public static void SetRoomCreatedAt(string roomCode, long createdAtTicks)
        {
            string code = string.IsNullOrWhiteSpace(roomCode)
                ? string.Empty
                : roomCode.Trim().ToUpperInvariant();

            if (createdAtTicks <= 0L)
                s_RoomCreatedAt.Remove(code);
            else
                s_RoomCreatedAt[code] = createdAtTicks;
        }

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

        /// <inheritdoc/>
        public Action<RoomEntry> OnRoomUpdated { get; set; }

        /// <inheritdoc/>
        public Action<string, int> OnSpectatorCountChanged { get; set; }

        /// <inheritdoc/>
        public Action<string> OnChatMessageReceived { get; set; }

        /// <inheritdoc/>
        public Action<string> OnPlayerKicked { get; set; }

        /// <inheritdoc/>
        public Action<string> OnPlayerMuted { get; set; }

        /// <inheritdoc/>
        public Action<string> OnPlayerUnmuted { get; set; }

        // ── Test-inspection surface ───────────────────────────────────────────

        /// <summary>All payloads passed to <see cref="SendMatchState"/>, in order.</summary>
        public List<byte[]> SentPayloads { get; } = new List<byte[]>(8);

        /// <summary>All messages passed to <see cref="SendChatMessage"/>, in order.</summary>
        public List<string> SentChatMessages { get; } = new List<string>(8);

        /// <summary>Number of times <see cref="Connect"/> has been called.</summary>
        public int ConnectCallCount { get; private set; }

        /// <summary>Number of times <see cref="Disconnect"/> has been called.</summary>
        public int DisconnectCallCount { get; private set; }

        /// <summary>Number of times <see cref="RequestRoomList"/> has been called.</summary>
        public int RequestRoomListCallCount { get; private set; }

        /// <summary>Number of times <see cref="KickPlayer"/> has been called.</summary>
        public int KickCallCount { get; private set; }

        /// <summary>Display name of the most recently kicked player, or empty string if none.</summary>
        public string LastKickedPlayer { get; private set; } = string.Empty;

        /// <summary>Number of times <see cref="MutePlayer"/> has been called.</summary>
        public int MuteCallCount { get; private set; }

        /// <summary>Display name of the most recently muted player, or empty string if none.</summary>
        public string LastMutedPlayer { get; private set; } = string.Empty;

        /// <summary>Number of times <see cref="UnmutePlayer"/> has been called.</summary>
        public int UnmuteCallCount { get; private set; }

        /// <summary>Display name of the most recently unmuted player, or empty string if none.</summary>
        public string LastUnmutedPlayer { get; private set; } = string.Empty;

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
        /// Register <paramref name="roomCode"/> as a public room with the default
        /// capacity (2 players) and invoke <see cref="OnRoomJoined"/>.
        /// </summary>
        public void Host(string roomCode)
        {
            Host(roomCode, maxPlayers: 2, isPrivate: false, password: string.Empty);
        }

        /// <summary>
        /// Register <paramref name="roomCode"/> with an explicit <paramref name="maxPlayers"/>
        /// capacity as a public room and invoke <see cref="OnRoomJoined"/>.
        /// </summary>
        public void Host(string roomCode, int maxPlayers)
        {
            Host(roomCode, maxPlayers, isPrivate: false, password: string.Empty);
        }

        /// <summary>
        /// Register <paramref name="roomCode"/> with an explicit capacity, privacy flag,
        /// and server-side <paramref name="password"/>.
        ///
        /// When <paramref name="isPrivate"/> is true the password is stored server-side
        /// (never broadcast) and verified by <see cref="Join(string,string)"/>.
        /// The host counts as the first player (playerCount = 1).
        /// </summary>
        public void Host(string roomCode, int maxPlayers, bool isPrivate, string password)
        {
            string code = Normalise(roomCode);
            int    cap  = maxPlayers > 0 ? maxPlayers : 2;

            LastRoomCode = code;

            // Initialise player-name list with the host as the first entry.
            var names = new List<string> { HostPlayerName };
            s_RoomPlayerNames[code] = names;

            s_ActiveRooms[code] = new RoomEntry(code, playerCount: 1, maxPlayers: cap,
                                                isPrivate: isPrivate, hostName: HostPlayerName,
                                                playerNames: new List<string>(names));

            if (isPrivate && !string.IsNullOrEmpty(password))
                s_RoomPasswords[code] = password;
            else
                s_RoomPasswords.Remove(code); // public or blank-password rooms have no entry

            OnRoomJoined?.Invoke(code);
        }

        /// <summary>
        /// Join the room without a password. Succeeds for public rooms; fails with
        /// "wrong password" for private rooms (equivalent to supplying an empty password).
        /// </summary>
        public void Join(string roomCode)
        {
            Join(roomCode, password: string.Empty);
        }

        /// <summary>
        /// Join the room, supplying an optional <paramref name="password"/>.
        ///
        /// Failure reasons (in priority order):
        ///   1. Room not found.
        ///   2. Room is full.
        ///   3. Room is private and <paramref name="password"/> does not match.
        ///
        /// On success the room's <c>playerCount</c> is incremented.
        /// </summary>
        public void Join(string roomCode, string password)
        {
            string code = Normalise(roomCode);
            LastRoomCode = code;

            if (!s_ActiveRooms.TryGetValue(code, out RoomEntry room))
            {
                string reason = $"Room '{code}' not found.";
                Debug.Log($"[StubNetworkAdapter] Join failed: {reason}");
                OnRoomJoinFailed?.Invoke(reason);
                return;
            }

            if (room.IsFull)
            {
                string reason = $"Room '{code}' is full ({room.playerCount}/{room.maxPlayers}).";
                Debug.Log($"[StubNetworkAdapter] Join failed: {reason}");
                OnRoomJoinFailed?.Invoke(reason);
                return;
            }

            if (room.isPrivate)
            {
                bool hasPassword = s_RoomPasswords.TryGetValue(code, out string stored);
                bool correct     = hasPassword && stored == (password ?? string.Empty);

                if (!correct)
                {
                    string reason = $"Room '{code}' requires a password.";
                    Debug.Log($"[StubNetworkAdapter] Join failed: {reason}");
                    OnRoomJoinFailed?.Invoke(reason);
                    return;
                }
            }

            // Append joining player name to the server-side list.
            if (!s_RoomPlayerNames.TryGetValue(code, out List<string> names))
            {
                names = new List<string>();
                s_RoomPlayerNames[code] = names;
            }
            names.Add(JoinPlayerName);

            // Write back updated entry (struct copy); preserve all existing fields.
            var updatedEntry = new RoomEntry(code, room.playerCount + 1, room.maxPlayers,
                                             room.isPrivate, room.pingMs, room.hostName,
                                             room.createdAt, new List<string>(names));
            s_ActiveRooms[code] = updatedEntry;

            // Notify subscribers that this room's state has changed.
            OnRoomUpdated?.Invoke(updatedEntry);

            OnRoomJoined?.Invoke(code);
        }

        /// <summary>
        /// Remove <paramref name="playerName"/> from the room and fire
        /// <see cref="OnPlayerKicked"/> and <see cref="OnRoomUpdated"/>.
        ///
        /// No-op (no callbacks) if <paramref name="roomCode"/> does not exist in
        /// <see cref="s_ActiveRooms"/>. Null or empty <paramref name="playerName"/>
        /// is silently ignored.
        /// </summary>
        public void KickPlayer(string roomCode, string playerName)
        {
            KickCallCount++;

            if (string.IsNullOrEmpty(playerName)) return;

            string code = Normalise(roomCode);
            if (!s_ActiveRooms.TryGetValue(code, out RoomEntry room)) return;

            LastKickedPlayer = playerName;

            // Remove from server-side player-name list.
            if (s_RoomPlayerNames.TryGetValue(code, out List<string> names))
                names.Remove(playerName);

            // Decrement player count (floor at 0).
            int newCount = Math.Max(0, room.playerCount - 1);

            List<string> updatedNames = s_RoomPlayerNames.TryGetValue(code, out List<string> n)
                ? new List<string>(n)
                : null;

            var updated = new RoomEntry(code, newCount, room.maxPlayers, room.isPrivate,
                                        room.pingMs, room.hostName, room.createdAt,
                                        updatedNames, room.spectatorCount);
            s_ActiveRooms[code] = updated;

            OnPlayerKicked?.Invoke(playerName);
            OnRoomUpdated?.Invoke(updated);
        }

        /// <summary>
        /// Silence <paramref name="playerName"/> in <paramref name="roomCode"/>.
        ///
        /// - No-op (no callbacks) if the room does not exist.
        /// - No-op (no callbacks) if the player is already muted.
        /// - Fires <see cref="OnPlayerMuted"/> with the player name on success.
        /// - Increments <see cref="MuteCallCount"/> regardless of outcome.
        /// </summary>
        public void MutePlayer(string roomCode, string playerName)
        {
            MuteCallCount++;

            if (string.IsNullOrEmpty(playerName)) return;

            string code = Normalise(roomCode);
            if (!s_ActiveRooms.ContainsKey(code)) return;

            if (!s_MutedPlayers.TryGetValue(code, out HashSet<string> muted))
            {
                muted = new HashSet<string>(StringComparer.Ordinal);
                s_MutedPlayers[code] = muted;
            }

            // Already muted — no-op.
            if (!muted.Add(playerName)) return;

            LastMutedPlayer = playerName;
            OnPlayerMuted?.Invoke(playerName);
        }

        /// <summary>
        /// Restore chat for a previously-muted <paramref name="playerName"/> in
        /// <paramref name="roomCode"/>.
        ///
        /// - No-op (no callbacks) if the room does not exist.
        /// - No-op (no callbacks) if the player is not muted.
        /// - Fires <see cref="OnPlayerUnmuted"/> with the player name on success.
        /// - Increments <see cref="UnmuteCallCount"/> regardless of outcome.
        /// </summary>
        public void UnmutePlayer(string roomCode, string playerName)
        {
            UnmuteCallCount++;

            if (string.IsNullOrEmpty(playerName)) return;

            string code = Normalise(roomCode);
            if (!s_ActiveRooms.ContainsKey(code)) return;

            if (!s_MutedPlayers.TryGetValue(code, out HashSet<string> muted)) return;

            // Not muted — no-op.
            if (!muted.Remove(playerName)) return;

            LastUnmutedPlayer = playerName;
            OnPlayerUnmuted?.Invoke(playerName);
        }

        /// <summary>
        /// Returns true if <paramref name="playerName"/> is currently muted in
        /// <paramref name="roomCode"/>.
        /// </summary>
        public bool IsMuted(string roomCode, string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return false;
            string code = Normalise(roomCode);
            return s_MutedPlayers.TryGetValue(code, out HashSet<string> muted)
                   && muted.Contains(playerName);
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
        /// Record the chat message for test inspection.
        /// Does not auto-fire <see cref="OnChatMessageReceived"/> (tests can do so
        /// explicitly to simulate a loopback from the remote peer).
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (message == null) return;
            SentChatMessages.Add(message);
        }

        /// <summary>
        /// Simulate a room-list response. Returns all rooms currently in
        /// <see cref="s_ActiveRooms"/> — including <c>playerCount</c>,
        /// <c>maxPlayers</c>, any <c>pingMs</c> set via <see cref="SetRoomPing"/>,
        /// and any <c>createdAt</c> set via <see cref="SetRoomCreatedAt"/> —
        /// via <see cref="OnRoomListReceived"/>.
        /// </summary>
        public void RequestRoomList()
        {
            RequestRoomListCallCount++;

            var rooms = new List<RoomEntry>(s_ActiveRooms.Count);
            foreach (var kvp in s_ActiveRooms)
            {
                RoomEntry entry = kvp.Value;
                if (s_RoomPings.TryGetValue(kvp.Key, out int ping))
                    entry.pingMs = ping;
                if (s_RoomCreatedAt.TryGetValue(kvp.Key, out long ts))
                    entry.createdAt = ts;
                if (s_RoomPlayerNames.TryGetValue(kvp.Key, out List<string> names))
                    entry.playerNames = new List<string>(names); // defensive copy
                if (s_SpectatorCounts.TryGetValue(kvp.Key, out int spectators))
                    entry.spectatorCount = spectators;
                rooms.Add(entry);
            }

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

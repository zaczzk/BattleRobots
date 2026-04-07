using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    // ── PingTier ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Latency classification used for section-header grouping in
    /// <see cref="RoomListUI"/> and colour-coding in <see cref="RoomEntryUI"/>.
    ///
    /// Enum int values define the display order (lowest = shown first):
    ///   Excellent → Good → High → Unknown
    /// </summary>
    public enum PingTier
    {
        /// <summary>Round-trip latency ≤ 80 ms — best connection quality.</summary>
        Excellent = 0,

        /// <summary>Round-trip latency 81–150 ms — acceptable connection.</summary>
        Good = 1,

        /// <summary>Round-trip latency > 150 ms — degraded connection.</summary>
        High = 2,

        /// <summary>Ping was not measured (pingMs = 0). Shown last in the list.</summary>
        Unknown = 3,
    }

    // ── RoomSortMode ──────────────────────────────────────────────────────────

    /// <summary>
    /// Controls the ordering of rooms returned by
    /// <see cref="RoomListSO.GetSortedFilteredRooms"/>.
    /// </summary>
    public enum RoomSortMode
    {
        /// <summary>Preserve the order rooms were received from the network.</summary>
        None,

        /// <summary>Rooms with the most current players appear first.</summary>
        ByPlayerCountDesc,

        /// <summary>Rooms sorted A→Z by room code (case-insensitive ordinal).</summary>
        ByRoomCodeAsc,
    }

    // ── RoomEntry ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight descriptor for a single network room visible in the browser.
    /// Serializable so it can be passed through the event pipeline and stored
    /// in a MatchRecord if needed in future.
    /// </summary>
    [Serializable]
    public struct RoomEntry
    {
        [Tooltip("Four-character room code used to join the room.")]
        public string roomCode;

        [Tooltip("Number of players currently in the room.")]
        public int playerCount;

        [Tooltip("Maximum number of players allowed in this room.")]
        public int maxPlayers;

        [Tooltip("When true this room requires a password to join. " +
                 "The password itself is never transmitted to clients; only this flag is.")]
        public bool isPrivate;

        [Tooltip("Estimated round-trip latency to the room host in milliseconds. " +
                 "0 means unknown / not yet measured. Negative values are clamped to 0.")]
        public int pingMs;

        [Tooltip("Display name of the player who created (hosts) this room. " +
                 "Empty string when the host name is not provided.")]
        public string hostName;

        [Tooltip("UTC ticks (DateTime.UtcNow.Ticks) at the moment the room was created. " +
                 "0 means the creation time is unknown or was not provided by the adapter.")]
        public long createdAt;

        /// <summary>
        /// Convenience constructor for use in tests and the stub adapter.
        /// </summary>
        /// <param name="roomCode">Four-character room identifier.</param>
        /// <param name="playerCount">Current number of players in the room.</param>
        /// <param name="maxPlayers">Capacity cap. Defaults to 2 (standard 1v1 match).</param>
        /// <param name="isPrivate">Whether a password is required to join. Defaults to false.</param>
        /// <param name="pingMs">Round-trip latency in ms to the room host. 0 = unknown.</param>
        /// <param name="hostName">Display name of the room host. Defaults to empty string.</param>
        /// <param name="createdAt">UTC ticks of room creation. 0 = unknown.</param>
        public RoomEntry(string roomCode, int playerCount, int maxPlayers = 2,
                         bool isPrivate = false, int pingMs = 0, string hostName = "",
                         long createdAt = 0L)
        {
            this.roomCode    = roomCode    ?? string.Empty;
            this.playerCount = playerCount;
            this.maxPlayers  = maxPlayers > 0 ? maxPlayers : 2;
            this.isPrivate   = isPrivate;
            this.pingMs      = Mathf.Max(0, pingMs);
            this.hostName    = hostName ?? string.Empty;
            this.createdAt   = createdAt < 0L ? 0L : createdAt;
        }

        /// <summary>
        /// Returns true when the room cannot accept any more players
        /// (<see cref="playerCount"/> &gt;= <see cref="maxPlayers"/>).
        /// </summary>
        public bool IsFull => maxPlayers > 0 && playerCount >= maxPlayers;

        /// <summary>
        /// Number of open slots in the room.
        /// Returns 0 when the room is full, when <see cref="maxPlayers"/> is not
        /// configured (&lt;= 0), or when <see cref="playerCount"/> exceeds capacity.
        /// Never negative.
        /// </summary>
        public int SlotsRemaining
        {
            get
            {
                if (maxPlayers <= 0 || IsFull) return 0;
                return Mathf.Max(0, maxPlayers - playerCount);
            }
        }
    }

    // ── RoomListSO ────────────────────────────────────────────────────────────

    /// <summary>
    /// Runtime ScriptableObject that holds the current list of available network
    /// rooms. Updated by the network layer (adapter / bridge); read by
    /// <see cref="RoomListUI"/> to populate the room browser.
    ///
    /// Mutation is only allowed through the designated mutators
    /// (<see cref="SetRooms"/> and <see cref="Clear"/>), which also fire
    /// the <see cref="_onRoomsUpdated"/> SO event channel so that UI components
    /// react without polling.
    ///
    /// ARCHITECTURE RULES:
    ///   • Lives in BattleRobots.Core — safe to reference from UI.
    ///   • No Physics references.
    ///   • No heap allocations in Update (no Update defined here).
    ///   • Read-only at runtime except through designated mutators.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Network ▶ RoomListSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/RoomListSO", order = 1)]
    public sealed class RoomListSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel")]
        [Tooltip("Raised after SetRooms() or Clear() mutates the room list. " +
                 "Wire a VoidGameEventListener to RoomListUI.OnRoomsUpdated().")]
        [SerializeField] private VoidGameEvent _onRoomsUpdated;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Pre-allocated; cleared and repopulated by SetRooms / Clear.
        private readonly List<RoomEntry> _rooms = new List<RoomEntry>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Read-only view of the current room list.
        /// Never mutate the underlying list — call <see cref="SetRooms"/> instead.
        /// </summary>
        public IReadOnlyList<RoomEntry> Rooms => _rooms;

        /// <summary>Number of rooms currently in the list.</summary>
        public int Count => _rooms.Count;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Replace the room list with <paramref name="rooms"/> and fire the
        /// <see cref="_onRoomsUpdated"/> event channel.
        /// Passing <c>null</c> is equivalent to calling <see cref="Clear"/>.
        /// </summary>
        public void SetRooms(List<RoomEntry> rooms)
        {
            _rooms.Clear();
            if (rooms != null)
                _rooms.AddRange(rooms);

            _onRoomsUpdated?.Raise();
        }

        /// <summary>
        /// Remove all entries from the room list and fire the
        /// <see cref="_onRoomsUpdated"/> event channel.
        /// </summary>
        public void Clear()
        {
            _rooms.Clear();
            _onRoomsUpdated?.Raise();
        }

        /// <summary>
        /// Returns a snapshot of rooms whose <see cref="RoomEntry.roomCode"/> starts
        /// with <paramref name="prefix"/> (case-insensitive), in their original order.
        ///
        /// A null or empty <paramref name="prefix"/> returns the full room list
        /// without allocating a new collection.
        ///
        /// Backward-compatible wrapper around <see cref="GetSortedFilteredRooms"/>.
        /// Prefer that method when a sort order is needed.
        /// </summary>
        public IReadOnlyList<RoomEntry> GetFilteredRooms(string prefix)
        {
            return GetSortedFilteredRooms(prefix, RoomSortMode.None);
        }

        /// <summary>
        /// Returns a snapshot of rooms filtered by <paramref name="prefix"/> and
        /// ordered according to <paramref name="sort"/>.
        ///
        /// Filtering is applied first; sorting is applied to the filtered results.
        /// A null or empty <paramref name="prefix"/> skips the filter step.
        ///
        /// Allocations are acceptable here — this method is called only on user
        /// interaction (keystrokes, button clicks), never in Update / FixedUpdate.
        /// </summary>
        public IReadOnlyList<RoomEntry> GetSortedFilteredRooms(string prefix, RoomSortMode sort)
        {
            // ── 1. Filter ─────────────────────────────────────────────────────

            List<RoomEntry> result;

            if (string.IsNullOrEmpty(prefix))
            {
                // No filter: copy the full list so we can sort in-place safely.
                result = new List<RoomEntry>(_rooms);
            }
            else
            {
                result = new List<RoomEntry>(_rooms.Count);
                for (int i = 0; i < _rooms.Count; i++)
                {
                    RoomEntry entry = _rooms[i];
                    if (!string.IsNullOrEmpty(entry.roomCode) &&
                        entry.roomCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(entry);
                    }
                }
            }

            // ── 2. Sort ───────────────────────────────────────────────────────

            switch (sort)
            {
                case RoomSortMode.ByPlayerCountDesc:
                    result.Sort(CompareByPlayerCountDesc);
                    break;

                case RoomSortMode.ByRoomCodeAsc:
                    result.Sort(CompareByRoomCodeAsc);
                    break;

                // RoomSortMode.None — preserve insertion order.
                default:
                    break;
            }

            return result;
        }

        // ── Sort comparers (static — no closure allocation) ───────────────────

        private static int CompareByPlayerCountDesc(RoomEntry a, RoomEntry b)
        {
            // Descending: larger count first.
            return b.playerCount.CompareTo(a.playerCount);
        }

        private static int CompareByRoomCodeAsc(RoomEntry a, RoomEntry b)
        {
            return string.Compare(a.roomCode, b.roomCode, StringComparison.OrdinalIgnoreCase);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
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

        /// <summary>
        /// Convenience constructor for use in tests and the stub adapter.
        /// </summary>
        /// <param name="roomCode">Four-character room identifier.</param>
        /// <param name="playerCount">Current number of players in the room.</param>
        /// <param name="maxPlayers">Capacity cap. Defaults to 2 (standard 1v1 match).</param>
        /// <param name="isPrivate">Whether a password is required to join. Defaults to false.</param>
        public RoomEntry(string roomCode, int playerCount, int maxPlayers = 2, bool isPrivate = false)
        {
            this.roomCode    = roomCode    ?? string.Empty;
            this.playerCount = playerCount;
            this.maxPlayers  = maxPlayers > 0 ? maxPlayers : 2;
            this.isPrivate   = isPrivate;
        }

        /// <summary>
        /// Returns true when the room cannot accept any more players
        /// (<see cref="playerCount"/> &gt;= <see cref="maxPlayers"/>).
        /// </summary>
        public bool IsFull => maxPlayers > 0 && playerCount >= maxPlayers;
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
        /// with <paramref name="prefix"/> (case-insensitive).
        ///
        /// A null or empty <paramref name="prefix"/> returns the full room list
        /// without allocating a new collection.
        ///
        /// Called by <c>RoomListUI.Rebuild</c> when a search prefix is active.
        /// Allocations here are acceptable: Rebuild runs only on user interaction,
        /// never in Update / FixedUpdate.
        /// </summary>
        public IReadOnlyList<RoomEntry> GetFilteredRooms(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return _rooms;

            var filtered = new List<RoomEntry>(_rooms.Count);
            for (int i = 0; i < _rooms.Count; i++)
            {
                RoomEntry entry = _rooms[i];
                if (!string.IsNullOrEmpty(entry.roomCode) &&
                    entry.roomCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(entry);
                }
            }
            return filtered;
        }
    }
}

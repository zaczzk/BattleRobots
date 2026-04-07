using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks which rooms the player has recently joined,
    /// stored newest-first in a ring-buffer capped at <see cref="MaxCapacity"/> entries
    /// (default 10).
    ///
    /// Changes are persisted immediately to the SaveSystem so they survive app restarts
    /// without requiring an explicit save trigger — matching the pattern used by
    /// <see cref="FavouriteRoomsSO"/>.
    ///
    /// Lifecycle:
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadFromData"/> at startup
    ///      with <see cref="SaveData.recentRoomCodes"/>.
    ///   2. <see cref="NetworkEventBridge"/> calls <see cref="RecordVisit"/> each time a
    ///      room is successfully joined (wired inside RegisterAdapterCallbacks).
    ///   3. <see cref="RecentRoomsUI"/> listens to <see cref="_onRecentRoomsChanged"/>
    ///      and rebuilds its list on every change.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • SO asset is immutable at runtime; all mutation via designated mutators.
    ///   • <see cref="Recent"/> is O(n) indexed but the list is always ≤ 10 entries.
    ///
    /// Create via: Assets ▶ Create ▶ BattleRobots ▶ Network ▶ RecentRoomsSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/RecentRoomsSO", order = 3)]
    public sealed class RecentRoomsSO : ScriptableObject
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Maximum number of entries kept in the ring-buffer.</summary>
        public const int MaxCapacity = 10;

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel")]
        [Tooltip("Raised after any RecordVisit, Clear, or LoadFromData call. " +
                 "Wire RecentRoomsUI to refresh whenever the list changes.")]
        [SerializeField] private VoidGameEvent _onRecentRoomsChanged;

        // ── Runtime state (transient — not serialised to the SO asset) ────────

        // Newest-first ordered list; source-of-truth for BuildData().
        private readonly List<string> _recent = new List<string>(MaxCapacity);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Read-only view of recent room codes, newest-first.
        /// Always contains at most <see cref="MaxCapacity"/> entries.
        /// </summary>
        public IReadOnlyList<string> Recent => _recent;

        /// <summary>Number of entries currently in the recent list.</summary>
        public int Count => _recent.Count;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a room visit: moves <paramref name="roomCode"/> to the front
        /// of the list (dedup), then trims to <see cref="MaxCapacity"/>.
        /// No-ops silently if the code is null or empty.
        /// Persists via <see cref="SaveSystem"/> and fires the change event.
        /// </summary>
        public void RecordVisit(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) return;

            // Remove any existing occurrence so the code moves to the front.
            _recent.Remove(roomCode);

            // Insert at the beginning (newest-first).
            _recent.Insert(0, roomCode);

            // Trim to capacity.
            while (_recent.Count > MaxCapacity)
                _recent.RemoveAt(_recent.Count - 1);

            _onRecentRoomsChanged?.Raise();
            PersistRecent();
        }

        /// <summary>
        /// Clears the recent-rooms list.
        /// Persists via <see cref="SaveSystem"/> and fires the change event.
        /// No-op if the list is already empty.
        /// </summary>
        public void Clear()
        {
            if (_recent.Count == 0) return;

            _recent.Clear();
            _onRecentRoomsChanged?.Raise();
            PersistRecent();
        }

        // ── Save / Load bridge ────────────────────────────────────────────────

        /// <summary>
        /// Populates runtime state from a deserialized <see cref="List{T}"/> of room codes.
        /// Call from <see cref="GameBootstrapper"/> immediately after <see cref="SaveSystem.Load"/>.
        /// Null/empty codes are skipped. The list is trimmed to <see cref="MaxCapacity"/>.
        /// Does NOT fire <see cref="_onRecentRoomsChanged"/> — listeners may not be ready yet.
        /// </summary>
        public void LoadFromData(List<string> codes)
        {
            _recent.Clear();

            if (codes == null) return;

            for (int i = 0; i < codes.Count && _recent.Count < MaxCapacity; i++)
            {
                string code = codes[i];
                if (!string.IsNullOrEmpty(code))
                    _recent.Add(code);
            }
        }

        /// <summary>
        /// Snapshots the current recent list into a new <see cref="List{T}"/> ready
        /// to be stored in <see cref="SaveData.recentRoomCodes"/>.
        /// Allocates — only call from save paths, never from the hot path.
        /// </summary>
        public List<string> BuildData()
        {
            return new List<string>(_recent);
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void PersistRecent()
        {
            SaveData save = SaveSystem.Load();
            save.recentRoomCodes = BuildData();
            SaveSystem.Save(save);
        }
    }
}

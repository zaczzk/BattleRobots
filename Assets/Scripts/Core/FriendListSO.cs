using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject managing the player's friend and block lists.
    ///
    /// ── Invariant ─────────────────────────────────────────────────────────────
    ///   A player name appears in at most one list: friends XOR blocked.
    ///   <see cref="BlockPlayer"/> automatically removes from friends if present.
    ///   <see cref="AddFriend"/> is silently rejected when the target is blocked.
    ///
    /// ── Persistence ───────────────────────────────────────────────────────────
    ///   Auto-persists on every mutation via <see cref="SaveSystem"/> (same pattern
    ///   as <see cref="FavouriteRoomsSO"/>). <see cref="LoadFromData"/> is called
    ///   by <see cref="GameBootstrapper"/> at startup.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics/UI references.
    ///   • <see cref="IsFriend"/> and <see cref="IsBlocked"/> are O(1) via HashSet.
    ///   • No heap allocations in membership queries.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Network ▶ FriendListSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/FriendListSO", order = 3)]
    public sealed class FriendListSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — Out")]
        [Tooltip("Raised after any friends list mutation (AddFriend, RemoveFriend, BlockPlayer when friend removed).")]
        [SerializeField] private VoidGameEvent _onFriendsChanged;

        [Tooltip("Raised after any blocked list mutation (BlockPlayer, UnblockPlayer).")]
        [SerializeField] private VoidGameEvent _onBlockedChanged;

        // ── Runtime state (transient) ─────────────────────────────────────────

        private readonly List<string>    _friends        = new List<string>();
        private readonly HashSet<string> _friendSet      =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        private readonly List<string>    _blocked        = new List<string>();
        private readonly HashSet<string> _blockedSet     =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        // ── Public read-only views ────────────────────────────────────────────

        /// <summary>Read-only ordered view of the friends list.</summary>
        public IReadOnlyList<string> Friends => _friends;

        /// <summary>Read-only ordered view of the blocked list.</summary>
        public IReadOnlyList<string> Blocked => _blocked;

        /// <summary>Number of friends.</summary>
        public int FriendCount  => _friends.Count;

        /// <summary>Number of blocked players.</summary>
        public int BlockedCount => _blocked.Count;

        // ── Membership queries (O(1), no alloc) ──────────────────────────────

        /// <summary>Returns true if <paramref name="playerName"/> is on the friends list.</summary>
        public bool IsFriend(string playerName) =>
            !string.IsNullOrWhiteSpace(playerName) && _friendSet.Contains(playerName.Trim());

        /// <summary>Returns true if <paramref name="playerName"/> is on the blocked list.</summary>
        public bool IsBlocked(string playerName) =>
            !string.IsNullOrWhiteSpace(playerName) && _blockedSet.Contains(playerName.Trim());

        // ── Friend mutators ───────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="playerName"/> to the friends list.
        /// No-ops silently when:
        ///   • name is null/empty/whitespace
        ///   • name is already a friend
        ///   • name is on the blocked list (blocked takes precedence)
        /// Fires <see cref="_onFriendsChanged"/> and persists.
        /// </summary>
        public void AddFriend(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;
            string name = playerName.Trim();
            if (_friendSet.Contains(name))  return; // already friend
            if (_blockedSet.Contains(name)) return; // blocked — cannot add as friend

            _friends.Add(name);
            _friendSet.Add(name);
            _onFriendsChanged?.Raise();
            Persist();
        }

        /// <summary>
        /// Removes <paramref name="playerName"/> from the friends list.
        /// No-op if not present. Fires <see cref="_onFriendsChanged"/> and persists.
        /// </summary>
        public void RemoveFriend(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;
            string name = playerName.Trim();
            if (!_friendSet.Remove(name)) return;

            _friends.Remove(name);
            _onFriendsChanged?.Raise();
            Persist();
        }

        // ── Block mutators ────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="playerName"/> to the blocked list.
        /// If they were a friend, they are automatically removed from friends first.
        /// No-op if already blocked. Fires appropriate event channels and persists.
        /// </summary>
        public void BlockPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;
            string name = playerName.Trim();
            if (_blockedSet.Contains(name)) return; // already blocked

            // Remove from friends if present — mutual-exclusion invariant.
            bool wasFriend = _friendSet.Remove(name);
            if (wasFriend)
            {
                _friends.Remove(name);
                _onFriendsChanged?.Raise();
            }

            _blocked.Add(name);
            _blockedSet.Add(name);
            _onBlockedChanged?.Raise();
            Persist();
        }

        /// <summary>
        /// Removes <paramref name="playerName"/> from the blocked list.
        /// Does NOT automatically re-add them as a friend — caller decides.
        /// No-op if not blocked. Fires <see cref="_onBlockedChanged"/> and persists.
        /// </summary>
        public void UnblockPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;
            string name = playerName.Trim();
            if (!_blockedSet.Remove(name)) return;

            _blocked.Remove(name);
            _onBlockedChanged?.Raise();
            Persist();
        }

        /// <summary>
        /// Clears both friends and blocked lists.
        /// Fires both event channels and persists. No-op if both lists already empty.
        /// </summary>
        public void ClearAll()
        {
            bool hadFriends  = _friends.Count  > 0;
            bool hadBlocked  = _blocked.Count  > 0;
            if (!hadFriends && !hadBlocked) return;

            _friends.Clear();
            _friendSet.Clear();
            _blocked.Clear();
            _blockedSet.Clear();

            if (hadFriends) _onFriendsChanged?.Raise();
            if (hadBlocked) _onBlockedChanged?.Raise();
            Persist();
        }

        // ── Persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Populates runtime state from a deserialized <see cref="FriendListData"/> snapshot.
        /// Call from <see cref="GameBootstrapper"/> on startup.
        /// Duplicate/blank names are silently skipped.
        /// Does NOT fire event channels — listeners may not be ready yet.
        /// </summary>
        public void LoadFromData(FriendListData data)
        {
            _friends.Clear();
            _friendSet.Clear();
            _blocked.Clear();
            _blockedSet.Clear();

            if (data == null) return;

            AddNames(data.friendNames, _friends, _friendSet);
            AddNames(data.blockedNames, _blocked, _blockedSet);

            // Enforce mutual-exclusion: remove from friends any name also in blocked.
            for (int i = _friends.Count - 1; i >= 0; i--)
            {
                if (_blockedSet.Contains(_friends[i]))
                {
                    _friendSet.Remove(_friends[i]);
                    _friends.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Snapshots runtime state into a <see cref="FriendListData"/> POCO
        /// for XOR-SaveSystem persistence.
        /// Allocates — only call from save paths.
        /// </summary>
        public FriendListData BuildData()
        {
            return new FriendListData
            {
                friendNames  = new List<string>(_friends),
                blockedNames = new List<string>(_blocked),
            };
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void Persist()
        {
            SaveData save = SaveSystem.Load();
            save.friendList = BuildData();
            SaveSystem.Save(save);
        }

        private static void AddNames(
            List<string> source,
            List<string> targetList,
            HashSet<string> targetSet)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                string n = source[i];
                if (string.IsNullOrWhiteSpace(n)) continue;
                string trimmed = n.Trim();
                if (targetSet.Add(trimmed))
                    targetList.Add(trimmed);
            }
        }
    }
}

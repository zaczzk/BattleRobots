using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI panel displaying the player's friends and blocked list.
    ///
    /// ── Tabs ──────────────────────────────────────────────────────────────────
    ///   Friends tab  : lists all friends with Unfriend + Block buttons per row.
    ///   Blocked tab  : lists all blocked players with Unblock button per row.
    ///
    /// ── Reactivity ────────────────────────────────────────────────────────────
    ///   Add VoidGameEventListeners on this GO:
    ///     • FriendListSO._onFriendsChanged  → ShowFriendsTab() (or Rebuild())
    ///     • FriendListSO._onBlockedChanged  → ShowBlockedTab() (or Rebuild())
    ///
    /// ── Add-friend flow ───────────────────────────────────────────────────────
    ///   Wire _addFriendInput (InputField) + _addFriendButton (Button) in the
    ///   Inspector. HandleAddFriend() is also public so tests can invoke it directly.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace; must NOT reference BattleRobots.Physics.
    ///   • Rebuild() clears + re-instantiates rows — called from OnEnable and
    ///     in response to list-change events. No allocations in Update.
    ///   • _friendEntryPrefab must have a FriendEntryUI component.
    ///
    /// Wire in Inspector:
    ///   _friendList        → FriendListSO
    ///   _friendEntryPrefab → FriendEntryUI prefab
    ///   _contentParent     → ScrollRect content Transform (friends tab)
    ///   _blockedContentParent → ScrollRect content Transform (blocked tab)
    ///   _friendsPanel      → friends tab root GO
    ///   _blockedPanel      → blocked tab root GO
    ///   _emptyFriendsLabel → Text shown when friends list is empty
    ///   _emptyBlockedLabel → Text shown when blocked list is empty
    ///   _addFriendInput    → InputField for typing a player name
    ///   _addFriendButton   → Button that calls HandleAddFriend
    ///   _addFriendFeedback → Text label for success / error feedback
    /// </summary>
    public sealed class FriendListUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [SerializeField] private FriendListSO _friendList;

        [Header("Prefabs")]
        [SerializeField] private FriendEntryUI _friendEntryPrefab;

        [Header("Friends Tab")]
        [SerializeField] private GameObject _friendsPanel;
        [SerializeField] private Transform  _contentParent;
        [SerializeField] private Text       _emptyFriendsLabel;

        [Header("Blocked Tab")]
        [SerializeField] private GameObject _blockedPanel;
        [SerializeField] private Transform  _blockedContentParent;
        [SerializeField] private Text       _emptyBlockedLabel;

        [Header("Add Friend")]
        [SerializeField] private InputField _addFriendInput;
        [SerializeField] private Button     _addFriendButton;
        [SerializeField] private Text       _addFriendFeedback;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _showingFriends = true;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>True when the friends tab is active; false when blocked tab is active.</summary>
        public bool IsShowingFriends => _showingFriends;

        /// <summary>Switches to the friends tab and rebuilds the list.</summary>
        public void ShowFriendsTab()
        {
            _showingFriends = true;
            SetPanelsActive();
            RebuildFriends();
        }

        /// <summary>Switches to the blocked tab and rebuilds the list.</summary>
        public void ShowBlockedTab()
        {
            _showingFriends = false;
            SetPanelsActive();
            RebuildBlocked();
        }

        /// <summary>
        /// Rebuilds whichever tab is currently visible.
        /// Safe to call from VoidGameEventListener response.
        /// </summary>
        public void Rebuild()
        {
            if (_showingFriends) RebuildFriends();
            else                 RebuildBlocked();
        }

        /// <summary>
        /// Reads the add-friend input field, calls FriendListSO.AddFriend,
        /// and updates feedback text. Public so UI button can reference it directly.
        /// </summary>
        public void HandleAddFriend()
        {
            string input = _addFriendInput != null ? _addFriendInput.text : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                SetFeedback("Enter a player name.");
                return;
            }

            if (_friendList == null)
            {
                SetFeedback("Friend list not configured.");
                return;
            }

            string trimmed = input.Trim();

            if (_friendList.IsBlocked(trimmed))
            {
                SetFeedback($"'{trimmed}' is blocked. Unblock them first.");
                return;
            }

            if (_friendList.IsFriend(trimmed))
            {
                SetFeedback($"'{trimmed}' is already a friend.");
                return;
            }

            _friendList.AddFriend(trimmed);

            if (_addFriendInput != null)
                _addFriendInput.text = string.Empty;

            SetFeedback($"Added '{trimmed}' as a friend.");

            if (_showingFriends) RebuildFriends();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_addFriendButton != null)
                _addFriendButton.onClick.AddListener(HandleAddFriend);
        }

        private void OnEnable()
        {
            SetPanelsActive();
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_addFriendButton != null)
                _addFriendButton.onClick.RemoveListener(HandleAddFriend);
        }

        // ── Private rebuild helpers ───────────────────────────────────────────

        private void RebuildFriends()
        {
            ClearChildren(_contentParent);

            if (_friendList == null) return;

            bool empty = _friendList.FriendCount == 0;
            if (_emptyFriendsLabel != null)
                _emptyFriendsLabel.gameObject.SetActive(empty);

            if (empty || _friendEntryPrefab == null || _contentParent == null) return;

            for (int i = 0; i < _friendList.FriendCount; i++)
            {
                string name  = _friendList.Friends[i];
                FriendEntryUI entry = Instantiate(_friendEntryPrefab, _contentParent);
                entry.SetupAsFriend(name, _friendList);
            }
        }

        private void RebuildBlocked()
        {
            ClearChildren(_blockedContentParent);

            if (_friendList == null) return;

            bool empty = _friendList.BlockedCount == 0;
            if (_emptyBlockedLabel != null)
                _emptyBlockedLabel.gameObject.SetActive(empty);

            if (empty || _friendEntryPrefab == null || _blockedContentParent == null) return;

            for (int i = 0; i < _friendList.BlockedCount; i++)
            {
                string name  = _friendList.Blocked[i];
                FriendEntryUI entry = Instantiate(_friendEntryPrefab, _blockedContentParent);
                entry.SetupAsBlocked(name, _friendList);
            }
        }

        private void SetPanelsActive()
        {
            if (_friendsPanel != null)  _friendsPanel.SetActive(_showingFriends);
            if (_blockedPanel != null)  _blockedPanel.SetActive(!_showingFriends);
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private void SetFeedback(string message)
        {
            if (_addFriendFeedback != null)
                _addFriendFeedback.text = message;
        }
    }
}

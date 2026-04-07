using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Single-row UI component representing one entry in the friend or blocked list.
    ///
    /// Instantiated and configured by <see cref="FriendListUI.Rebuild"/>.
    /// Buttons call back into <see cref="FriendListSO"/> directly — no cross-namespace
    /// dependency on Physics.
    ///
    /// Wire in Prefab Inspector:
    ///   _nameLabel     → Text showing the player name
    ///   _unfriendButton → visible only when showing a friend entry
    ///   _blockButton   → visible only when showing a friend entry
    ///   _unblockButton → visible only when showing a blocked entry
    /// </summary>
    public sealed class FriendEntryUI : MonoBehaviour
    {
        [SerializeField] private Text   _nameLabel;
        [SerializeField] private Button _unfriendButton;
        [SerializeField] private Button _blockButton;
        [SerializeField] private Button _unblockButton;

        private FriendListSO _friendList;
        private string       _playerName;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the row to display a <em>friend</em> entry.
        /// Shows Unfriend + Block buttons; hides Unblock.
        /// </summary>
        public void SetupAsFriend(string playerName, FriendListSO friendList)
        {
            _playerName = playerName;
            _friendList  = friendList;

            if (_nameLabel != null) _nameLabel.text = playerName;

            SetVisible(_unfriendButton, true);
            SetVisible(_blockButton,    true);
            SetVisible(_unblockButton,  false);
        }

        /// <summary>
        /// Configures the row to display a <em>blocked</em> entry.
        /// Shows Unblock button; hides Unfriend + Block.
        /// </summary>
        public void SetupAsBlocked(string playerName, FriendListSO friendList)
        {
            _playerName = playerName;
            _friendList  = friendList;

            if (_nameLabel != null) _nameLabel.text = playerName;

            SetVisible(_unfriendButton, false);
            SetVisible(_blockButton,    false);
            SetVisible(_unblockButton,  true);
        }

        /// <summary>The display name this row was configured with.</summary>
        public string PlayerName => _playerName;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_unfriendButton != null)
                _unfriendButton.onClick.AddListener(HandleUnfriend);

            if (_blockButton != null)
                _blockButton.onClick.AddListener(HandleBlock);

            if (_unblockButton != null)
                _unblockButton.onClick.AddListener(HandleUnblock);
        }

        private void OnDestroy()
        {
            if (_unfriendButton != null) _unfriendButton.onClick.RemoveListener(HandleUnfriend);
            if (_blockButton    != null) _blockButton.onClick.RemoveListener(HandleBlock);
            if (_unblockButton  != null) _unblockButton.onClick.RemoveListener(HandleUnblock);
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void HandleUnfriend()
        {
            if (_friendList == null || string.IsNullOrEmpty(_playerName)) return;
            _friendList.RemoveFriend(_playerName);
        }

        private void HandleBlock()
        {
            if (_friendList == null || string.IsNullOrEmpty(_playerName)) return;
            _friendList.BlockPlayer(_playerName);
        }

        private void HandleUnblock()
        {
            if (_friendList == null || string.IsNullOrEmpty(_playerName)) return;
            _friendList.UnblockPlayer(_playerName);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }
}

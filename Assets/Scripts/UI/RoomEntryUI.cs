using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Prefab row component for a single room in the <see cref="RoomListUI"/> browser.
    ///
    /// Data is pushed in via <see cref="Setup(RoomEntry, Action{string})"/>; this
    /// component holds no direct reference to <see cref="RoomListSO"/> or any network
    /// adapter — keeping it decoupled from the data source.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No per-frame cost — no Update / FixedUpdate defined.
    ///   • Allocation only in Awake (AddListener) and Setup (closure captured action).
    ///
    /// Inspector wiring:
    ///   □ _roomCodeLabel    → Text displaying the 4-char room code
    ///   □ _playerCountLabel → Text displaying "N/MAX" player capacity
    ///   □ _fullBadge        → (optional) GameObject shown only when the room is full
    ///   □ _joinButton       → Button that triggers the join action (disabled when full)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEntryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Labels")]
        [Tooltip("Text component that shows the room code.")]
        [SerializeField] private Text _roomCodeLabel;

        [Tooltip("Text component that shows the player count as 'N/MAX'.")]
        [SerializeField] private Text _playerCountLabel;

        [Tooltip("(Optional) GameObject shown when the room has reached capacity. " +
                 "Use a Text child labelled 'FULL' or a coloured overlay.")]
        [SerializeField] private GameObject _fullBadge;

        [Tooltip("(Optional) GameObject shown when the room is private (requires a password). " +
                 "Use a lock icon or Text labelled 'PRIVATE'.")]
        [SerializeField] private GameObject _privateBadge;

        [Header("Action")]
        [Tooltip("Button the user presses to join this room. Disabled when the room is full.")]
        [SerializeField] private Button _joinButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Action<string> _onJoin;
        private string         _roomCode = string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_joinButton != null)
                _joinButton.onClick.AddListener(HandleJoinClicked);
        }

        private void OnDestroy()
        {
            if (_joinButton != null)
                _joinButton.onClick.RemoveListener(HandleJoinClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Configure this row for the given <paramref name="entry"/>.
        /// <paramref name="onJoin"/> is invoked with the room code when the
        /// Join button is pressed. Call once immediately after instantiation.
        /// </summary>
        public void Setup(RoomEntry entry, Action<string> onJoin)
        {
            _roomCode = entry.roomCode ?? string.Empty;
            _onJoin   = onJoin;

            if (_roomCodeLabel != null)
                _roomCodeLabel.text = _roomCode;

            if (_playerCountLabel != null)
            {
                // Format: "1/2" when capacity is known; fall back to plain count.
                _playerCountLabel.text = entry.maxPlayers > 0
                    ? $"{entry.playerCount}/{entry.maxPlayers}"
                    : entry.playerCount.ToString();
            }

            bool isFull    = entry.IsFull;
            bool isPrivate = entry.isPrivate;

            if (_fullBadge != null)
                _fullBadge.SetActive(isFull);

            if (_privateBadge != null)
                _privateBadge.SetActive(isPrivate);

            if (_joinButton != null)
                _joinButton.interactable = !string.IsNullOrEmpty(_roomCode) && !isFull;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void HandleJoinClicked()
        {
            _onJoin?.Invoke(_roomCode);
        }
    }
}

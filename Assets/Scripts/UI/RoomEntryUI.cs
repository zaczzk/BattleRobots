using System;
using System.Collections;
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
    ///   □ _roomCodeLabel        → Text displaying the 4-char room code
    ///   □ _playerCountLabel     → Text displaying "N/MAX" player capacity
    ///   □ _fullBadge            → (optional) GameObject shown only when the room is full
    ///   □ _privateBadge         → (optional) GameObject shown when the room is private
    ///   □ _joinButton           → Button that triggers the join action (disabled when full)
    ///   □ _favouriteButton      → (optional) FavouriteButtonUI child component
    ///   □ _copyButton           → (optional) Button that copies room code to clipboard
    ///   □ _copiedFeedbackLabel  → (optional) Text showing "Copied!" for 1.5 s after copy
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

        [Tooltip("(Optional) Text component showing how many slots are still open, e.g. '1 left'. " +
                 "Hidden (empty string) when the room is full or maxPlayers is not configured.")]
        [SerializeField] private Text _slotsRemainingLabel;

        [Tooltip("(Optional) GameObject shown when the room has reached capacity. " +
                 "Use a Text child labelled 'FULL' or a coloured overlay.")]
        [SerializeField] private GameObject _fullBadge;

        [Tooltip("(Optional) GameObject shown when the room is private (requires a password). " +
                 "Use a lock icon or Text labelled 'PRIVATE'.")]
        [SerializeField] private GameObject _privateBadge;

        [Header("Action")]
        [Tooltip("Button the user presses to join this room. Disabled when the room is full.")]
        [SerializeField] private Button _joinButton;

        [Header("Favourite (optional)")]
        [Tooltip("(Optional) FavouriteButtonUI child component. " +
                 "Setup() will wire it automatically when a FavouriteRoomsSO is provided.")]
        [SerializeField] private FavouriteButtonUI _favouriteButton;

        [Header("Copy to Clipboard (optional)")]
        [Tooltip("(Optional) Button that copies the room code to the system clipboard.")]
        [SerializeField] private Button _copyButton;

        [Tooltip("(Optional) Text label that briefly shows 'Copied!' for 1.5 s after the " +
                 "copy button is pressed, then reverts to empty.")]
        [SerializeField] private Text _copiedFeedbackLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Action<string> _onJoin;
        private string         _roomCode = string.Empty;

        /// <summary>
        /// The room code most recently copied to the system clipboard via this button.
        /// Empty string until <see cref="HandleCopyClicked"/> has been invoked at least once.
        /// Primarily useful for testing.
        /// </summary>
        public string LastCopiedCode { get; private set; } = string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_joinButton != null)
                _joinButton.onClick.AddListener(HandleJoinClicked);

            if (_copyButton != null)
                _copyButton.onClick.AddListener(HandleCopyClicked);
        }

        private void OnDestroy()
        {
            if (_joinButton != null)
                _joinButton.onClick.RemoveListener(HandleJoinClicked);

            if (_copyButton != null)
                _copyButton.onClick.RemoveListener(HandleCopyClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Configure this row for the given <paramref name="entry"/>.
        /// <paramref name="onJoin"/> is invoked with the room code when the
        /// Join button is pressed. Call once immediately after instantiation.
        /// </summary>
        public void Setup(RoomEntry entry, Action<string> onJoin)
            => Setup(entry, onJoin, null);

        /// <summary>
        /// Configure this row for the given <paramref name="entry"/> with optional
        /// favourite-room support.
        /// <paramref name="onJoin"/> is invoked with the room code when the Join button
        /// is pressed. <paramref name="favourites"/> wires the star button — pass
        /// <c>null</c> to hide/disable favourite functionality.
        /// </summary>
        public void Setup(RoomEntry entry, Action<string> onJoin, BattleRobots.Core.FavouriteRoomsSO favourites)
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

            // Slots-remaining label: show "N left" only when room is not full and
            // maxPlayers is configured. Empty string hides the label gracefully.
            if (_slotsRemainingLabel != null)
            {
                _slotsRemainingLabel.text = entry.SlotsRemaining > 0
                    ? $"{entry.SlotsRemaining} left"
                    : string.Empty;
            }

            if (_fullBadge != null)
                _fullBadge.SetActive(isFull);

            if (_privateBadge != null)
                _privateBadge.SetActive(isPrivate);

            if (_joinButton != null)
                _joinButton.interactable = !string.IsNullOrEmpty(_roomCode) && !isFull;

            if (_copyButton != null)
                _copyButton.interactable = !string.IsNullOrEmpty(_roomCode);

            // Wire the favourite star button if one is present and a SO was provided.
            if (_favouriteButton != null)
            {
                _favouriteButton.gameObject.SetActive(favourites != null);
                if (favourites != null)
                    _favouriteButton.Setup(favourites, _roomCode);
            }
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void HandleJoinClicked()
        {
            _onJoin?.Invoke(_roomCode);
        }

        /// <summary>
        /// Copies the current room code to the system clipboard and briefly shows
        /// "Copied!" in the feedback label (if wired). Safe to call even when no
        /// room code is set — no-ops in that case.
        /// </summary>
        public void HandleCopyClicked()
        {
            if (string.IsNullOrEmpty(_roomCode)) return;

            GUIUtility.systemCopyBuffer = _roomCode;
            LastCopiedCode = _roomCode;

            if (_copiedFeedbackLabel != null)
            {
                _copiedFeedbackLabel.text = "Copied!";
                // Stop any in-progress clear so only one runs at a time.
                StopCoroutine(nameof(ClearCopiedFeedback));
                StartCoroutine(nameof(ClearCopiedFeedback));
            }
        }

        private IEnumerator ClearCopiedFeedback()
        {
            yield return new WaitForSeconds(1.5f);
            if (_copiedFeedbackLabel != null)
                _copiedFeedbackLabel.text = string.Empty;
        }
    }
}

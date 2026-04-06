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
    ///   □ _roomCodeLabel   → Text displaying the 4-char room code
    ///   □ _playerCountLabel → Text displaying "N / MAX players" or similar
    ///   □ _joinButton       → Button that triggers the join action
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEntryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Labels")]
        [Tooltip("Text component that shows the room code.")]
        [SerializeField] private Text _roomCodeLabel;

        [Tooltip("Text component that shows the player count.")]
        [SerializeField] private Text _playerCountLabel;

        [Header("Action")]
        [Tooltip("Button the user presses to join this room.")]
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
                _playerCountLabel.text = $"{entry.playerCount} player{(entry.playerCount == 1 ? "" : "s")}";

            if (_joinButton != null)
                _joinButton.interactable = !string.IsNullOrEmpty(_roomCode);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void HandleJoinClicked()
        {
            _onJoin?.Invoke(_roomCode);
        }
    }
}

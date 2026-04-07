using System;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Prefab row component for a single entry in the <see cref="RecentRoomsUI"/> list.
    ///
    /// Data is pushed in via <see cref="Setup"/>; this component holds no reference to
    /// <see cref="BattleRobots.Core.RecentRoomsSO"/> — keeping it decoupled from the
    /// data source.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics or Core references beyond Setup params.
    ///   • No per-frame cost — no Update / FixedUpdate.
    ///
    /// Inspector wiring:
    ///   □ _roomCodeLabel → Text displaying the room code
    ///   □ _joinButton    → Button that triggers the join action
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RecentRoomEntryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Text component that displays the room code.")]
        [SerializeField] private Text _roomCodeLabel;

        [Tooltip("Button the user presses to re-join this room.")]
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
        /// Configure this row for the given <paramref name="roomCode"/>.
        /// <paramref name="onJoin"/> is invoked with the room code when the Join button
        /// is pressed. Call once immediately after instantiation.
        /// </summary>
        public void Setup(string roomCode, Action<string> onJoin)
        {
            _roomCode = roomCode ?? string.Empty;
            _onJoin   = onJoin;

            if (_roomCodeLabel != null)
                _roomCodeLabel.text = _roomCode;

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

using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Full-screen overlay displayed when the local player is kicked from a room
    /// by the host.
    ///
    /// Driven by a <see cref="StringGameEvent"/> channel raised by
    /// <see cref="NetworkEventBridge"/> when the adapter fires
    /// <c>INetworkAdapter.OnPlayerKicked</c>.
    ///
    /// Wiring (Inspector):
    ///   1. Create a panel GameObject (semi-transparent overlay) with a Text label
    ///      and an optional close Button as children.
    ///   2. Assign <c>_panel</c> to the root of that overlay group.
    ///   3. Assign <c>_reasonLabel</c> to the Text component (shows the kicked name).
    ///   4. (Optional) Assign <c>_closeButton</c> and wire its onClick to
    ///      <see cref="Hide"/>.
    ///   5. Add a <see cref="StringGameEventListener"/> component on the same
    ///      GameObject, set its Event to the <c>_onPlayerKickedChannel</c> SO,
    ///      and wire its Response to <see cref="ShowKicked"/>.
    ///
    /// Architecture notes:
    ///   • Namespace <c>BattleRobots.UI</c> — no Physics dependencies.
    ///   • No Update / FixedUpdate — state driven purely by event callbacks.
    ///   • All UI component references null-guarded to survive stripped Inspector wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KickedUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [Tooltip("Root GameObject of the kicked overlay. Hidden by default; shown on kick.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("(Optional) Text label displaying the kicked player's name or a message. " +
                 "If left blank the overlay still shows without text.")]
        [SerializeField] private Text _reasonLabel;

        [Header("Actions")]
        [Tooltip("(Optional) Button the player presses to dismiss the overlay and return " +
                 "to the lobby. Wire its onClick to Hide() in the Inspector.")]
        [SerializeField] private Button _closeButton;

        // ── Observable state (testable without full UI hierarchy) ─────────────

        /// <summary>True when the kicked overlay is currently visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// The display name of the most recently kicked player (as reported by the adapter),
        /// or <see cref="string.Empty"/> if the overlay has never been shown.
        /// </summary>
        public string LastReason { get; private set; } = string.Empty;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Start hidden regardless of scene-saved state.
            _panel?.SetActive(false);
            IsVisible = false;

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Hide);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Show the kicked overlay, displaying <paramref name="kickedPlayerName"/> as the
        /// reason text.
        ///
        /// Wire via a <see cref="StringGameEventListener"/> on this GameObject to the
        /// <c>_onPlayerKickedChannel</c> <see cref="StringGameEvent"/> SO raised by
        /// <see cref="NetworkEventBridge"/> when <c>INetworkAdapter.OnPlayerKicked</c> fires.
        ///
        /// Safe to call with a null or empty <paramref name="kickedPlayerName"/>.
        /// </summary>
        public void ShowKicked(string kickedPlayerName)
        {
            LastReason = kickedPlayerName ?? string.Empty;
            IsVisible  = true;

            if (_reasonLabel != null)
            {
                _reasonLabel.text = string.IsNullOrEmpty(LastReason)
                    ? "You have been removed from the room."
                    : $"{LastReason} was kicked from the room.";
            }

            _panel?.SetActive(true);
        }

        /// <summary>
        /// Hide the kicked overlay.
        ///
        /// Wire to a close Button's onClick in the Inspector, or call programmatically
        /// when the player navigates back to the main menu.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            _panel?.SetActive(false);
        }
    }
}

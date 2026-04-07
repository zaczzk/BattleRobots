using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a room join-failure panel with the reason string surfaced by
    /// <see cref="NetworkEventBridge"/> through a <see cref="StringGameEvent"/> channel.
    ///
    /// Wiring (Inspector):
    ///   1. Create a panel GameObject (e.g. a semi-transparent overlay) with a Text label
    ///      and a close Button as children.
    ///   2. Assign <c>_panel</c> to the root of that panel group.
    ///   3. Assign <c>_reasonLabel</c> to the Text component that shows the error message.
    ///   4. (Optional) Assign <c>_closeButton</c> and wire its onClick to <see cref="Hide"/>.
    ///   5. On the ArenaManager / NetworkBridge GameObject, add a
    ///      <see cref="StringGameEventListener"/>, assign the join-failed
    ///      <see cref="StringGameEvent"/> channel, and wire its Response to
    ///      <see cref="ShowFailure"/>.
    ///
    /// Architecture notes:
    ///   • Namespace <c>BattleRobots.UI</c> — no Physics dependencies.
    ///   • No Update / FixedUpdate — panel state driven purely by event callbacks.
    ///   • All UI component references null-guarded to survive stripped Inspector wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JoinFailureUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [Tooltip("Root GameObject of the failure panel. Hidden by default; shown on failure.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text label that displays the failure reason string.")]
        [SerializeField] private Text _reasonLabel;

        [Header("Actions")]
        [Tooltip("(Optional) Button that the player presses to dismiss the panel. "
               + "Wire its onClick to Hide() in the Inspector.")]
        [SerializeField] private Button _closeButton;

        // ── Observable state (testable without UI hierarchy) ──────────────────

        /// <summary>
        /// True when the failure panel is currently visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// The most-recently displayed failure reason, or <see cref="string.Empty"/>
        /// if the panel has never been shown.
        /// </summary>
        public string LastReason { get; private set; } = string.Empty;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure the panel starts hidden regardless of its scene-saved state.
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
        /// Show the failure panel with the supplied <paramref name="reason"/> string.
        ///
        /// Wired via a <see cref="StringGameEventListener"/> in the Inspector to the
        /// <see cref="StringGameEvent"/> channel raised by <see cref="NetworkEventBridge"/>
        /// when the adapter fires its <c>OnRoomJoinFailed</c> callback.
        ///
        /// Safe to call with a null or empty <paramref name="reason"/>.
        /// </summary>
        public void ShowFailure(string reason)
        {
            LastReason = reason ?? string.Empty;
            IsVisible  = true;

            if (_reasonLabel != null)
                _reasonLabel.text = string.IsNullOrEmpty(LastReason)
                    ? "Failed to join room."
                    : LastReason;

            _panel?.SetActive(true);
        }

        /// <summary>
        /// Hide the failure panel.
        ///
        /// Can be wired to a close Button's onClick in the Inspector,
        /// or called from a <see cref="VoidGameEventListener"/> when a match begins.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            _panel?.SetActive(false);
        }
    }
}

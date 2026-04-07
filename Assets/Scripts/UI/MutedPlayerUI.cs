using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD indicator displayed when the local player has been muted (or unmuted)
    /// by the room host.
    ///
    /// Driven by two <see cref="StringGameEvent"/> channels raised by
    /// <see cref="NetworkEventBridge"/> when the adapter fires
    /// <c>INetworkAdapter.OnPlayerMuted</c> or <c>INetworkAdapter.OnPlayerUnmuted</c>.
    ///
    /// Wiring (Inspector):
    ///   1. Create a panel GameObject (semi-transparent HUD bar) with a Text label
    ///      as a child.
    ///   2. Assign <c>_panel</c> to the root of that panel group.
    ///   3. Assign <c>_statusLabel</c> to the Text component (shows the mute reason).
    ///   4. (Optional) Assign <c>_closeButton</c> and wire its onClick to
    ///      <see cref="Hide"/>.
    ///   5a. Add a <see cref="StringGameEventListener"/> for the muted channel:
    ///       set its Event to the <c>_onPlayerMutedChannel</c> SO and wire its
    ///       Response to <see cref="ShowMuted"/>.
    ///   5b. Add a second <see cref="StringGameEventListener"/> for the unmuted channel:
    ///       set its Event to the <c>_onPlayerUnmutedChannel</c> SO and wire its
    ///       Response to <see cref="ShowUnmuted"/>.
    ///
    /// Architecture notes:
    ///   • Namespace <c>BattleRobots.UI</c> — no Physics dependencies.
    ///   • No Update / FixedUpdate — state driven purely by event callbacks.
    ///   • All UI component references null-guarded to survive stripped Inspector wiring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MutedPlayerUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [Tooltip("Root GameObject of the mute-status overlay. Hidden by default.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("(Optional) Text label displaying the mute-status message.")]
        [SerializeField] private Text _statusLabel;

        [Header("Actions")]
        [Tooltip("(Optional) Button to dismiss the mute indicator. " +
                 "Wire its onClick to Hide() in the Inspector.")]
        [SerializeField] private Button _closeButton;

        // ── Observable state (testable without full UI hierarchy) ─────────────

        /// <summary>True when the mute-status overlay is currently visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// True while the local player is considered muted.
        /// Set to true by <see cref="ShowMuted"/>; cleared by <see cref="ShowUnmuted"/>
        /// or <see cref="Hide"/>.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// The most recent status message shown (the player name from the last
        /// <see cref="ShowMuted"/> or <see cref="ShowUnmuted"/> call), or
        /// <see cref="string.Empty"/> if the overlay has never been shown.
        /// </summary>
        public string LastPlayerName { get; private set; } = string.Empty;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
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
        /// Show the mute indicator, informing the local player that
        /// <paramref name="playerName"/> (usually the local player's own name, or the
        /// name reported by the host) has been muted.
        ///
        /// Wire via a <see cref="StringGameEventListener"/> on this GameObject to the
        /// <c>_onPlayerMutedChannel</c> <see cref="StringGameEvent"/> SO.
        ///
        /// Safe to call with a null or empty <paramref name="playerName"/>.
        /// </summary>
        public void ShowMuted(string playerName)
        {
            LastPlayerName = playerName ?? string.Empty;
            IsMuted        = true;
            IsVisible      = true;

            if (_statusLabel != null)
            {
                _statusLabel.text = string.IsNullOrEmpty(LastPlayerName)
                    ? "You have been muted by the host."
                    : $"{LastPlayerName} has been muted.";
            }

            _panel?.SetActive(true);
        }

        /// <summary>
        /// Update the overlay to show that <paramref name="playerName"/> has been
        /// unmuted, then hide the panel after a brief acknowledgement.
        ///
        /// Wire via a <see cref="StringGameEventListener"/> on this GameObject to the
        /// <c>_onPlayerUnmutedChannel</c> <see cref="StringGameEvent"/> SO.
        ///
        /// Safe to call with a null or empty <paramref name="playerName"/>.
        /// </summary>
        public void ShowUnmuted(string playerName)
        {
            LastPlayerName = playerName ?? string.Empty;
            IsMuted        = false;
            IsVisible      = true;

            if (_statusLabel != null)
            {
                _statusLabel.text = string.IsNullOrEmpty(LastPlayerName)
                    ? "You have been unmuted."
                    : $"{LastPlayerName} has been unmuted.";
            }

            _panel?.SetActive(true);
        }

        /// <summary>
        /// Hide the mute-status overlay.
        ///
        /// Wire to a close Button's onClick in the Inspector, or call programmatically
        /// after a brief display timer (e.g. from a Coroutine that waits 3 seconds).
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            _panel?.SetActive(false);
        }
    }
}

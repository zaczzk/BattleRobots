using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a brief on-screen toast banner whenever a notification is posted.
    ///
    /// ── Scene wiring ───────────────────────────────────────────────────────────
    ///   1. Create a UI panel (the "toast root") with a child Text label.
    ///   2. Attach this component to the toast root (or any parent GO).
    ///   3. On the same GO add a <c>StringGameEventListener</c>:
    ///        Event    → <c>NotificationSO._onMessagePosted</c>
    ///        Response → <c>NotificationToastUI.ShowToast(string)</c>
    ///   4. Optionally add a close Button whose onClick calls <see cref="HideToast"/>.
    ///
    /// ── Timing ─────────────────────────────────────────────────────────────────
    ///   The toast auto-hides after <see cref="_displayDuration"/> seconds.
    ///   Calling <see cref="ShowToast"/> again while a toast is visible resets
    ///   the timer so the new message is shown for the full duration.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • <c>BattleRobots.UI</c> namespace — no Physics references.
    ///   • No allocations in <see cref="ShowToast"/> except the coroutine itself.
    ///   • Coroutine is stopped on <see cref="HideToast"/> and re-started on each
    ///     <see cref="ShowToast"/> call so the display duration is always fresh.
    ///
    /// Inspector fields:
    ///   _toastPanel      → root GameObject toggled active/inactive
    ///   _messageLabel    → Text component showing the notification text
    ///   _displayDuration → seconds the toast stays visible before auto-hiding
    /// </summary>
    public sealed class NotificationToastUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Toast Panel")]
        [Tooltip("Root GameObject of the toast UI. Toggled active/inactive on show/hide.")]
        [SerializeField] private GameObject _toastPanel;

        [Tooltip("Text component that displays the notification message.")]
        [SerializeField] private Text _messageLabel;

        [Header("Timing")]
        [Tooltip("Seconds the toast remains visible before auto-hiding.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 3f;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Coroutine _hideCoroutine;

        // ── Testable properties ───────────────────────────────────────────────

        /// <summary>True when the toast panel is currently visible.</summary>
        public bool IsVisible => _toastPanel != null && _toastPanel.activeSelf;

        /// <summary>The message text from the most recent <see cref="ShowToast"/> call.</summary>
        public string LastMessage { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the toast panel with <paramref name="message"/> and schedules
        /// auto-hide after <see cref="_displayDuration"/> seconds.
        /// If a toast is already visible its timer is reset.
        /// </summary>
        public void ShowToast(string message)
        {
            LastMessage = message;

            if (_messageLabel != null)
                _messageLabel.text = message;

            if (_toastPanel != null)
                _toastPanel.SetActive(true);

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            _hideCoroutine = StartCoroutine(AutoHide());
        }

        /// <summary>
        /// Immediately hides the toast panel and cancels any pending auto-hide timer.
        /// </summary>
        public void HideToast()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            if (_toastPanel != null)
                _toastPanel.SetActive(false);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(_displayDuration);
            HideToast();
        }
    }
}

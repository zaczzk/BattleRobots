using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that visualises bot auto-capture danger to the player.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _progressBar  → Slider; value = AccumulatedTime / AutoCaptureDuration.
    ///   _statusLabel  → "Bot Takeover: X.Xs" while accumulating.
    ///   _panel        → Shown only while the accumulator is active (player holds
    ///                   zero zones); hidden otherwise.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onAutoCapture</c> → <see cref="HandleAutoCapture"/> which
    ///   hides the panel once a takeover fires.
    ///   <see cref="Update"/> polls <c>_captureController.IsAccumulating</c> each
    ///   frame and drives the bar / label (zero alloc — only reads value-type fields).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one auto-capture HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAutoCaptureHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAutoCaptureSO        _autoCaptureSO;
        [SerializeField] private ZoneControlAutoCaptureController _captureController;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlAutoCaptureSO._onAutoCapture.")]
        [SerializeField] private VoidGameEvent _onAutoCapture;

        [Header("UI Refs (optional)")]
        [Tooltip("Slider showing danger level (0 = safe, 1 = takeover imminent).")]
        [SerializeField] private Slider     _progressBar;
        [Tooltip("Label showing 'Bot Takeover: X.Xs' while accumulating.")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleAutoCaptureDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => _handleAutoCaptureDelegate = HandleAutoCapture;

        private void OnEnable()
        {
            _onAutoCapture?.RegisterCallback(_handleAutoCaptureDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onAutoCapture?.UnregisterCallback(_handleAutoCaptureDelegate);
            _panel?.SetActive(false);
        }

        private void Update() => Refresh();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the auto-capture fires.
        /// Hides the danger panel because the takeover event has resolved.
        /// </summary>
        public void HandleAutoCapture()
        {
            _panel?.SetActive(false);
            if (_statusLabel != null)
                _statusLabel.text = "Bot Takeover!";
        }

        /// <summary>
        /// Rebuilds the progress bar and status label from the current controller state.
        /// Shows the panel only when the accumulator is active.
        /// Hides the panel when <c>_autoCaptureSO</c> or <c>_captureController</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_autoCaptureSO == null || _captureController == null)
            {
                _panel?.SetActive(false);
                return;
            }

            bool isAccumulating = _captureController.IsAccumulating;
            _panel?.SetActive(isAccumulating);

            if (!isAccumulating) return;

            float duration    = Mathf.Max(0.1f, _autoCaptureSO.AutoCaptureDuration);
            float accumulated = _captureController.AccumulatedTime;
            float ratio       = Mathf.Clamp01(accumulated / duration);
            float remaining   = Mathf.Max(0f, duration - accumulated);

            if (_progressBar != null)
                _progressBar.value = ratio;

            if (_statusLabel != null)
                _statusLabel.text = $"Bot Takeover: {remaining:F1}s";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound auto-capture SO (may be null).</summary>
        public ZoneControlAutoCaptureSO AutoCaptureSO => _autoCaptureSO;
    }
}

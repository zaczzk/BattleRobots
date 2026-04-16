using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that visualises <see cref="ZoneControlZoneCountdownSO"/> state
    /// as a progress bar and status label.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _progressBar  → Slider; value = ZoneControlZoneCountdownSO.Progress [0,1].
    ///   _statusLabel  → "Ready in X.Xs" while active / "Available" on expiry.
    ///   _panel        → Root panel; hidden when _countdownSO is null.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onCountdownExpired</c> → <see cref="HandleExpired"/>.
    ///   <see cref="Update"/> calls <see cref="Tick"/> which refreshes the HUD
    ///   each frame while the countdown is active.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one countdown HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCountdownHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneCountdownSO _countdownSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlZoneCountdownSO._onCountdownExpired.")]
        [SerializeField] private VoidGameEvent _onCountdownExpired;

        [Header("UI Refs (optional)")]
        [Tooltip("Slider showing countdown progress (1 = just started, 0 = expired).")]
        [SerializeField] private Slider     _progressBar;
        [Tooltip("Label showing 'Ready in X.Xs' or 'Available'.")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _isExpired;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleExpiredDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => _handleExpiredDelegate = HandleExpired;

        private void OnEnable()
        {
            _onCountdownExpired?.RegisterCallback(_handleExpiredDelegate);
            _isExpired = false;
            Refresh();
        }

        private void OnDisable()
        {
            _onCountdownExpired?.UnregisterCallback(_handleExpiredDelegate);
        }

        private void Update() => Tick();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the countdown expires.
        /// Sets the status label to "Available".
        /// </summary>
        public void HandleExpired()
        {
            _isExpired = true;
            Refresh();
        }

        /// <summary>
        /// Advances the HUD one frame.
        /// Refreshes the bar and label while the countdown SO is active.
        /// No-op when <c>_countdownSO</c> is null or the countdown is not active.
        /// </summary>
        public void Tick()
        {
            if (_countdownSO == null) return;
            if (!_countdownSO.IsActive) return;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the progress bar and status label from the current SO state.
        /// Hides the panel when <c>_countdownSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_countdownSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressBar != null)
                _progressBar.value = _countdownSO.Progress;

            if (_statusLabel != null)
            {
                if (_countdownSO.IsActive)
                {
                    float remaining = _countdownSO.Duration * _countdownSO.Progress;
                    _statusLabel.text = $"Ready in {remaining:F1}s";
                }
                else
                {
                    _statusLabel.text = "Available";
                }
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound countdown SO (may be null).</summary>
        public ZoneControlZoneCountdownSO CountdownSO => _countdownSO;

        /// <summary>True after the last <see cref="HandleExpired"/> call, until next OnEnable.</summary>
        public bool IsExpired => _isExpired;
    }
}

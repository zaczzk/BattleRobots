using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that shows a timed notification banner whenever a control
    /// zone is captured or lost.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onZoneCaptured</c>: shows the panel with <see cref="_capturedMessage"/>
    ///      and resets the display timer to <see cref="NotificationDuration"/>.
    ///   2. On <c>_onZoneLost</c>: shows the panel with <see cref="_lostMessage"/>
    ///      and resets the display timer.
    ///   3. Each <c>Update</c>: decrements the timer; hides the panel when it expires.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one notification banner per HUD canvas.
    ///   - <see cref="Tick"/> is public for EditMode test driving.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_onZoneCaptured</c>  → the shared zone-captured VoidGameEvent
    ///      (e.g., ControlZoneSO._onCaptured for the relevant player-side zone).
    ///   2. Assign <c>_onZoneLost</c>      → the shared zone-lost VoidGameEvent.
    ///   3. Assign optional <c>_notificationPanel</c> and <c>_notificationLabel</c>.
    ///   4. Tune <c>_notificationDuration</c> and message strings in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureNotificationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onZoneLost;

        [Header("Notification Settings")]
        [Tooltip("Message shown in the notification label when a zone is captured.")]
        [SerializeField] private string _capturedMessage = "ZONE CAPTURED!";

        [Tooltip("Message shown in the notification label when a zone is lost.")]
        [SerializeField] private string _lostMessage = "ZONE LOST!";

        [Tooltip("Seconds the notification banner stays visible.")]
        [SerializeField, Min(0.1f)] private float _notificationDuration = 2f;

        [Header("UI Refs (optional)")]
        [SerializeField] private GameObject _notificationPanel;
        [SerializeField] private Text       _notificationLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float _displayTimer;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _onCapturedDelegate;
        private Action _onLostDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onCapturedDelegate = ShowCapture;
            _onLostDelegate     = ShowLost;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_onCapturedDelegate);
            _onZoneLost?.RegisterCallback(_onLostDelegate);
            _displayTimer = 0f;
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_onCapturedDelegate);
            _onZoneLost?.UnregisterCallback(_onLostDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the notification panel with the captured message and starts the
        /// display timer. Wired to <c>_onZoneCaptured</c>.
        /// </summary>
        public void ShowCapture()
        {
            if (_notificationLabel != null)
                _notificationLabel.text = _capturedMessage;

            _notificationPanel?.SetActive(true);
            _displayTimer = _notificationDuration;
        }

        /// <summary>
        /// Shows the notification panel with the lost message and starts the
        /// display timer. Wired to <c>_onZoneLost</c>.
        /// </summary>
        public void ShowLost()
        {
            if (_notificationLabel != null)
                _notificationLabel.text = _lostMessage;

            _notificationPanel?.SetActive(true);
            _displayTimer = _notificationDuration;
        }

        /// <summary>
        /// Decrements the display timer by <paramref name="dt"/> seconds.
        /// Hides the panel when the timer reaches zero.
        /// No-op when the timer is already at zero.
        /// Public for EditMode test driving.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= dt;

            if (_displayTimer <= 0f)
            {
                _displayTimer = 0f;
                _notificationPanel?.SetActive(false);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Seconds the notification banner stays visible after a trigger.</summary>
        public float NotificationDuration => _notificationDuration;

        /// <summary>Remaining display time in seconds. Zero when the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>Message shown when a zone is captured.</summary>
        public string CapturedMessage => _capturedMessage;

        /// <summary>Message shown when a zone is lost.</summary>
        public string LostMessage => _lostMessage;
    }
}

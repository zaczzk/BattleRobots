using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that shows a timed "FAST PACE!" or "SLOW PACE!" notification
    /// banner when the corresponding pace events fire from
    /// <see cref="ZoneCapturePaceTrackerSO"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Subscribes to <c>_onFastPace</c> and <c>_onSlowPace</c> events.
    ///   • When an event fires the banner is shown for <see cref="_displayDuration"/>
    ///     seconds, then auto-hidden via <see cref="Tick"/>.
    ///   • A per-event <c>_cooldownDuration</c> deduplication window prevents the
    ///     same event from re-triggering the banner within the cooldown period.
    ///     The other event type is always allowed regardless of cooldown state.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one notification banner per panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _onFastPace → ZoneCapturePaceTrackerSO._onFastPace channel.
    ///   2. Assign _onSlowPace → ZoneCapturePaceTrackerSO._onSlowPace channel.
    ///   3. Assign _panel, _messageLabel, and tune _displayDuration/_cooldownDuration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCapturePaceNotificationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Notification Settings")]
        [Tooltip("How long the banner stays visible after a pace event fires.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2.5f;

        [Tooltip("Minimum seconds before the same pace event can re-show the banner.")]
        [SerializeField, Min(0f)]   private float _cooldownDuration = 1.0f;

        [Tooltip("Message shown when the fast-pace event fires.")]
        [SerializeField] private string _fastPaceMessage = "FAST PACE!";

        [Tooltip("Message shown when the slow-pace event fires.")]
        [SerializeField] private string _slowPaceMessage = "SLOW PACE!";

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneCapturePaceTrackerSO._onFastPace.")]
        [SerializeField] private VoidGameEvent _onFastPace;

        [Tooltip("Wire to ZoneCapturePaceTrackerSO._onSlowPace.")]
        [SerializeField] private VoidGameEvent _onSlowPace;

        [Header("UI Refs (optional)")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text       _messageLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isActive;
        private float _displayTimer;
        private float _fastCooldown;
        private float _slowCooldown;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleFastDelegate;
        private Action _handleSlowDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleFastDelegate = HandleFastPace;
            _handleSlowDelegate = HandleSlowPace;
        }

        private void OnEnable()
        {
            _onFastPace?.RegisterCallback(_handleFastDelegate);
            _onSlowPace?.RegisterCallback(_handleSlowDelegate);
            _isActive     = false;
            _displayTimer = 0f;
            _fastCooldown = 0f;
            _slowCooldown = 0f;
            _panel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onFastPace?.UnregisterCallback(_handleFastDelegate);
            _onSlowPace?.UnregisterCallback(_handleSlowDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the fast-pace banner, unless the fast-pace cooldown is still active.
        /// Null-safe.
        /// </summary>
        public void HandleFastPace()
        {
            if (_fastCooldown > 0f) return;
            _fastCooldown = _cooldownDuration;
            ShowBanner(_fastPaceMessage);
        }

        /// <summary>
        /// Shows the slow-pace banner, unless the slow-pace cooldown is still active.
        /// Null-safe.
        /// </summary>
        public void HandleSlowPace()
        {
            if (_slowCooldown > 0f) return;
            _slowCooldown = _cooldownDuration;
            ShowBanner(_slowPaceMessage);
        }

        /// <summary>
        /// Advances the display and cooldown timers.
        /// Hides the banner once <see cref="_displayDuration"/> elapses.
        /// Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            if (_fastCooldown > 0f) _fastCooldown = Mathf.Max(0f, _fastCooldown - dt);
            if (_slowCooldown > 0f) _slowCooldown = Mathf.Max(0f, _slowCooldown - dt);

            if (!_isActive) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                HideBanner();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ShowBanner(string message)
        {
            _isActive     = true;
            _displayTimer = _displayDuration;
            _panel?.SetActive(true);
            if (_messageLabel != null) _messageLabel.text = message;
        }

        private void HideBanner()
        {
            _isActive = false;
            _panel?.SetActive(false);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the notification banner is visible.</summary>
        public bool IsActive => _isActive;

        /// <summary>Remaining display time in seconds (0 when inactive).</summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>Remaining fast-pace cooldown time in seconds.</summary>
        public float FastCooldownRemaining => _fastCooldown;

        /// <summary>Remaining slow-pace cooldown time in seconds.</summary>
        public float SlowCooldownRemaining => _slowCooldown;

        /// <summary>Configured display duration (seconds).</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>Configured cooldown duration (seconds).</summary>
        public float CooldownDuration => _cooldownDuration;
    }
}

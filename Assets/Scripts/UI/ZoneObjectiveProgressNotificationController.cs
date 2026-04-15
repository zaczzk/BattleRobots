using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that shows a timed "OBJECTIVE MET!" notification banner
    /// the first time <see cref="ZoneObjectiveProgressTrackerSO.IsObjectiveMet"/>
    /// transitions from <c>false</c> to <c>true</c> within a match.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Subscribes to <c>_onProgressUpdated</c> from the tracker SO.
    ///   • On each event, if the previous state was <em>not</em> met and the new
    ///     state is met, the banner is shown for <see cref="_displayDuration"/>
    ///     seconds, then auto-hidden.
    ///   • The <c>_wasObjectiveMet</c> flag prevents the banner from showing again
    ///     unless the controller is re-enabled (resetting the state to unmet).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one notification per panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _trackerSO        → ZoneObjectiveProgressTrackerSO asset.
    ///   2. Assign _onProgressUpdated → ZoneObjectiveProgressTrackerSO._onProgressUpdated.
    ///   3. Assign _panel, _messageLabel, and set _displayDuration as desired.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneObjectiveProgressNotificationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneObjectiveProgressTrackerSO _trackerSO;

        [Header("Notification Settings")]
        [Tooltip("How long the banner stays visible after the objective is met.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2.5f;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneObjectiveProgressTrackerSO._onProgressUpdated.")]
        [SerializeField] private VoidGameEvent _onProgressUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text       _messageLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _wasObjectiveMet;
        private bool  _isActive;
        private float _displayTimer;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleProgressDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleProgressDelegate = HandleProgressUpdated;
        }

        private void OnEnable()
        {
            _onProgressUpdated?.RegisterCallback(_handleProgressDelegate);
            _wasObjectiveMet = false;
            _isActive        = false;
            _panel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onProgressUpdated?.UnregisterCallback(_handleProgressDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the tracker SO raises <c>_onProgressUpdated</c>.
        /// Shows the banner if the objective just transitioned to met.
        /// Null-safe; no-op when <c>_trackerSO</c> is null.
        /// </summary>
        public void HandleProgressUpdated()
        {
            if (_trackerSO == null) return;

            bool isNowMet = _trackerSO.IsObjectiveMet;

            if (!_wasObjectiveMet && isNowMet)
                ShowBanner();

            _wasObjectiveMet = isNowMet;
        }

        /// <summary>
        /// Advances the auto-hide timer.
        /// Hides the banner once <see cref="_displayDuration"/> has elapsed.
        /// Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isActive) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                HideBanner();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ShowBanner()
        {
            _isActive     = true;
            _displayTimer = _displayDuration;
            _panel?.SetActive(true);
            if (_messageLabel != null) _messageLabel.text = "OBJECTIVE MET!";
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

        /// <summary>Display duration configured in the inspector.</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>The bound tracker SO (may be null).</summary>
        public ZoneObjectiveProgressTrackerSO TrackerSO => _trackerSO;
    }
}

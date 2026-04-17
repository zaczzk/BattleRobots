using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlPerformanceMetricsSO"/>
    /// and displays the end-of-match performance grade panel.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>_onZoneCaptured</c>: calls <c>RecordCapture</c>.
    ///   <c>_onCaptureAttempted</c>: calls <c>RecordAttempt</c>.
    ///   <c>_onMatchEnded</c>: finalizes metrics with elapsed time and refreshes.
    ///   <c>_onMatchStarted</c>: resets metrics and refreshes.
    ///   <c>_onMetricsFinalized</c>: refreshes display.
    ///   <see cref="Refresh"/>: shows grade, accuracy %, and rate label;
    ///   hides panel when <c>_metricsSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlPerformanceMetricsController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlPerformanceMetricsSO _metricsSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onCaptureAttempted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMetricsFinalized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gradeLabel;
        [SerializeField] private Text       _accuracyLabel;
        [SerializeField] private Text       _rateLabel;
        [SerializeField] private GameObject _panel;

        // ── Runtime ───────────────────────────────────────────────────────────

        private float _matchStartTime;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleCaptureDelegate;
        private Action _handleAttemptDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleCaptureDelegate      = HandleCapture;
            _handleAttemptDelegate      = HandleAttempt;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onCaptureAttempted?.RegisterCallback(_handleAttemptDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMetricsFinalized?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onCaptureAttempted?.UnregisterCallback(_handleAttemptDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMetricsFinalized?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleCapture()  => _metricsSO?.RecordCapture();
        private void HandleAttempt()  => _metricsSO?.RecordAttempt();

        private void HandleMatchEnded()
        {
            if (_metricsSO == null) return;
            float elapsed = Time.time - _matchStartTime;
            _metricsSO.FinalizeMetrics(elapsed);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _matchStartTime = Time.time;
            _metricsSO?.Reset();
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>Updates the performance metrics panel.</summary>
        public void Refresh()
        {
            if (_metricsSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gradeLabel != null)
                _gradeLabel.text = $"Grade: {_metricsSO.GetPerformanceGrade()}";

            if (_accuracyLabel != null)
                _accuracyLabel.text = $"Accuracy: {_metricsSO.CaptureAccuracy * 100f:F0}%";

            if (_rateLabel != null)
                _rateLabel.text = $"Rate: {_metricsSO.CaptureRate:F1}/min";
        }

        // ── Properties ────────────────────────────────────────────────────────

        public ZoneControlPerformanceMetricsSO MetricsSO => _metricsSO;
    }
}

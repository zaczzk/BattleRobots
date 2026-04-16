using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that subscribes to pressure, threat, dominance, and match
    /// events; feeds them to <see cref="ZoneControlAlertSystemSO"/>; and drives a
    /// flashing critical-alert banner.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _alertLabel → "CRITICAL ALERT!" when active.
    ///   _panel      → Root panel; shown when alert is critical, hidden otherwise.
    ///                 Hidden when <c>_alertSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one alert panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAlertSystemController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAlertSystemSO      _alertSO;
        [SerializeField] private ZoneControlMatchPressureSO    _pressureSO;
        [SerializeField] private ZoneControlThreatAssessmentSO _assessmentSO;
        [SerializeField] private ZoneDominanceSO               _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlMatchPressureSO._onHighPressure.")]
        [SerializeField] private VoidGameEvent _onHighPressure;

        [Tooltip("Wire to ZoneControlMatchPressureSO._onPressureRelieved.")]
        [SerializeField] private VoidGameEvent _onPressureRelieved;

        [Tooltip("Wire to ZoneControlThreatAssessmentSO._onThreatChanged.")]
        [SerializeField] private VoidGameEvent _onThreatChanged;

        [Tooltip("Wire to ZoneDominanceSO._onDominanceChanged.")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        [Tooltip("Raised at match start; resets the alert SO.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Re-raised by this controller when a critical alert fires.")]
        [SerializeField] private VoidGameEvent _onCriticalAlert;

        [Tooltip("Re-raised by this controller when the alert clears.")]
        [SerializeField] private VoidGameEvent _onAlertCleared;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _alertLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _evaluateDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _evaluateDelegate           = EvaluateFromCurrentState;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onHighPressure?.RegisterCallback(_evaluateDelegate);
            _onPressureRelieved?.RegisterCallback(_evaluateDelegate);
            _onThreatChanged?.RegisterCallback(_evaluateDelegate);
            _onDominanceChanged?.RegisterCallback(_evaluateDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHighPressure?.UnregisterCallback(_evaluateDelegate);
            _onPressureRelieved?.UnregisterCallback(_evaluateDelegate);
            _onThreatChanged?.UnregisterCallback(_evaluateDelegate);
            _onDominanceChanged?.UnregisterCallback(_evaluateDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current values from all data SOs and calls
        /// <see cref="ZoneControlAlertSystemSO.EvaluateAlert"/>.
        /// No-op when <c>_alertSO</c> is null.
        /// </summary>
        public void EvaluateFromCurrentState()
        {
            if (_alertSO == null) return;

            bool isHighPressure = _pressureSO != null && _pressureSO.IsHighPressure;
            ThreatLevel threat  = _assessmentSO != null
                ? _assessmentSO.CurrentThreat
                : ThreatLevel.Low;
            bool hasDominance   = _dominanceSO != null && _dominanceSO.HasDominance;

            bool wasCritical = _alertSO.IsCritical;
            _alertSO.EvaluateAlert(isHighPressure, threat, hasDominance);

            if (_alertSO.IsCritical && !wasCritical)
                _onCriticalAlert?.Raise();
            else if (!_alertSO.IsCritical && wasCritical)
                _onAlertCleared?.Raise();

            Refresh();
        }

        /// <summary>Resets the alert SO at match start and refreshes.</summary>
        public void HandleMatchStarted()
        {
            _alertSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the alert panel from the current critical state.
        /// Hides the panel when <c>_alertSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_alertSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            bool critical = _alertSO.IsCritical;
            _panel?.SetActive(critical);

            if (critical && _alertLabel != null)
                _alertLabel.text = "CRITICAL ALERT!";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound alert system SO (may be null).</summary>
        public ZoneControlAlertSystemSO AlertSO => _alertSO;
    }
}

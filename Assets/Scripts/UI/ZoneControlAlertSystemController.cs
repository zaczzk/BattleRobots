using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates <see cref="ZoneControlAlertSystemSO"/> whenever
    /// match pressure, threat level, or dominance changes, then drives an alert banner.
    ///
    /// ── Wiring ─────────────────────────────────────────────────────────────────
    ///   _pressureSO         → ZoneControlMatchPressureSO asset.
    ///   _threatSO           → ZoneControlThreatAssessmentSO asset.
    ///   _dominanceSO        → ZoneDominanceSO asset.
    ///   _onHighPressure     → ZoneControlMatchPressureSO._onHighPressure.
    ///   _onPressureRelieved → ZoneControlMatchPressureSO._onPressureRelieved.
    ///   _onThreatChanged    → ZoneControlThreatAssessmentSO._onThreatChanged.
    ///   _onMatchStarted     → shared MatchStarted VoidGameEvent.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _alertPanel  → shown while alert is active, hidden otherwise.
    ///   _alertLabel  → "CRITICAL ALERT!" while active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAlertSystemController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAlertSystemSO      _alertSO;
        [SerializeField] private ZoneControlMatchPressureSO    _pressureSO;
        [SerializeField] private ZoneControlThreatAssessmentSO _threatSO;
        [SerializeField] private ZoneDominanceSO                _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onHighPressure;
        [SerializeField] private VoidGameEvent _onPressureRelieved;
        [SerializeField] private VoidGameEvent _onThreatChanged;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private GameObject _alertPanel;
        [SerializeField] private Text       _alertLabel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _evaluateDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _evaluateDelegate          = EvaluateAndRefresh;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onHighPressure?.RegisterCallback(_evaluateDelegate);
            _onPressureRelieved?.RegisterCallback(_evaluateDelegate);
            _onThreatChanged?.RegisterCallback(_evaluateDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHighPressure?.UnregisterCallback(_evaluateDelegate);
            _onPressureRelieved?.UnregisterCallback(_evaluateDelegate);
            _onThreatChanged?.UnregisterCallback(_evaluateDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads current pressure, threat, and dominance values then calls
        /// <see cref="ZoneControlAlertSystemSO.EvaluateAlert"/> and refreshes the HUD.
        /// No-op when <c>_alertSO</c> is null.
        /// </summary>
        public void EvaluateAndRefresh()
        {
            if (_alertSO == null) return;

            bool isHighPressure = _pressureSO != null && _pressureSO.IsHighPressure;
            bool isThreatHigh   = _threatSO   != null && _threatSO.CurrentThreat >= ThreatLevel.High;
            bool hasDominance   = _dominanceSO != null && _dominanceSO.HasDominance;

            _alertSO.EvaluateAlert(isHighPressure, isThreatHigh, hasDominance);
            Refresh();
        }

        /// <summary>Resets the alert SO and refreshes the HUD at match start.</summary>
        public void HandleMatchStarted()
        {
            _alertSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the alert panel from the current SO state.
        /// Hides the panel when <c>_alertSO</c> is null or alert is inactive.
        /// </summary>
        public void Refresh()
        {
            if (_alertSO == null)
            {
                _alertPanel?.SetActive(false);
                return;
            }

            bool active = _alertSO.IsCriticalAlert;
            _alertPanel?.SetActive(active);

            if (_alertLabel != null)
                _alertLabel.text = active ? "CRITICAL ALERT!" : string.Empty;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound alert system SO (may be null).</summary>
        public ZoneControlAlertSystemSO AlertSO => _alertSO;
    }
}

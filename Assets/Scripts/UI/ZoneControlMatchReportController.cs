using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that generates and displays an end-of-match report panel
    /// showing zone captures, threat level, and best combo.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _zonesLabel  → "Zones: N".
    ///   _threatLabel → "Threat: X" (e.g. "Threat: High").
    ///   _comboLabel  → "Best Combo: N".
    ///   _panel       → Root panel; shown when report is generated.
    ///                  Hidden when <c>_reportSO</c> is null or report not generated.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one report panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchReportController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchReportSO     _reportSO;
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;
        [SerializeField] private ZoneControlMatchPressureSO   _pressureSO;
        [SerializeField] private ZoneControlThreatAssessmentSO _threatSO;
        [SerializeField] private ZoneControlComboTrackerSO    _comboSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers report generation.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised by ZoneControlMatchReportSO after report generation; refreshes HUD.")]
        [SerializeField] private VoidGameEvent _onReportGenerated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _zonesLabel;
        [SerializeField] private Text       _threatLabel;
        [SerializeField] private Text       _comboLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onReportGenerated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onReportGenerated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates the match report from current data SOs and refreshes the HUD.
        /// </summary>
        public void HandleMatchEnded()
        {
            _reportSO?.GenerateReport(_summarySO, _pressureSO, _threatSO, _comboSO);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the report labels from the current report state.
        /// Hides the panel when <c>_reportSO</c> is null or the report has not been generated.
        /// </summary>
        public void Refresh()
        {
            if (_reportSO == null || !_reportSO.IsGenerated)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_zonesLabel != null)
                _zonesLabel.text = $"Zones: {_reportSO.TotalZonesCaptured}";

            if (_threatLabel != null)
                _threatLabel.text = $"Threat: {_reportSO.FinalThreatLevel}";

            if (_comboLabel != null)
                _comboLabel.text = $"Best Combo: {_reportSO.BestCombo}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound match report SO (may be null).</summary>
        public ZoneControlMatchReportSO ReportSO => _reportSO;
    }
}

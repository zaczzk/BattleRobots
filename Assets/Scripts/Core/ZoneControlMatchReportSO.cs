using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that compiles a structured end-of-match report:
    /// total zones captured, current match pressure, final threat level, and best combo.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="GenerateReport"/> at match end with the relevant data SOs.
    ///   The report is then readable via its properties until <see cref="Reset"/> is called.
    ///   <see cref="IsGenerated"/> is true after a successful <see cref="GenerateReport"/> call.
    ///   Call <see cref="Reset"/> at match start.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchReport.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchReport", order = 54)]
    public sealed class ZoneControlMatchReportSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after a successful GenerateReport call.")]
        [SerializeField] private VoidGameEvent _onReportGenerated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int        _totalZonesCaptured;
        private float      _matchPressure;
        private ThreatLevel _finalThreatLevel;
        private int        _bestCombo;
        private bool       _isGenerated;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total zones captured this match (from the session summary SO).</summary>
        public int         TotalZonesCaptured => _totalZonesCaptured;

        /// <summary>Match pressure value at report time (from the pressure SO).</summary>
        public float       MatchPressure      => _matchPressure;

        /// <summary>Final assessed threat level (from the threat assessment SO).</summary>
        public ThreatLevel FinalThreatLevel   => _finalThreatLevel;

        /// <summary>Combo count at report time (from the combo tracker SO).</summary>
        public int         BestCombo          => _bestCombo;

        /// <summary>True after a successful GenerateReport call; false after Reset.</summary>
        public bool        IsGenerated        => _isGenerated;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the report fields from the provided data SOs.
        /// No-op when <paramref name="summarySO"/> is null.
        /// Fires <see cref="_onReportGenerated"/> on success.
        /// </summary>
        public void GenerateReport(
            ZoneControlSessionSummarySO    summarySO,
            ZoneControlMatchPressureSO     pressureSO,
            ZoneControlThreatAssessmentSO  threatSO,
            ZoneControlComboTrackerSO      comboSO)
        {
            if (summarySO == null) return;

            _totalZonesCaptured = summarySO.TotalZonesCaptured;
            _matchPressure      = pressureSO != null ? pressureSO.Pressure      : 0f;
            _finalThreatLevel   = threatSO   != null ? threatSO.CurrentThreat   : ThreatLevel.Low;
            _bestCombo          = comboSO    != null ? comboSO.ComboCount        : 0;
            _isGenerated        = true;

            _onReportGenerated?.Raise();
        }

        /// <summary>
        /// Clears all report fields silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalZonesCaptured = 0;
            _matchPressure      = 0f;
            _finalThreatLevel   = ThreatLevel.Low;
            _bestCombo          = 0;
            _isGenerated        = false;
        }
    }
}

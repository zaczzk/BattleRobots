using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives a threat-level badge HUD for zone-control mode.
    ///
    /// Subscribes to scoreboard and match-boundary events; evaluates the current bot
    /// threat level via <see cref="ZoneControlThreatAssessmentSO"/>; and colours a
    /// badge image and label according to the assessed threat (Low/Medium/High).
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _threatLabel → "Low" / "Medium" / "High".
    ///   _threatBadge → Image recoloured to match the threat level.
    ///   _panel       → Root panel; hidden when <c>_assessmentSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one threat panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _assessmentSO        → ZoneControlThreatAssessmentSO asset.
    ///   2. Assign _scoreboardSO        → ZoneControlScoreboardSO asset.
    ///   3. Assign _dominanceSO         → ZoneDominanceSO asset (optional).
    ///   4. Assign _onScoreboardUpdated → ZoneControlScoreboardSO._onScoreboardUpdated.
    ///   5. Assign _onThreatChanged     → ZoneControlThreatAssessmentSO._onThreatChanged.
    ///   6. Assign _onMatchStarted      → shared MatchStarted VoidGameEvent.
    ///   7. Assign _threatLabel / _threatBadge / _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlThreatAssessmentController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlThreatAssessmentSO _assessmentSO;
        [SerializeField] private ZoneControlScoreboardSO        _scoreboardSO;
        [SerializeField] private ZoneDominanceSO                 _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlScoreboardSO._onScoreboardUpdated.")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        [Tooltip("Wire to ZoneControlThreatAssessmentSO._onThreatChanged for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onThreatChanged;

        [Tooltip("Raised at match start; resets the assessment SO.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text  _threatLabel;
        [SerializeField] private Image _threatBadge;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        [Header("Threat Colours")]
        [SerializeField] private Color _lowColor    = Color.green;
        [SerializeField] private Color _mediumColor = Color.yellow;
        [SerializeField] private Color _highColor   = Color.red;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreboardDelegate;
        private Action _refreshDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreboardDelegate   = HandleScoreboardUpdated;
            _refreshDelegate            = Refresh;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onScoreboardUpdated?.RegisterCallback(_handleScoreboardDelegate);
            _onThreatChanged?.RegisterCallback(_refreshDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreboardUpdated?.UnregisterCallback(_handleScoreboardDelegate);
            _onThreatChanged?.UnregisterCallback(_refreshDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current scoreboard rank and dominance state, then evaluates
        /// the threat level on the SO.  The SO fires <c>_onThreatChanged</c> if the
        /// level changes, which triggers <see cref="Refresh"/> reactively.
        /// No-op when <c>_assessmentSO</c> or <c>_scoreboardSO</c> is null.
        /// </summary>
        public void HandleScoreboardUpdated()
        {
            if (_assessmentSO == null || _scoreboardSO == null) return;

            bool hasDominance = _dominanceSO != null && _dominanceSO.HasDominance;
            _assessmentSO.EvaluateThreat(_scoreboardSO.PlayerRank, hasDominance);
        }

        /// <summary>
        /// Resets the assessment SO at match start and refreshes the HUD.
        /// No-op when <c>_assessmentSO</c> is null.
        /// </summary>
        public void HandleMatchStarted()
        {
            _assessmentSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the threat label and badge colour from the current assessment state.
        /// Hides the panel when <c>_assessmentSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_assessmentSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            ThreatLevel threat = _assessmentSO.CurrentThreat;

            if (_threatLabel != null)
                _threatLabel.text = threat.ToString();

            if (_threatBadge != null)
            {
                _threatBadge.color = threat switch
                {
                    ThreatLevel.High   => _highColor,
                    ThreatLevel.Medium => _mediumColor,
                    _                  => _lowColor
                };
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound threat assessment SO (may be null).</summary>
        public ZoneControlThreatAssessmentSO AssessmentSO => _assessmentSO;
    }
}

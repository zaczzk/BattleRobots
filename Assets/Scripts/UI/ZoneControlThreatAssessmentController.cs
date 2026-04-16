using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives a coloured threat-badge HUD for zone-control matches.
    ///
    /// Reads the player's scoreboard rank (<see cref="ZoneControlScoreboardSO"/>) and
    /// zone-dominance status (<see cref="ZoneDominanceSO"/>), delegates the threat level
    /// computation to <see cref="ZoneControlThreatAssessmentSO"/>, and shows the
    /// corresponding Low / Medium / High badge.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _threatLevelLabel → "Low" / "Medium" / "High"
    ///   _lowThreatBadge   → active when threat is Low
    ///   _medThreatBadge   → active when threat is Medium
    ///   _highThreatBadge  → active when threat is High
    ///   _panel            → hidden when _assessmentSO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one threat-badge panel per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _assessmentSO        → ZoneControlThreatAssessmentSO asset.
    ///   2. Assign _scoreboardSO        → ZoneControlScoreboardSO asset.
    ///   3. Assign _dominanceSO         → ZoneDominanceSO asset.
    ///   4. Assign _onScoreboardUpdated → VoidGameEvent raised on score changes.
    ///   5. Assign _onDominanceChanged  → VoidGameEvent raised on dominance changes.
    ///   6. Assign _onMatchStarted      → VoidGameEvent raised at match start.
    ///   7. Wire badge GameObjects and label Text.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlThreatAssessmentController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlThreatAssessmentSO _assessmentSO;
        [SerializeField] private ZoneControlScoreboardSO       _scoreboardSO;
        [SerializeField] private ZoneDominanceSO               _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised on any scoreboard score update.")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        [Tooltip("Raised when dominance changes.")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        [Tooltip("Raised when the threat level SO fires _onThreatChanged; triggers a HUD refresh.")]
        [SerializeField] private VoidGameEvent _onThreatChanged;

        [Tooltip("Raised at match start; resets the assessment SO.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _threatLevelLabel;
        [SerializeField] private GameObject _lowThreatBadge;
        [SerializeField] private GameObject _medThreatBadge;
        [SerializeField] private GameObject _highThreatBadge;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreboardUpdatedDelegate;
        private Action _handleDominanceChangedDelegate;
        private Action _refreshDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreboardUpdatedDelegate = HandleScoreboardUpdated;
            _handleDominanceChangedDelegate  = HandleDominanceChanged;
            _refreshDelegate                 = Refresh;
            _handleMatchStartedDelegate      = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onScoreboardUpdated?.RegisterCallback(_handleScoreboardUpdatedDelegate);
            _onDominanceChanged?.RegisterCallback(_handleDominanceChangedDelegate);
            _onThreatChanged?.RegisterCallback(_refreshDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreboardUpdated?.UnregisterCallback(_handleScoreboardUpdatedDelegate);
            _onDominanceChanged?.UnregisterCallback(_handleDominanceChangedDelegate);
            _onThreatChanged?.UnregisterCallback(_refreshDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Evaluates and refreshes the threat badge on scoreboard changes.</summary>
        public void HandleScoreboardUpdated()
        {
            Evaluate();
            Refresh();
        }

        /// <summary>Evaluates and refreshes the threat badge on dominance changes.</summary>
        public void HandleDominanceChanged()
        {
            Evaluate();
            Refresh();
        }

        /// <summary>Resets the assessment SO and refreshes the HUD at match start.</summary>
        public void HandleMatchStarted()
        {
            _assessmentSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current player rank and dominance state, then delegates
        /// threat-level evaluation to the assessment SO.
        /// No-op when <c>_assessmentSO</c> is null.
        /// </summary>
        public void Evaluate()
        {
            if (_assessmentSO == null) return;
            int  rank         = _scoreboardSO != null ? _scoreboardSO.PlayerRank : 1;
            bool hasDominance = _dominanceSO  != null && _dominanceSO.HasDominance;
            _assessmentSO.EvaluateThreat(rank, hasDominance);
        }

        /// <summary>
        /// Updates the threat label and badge visibility from the SO's current threat level.
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

            ThreatLevel level = _assessmentSO.CurrentThreat;

            if (_threatLevelLabel != null)
                _threatLevelLabel.text = level.ToString();

            _lowThreatBadge?.SetActive(level == ThreatLevel.Low);
            _medThreatBadge?.SetActive(level == ThreatLevel.Medium);
            _highThreatBadge?.SetActive(level == ThreatLevel.High);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound threat-assessment SO (may be null).</summary>
        public ZoneControlThreatAssessmentSO AssessmentSO => _assessmentSO;
    }
}

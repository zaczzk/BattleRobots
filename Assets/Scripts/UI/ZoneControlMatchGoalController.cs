using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates the zone-control match goal and drives the
    /// goal HUD label.
    ///
    /// ── Flow ────────────────────────────────────────────────────────────────────
    ///   • <c>_onMatchStarted</c>       → resets internal goal-met flag and records
    ///     the match start time; calls Refresh().
    ///   • <c>_onScoreboardUpdated</c>  → HandleScoreboardUpdated: evaluates
    ///     <see cref="ZoneControlMatchGoalSO.IsGoalMet"/>; on first met → fires
    ///     <c>_onGoalMet</c> and calls Refresh().
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _goalLabel → "First to N zones" / "Most zones in 120s"
    ///   _panel     → hidden when _goalSO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one goal panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _goalSO              → ZoneControlMatchGoalSO asset.
    ///   2. Assign _scoreboardSO        → ZoneControlScoreboardSO asset.
    ///   3. Assign _onScoreboardUpdated → channel fired when the scoreboard changes.
    ///   4. Assign _onMatchStarted      → channel fired at match start.
    ///   5. Assign _onGoalMet           → VoidGameEvent to raise when goal is reached.
    ///   6. Assign _goalLabel / _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchGoalController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchGoalSO    _goalSO;
        [SerializeField] private ZoneControlScoreboardSO   _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when the zone-control scoreboard changes; triggers goal evaluation.")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        [Tooltip("Raised at match start; resets the goal-met flag.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Raised once when the match goal is first met.")]
        [SerializeField] private VoidGameEvent _onGoalMet;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _goalLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleScoreboardUpdatedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isGoalMet;
        private float _matchStartTime;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleScoreboardUpdatedDelegate = HandleScoreboardUpdated;
            _handleMatchStartedDelegate      = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onScoreboardUpdated?.RegisterCallback(_handleScoreboardUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreboardUpdated?.UnregisterCallback(_handleScoreboardUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the match goal against the current scoreboard and elapsed time.
        /// Fires <see cref="_onGoalMet"/> on the first transition to met.
        /// </summary>
        public void HandleScoreboardUpdated()
        {
            if (_goalSO == null) return;
            if (_isGoalMet) return;

            int playerScore  = _scoreboardSO != null ? _scoreboardSO.PlayerScore : 0;
            int timeElapsed  = Mathf.FloorToInt(Time.time - _matchStartTime);

            if (_goalSO.IsGoalMet(playerScore, timeElapsed))
            {
                _isGoalMet = true;
                _onGoalMet?.Raise();
                Refresh();
            }
        }

        /// <summary>
        /// Resets the goal-met flag and records the match start time.
        /// </summary>
        public void HandleMatchStarted()
        {
            _isGoalMet      = false;
            _matchStartTime = Time.time;
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the goal label from the SO description.
        /// Hides the panel when <c>_goalSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_goalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_goalLabel != null)
                _goalLabel.text = _goalSO.GoalDescription;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True once the match goal has been met this session.</summary>
        public bool IsGoalMet => _isGoalMet;

        /// <summary>The bound goal SO (may be null).</summary>
        public ZoneControlMatchGoalSO GoalSO => _goalSO;
    }
}

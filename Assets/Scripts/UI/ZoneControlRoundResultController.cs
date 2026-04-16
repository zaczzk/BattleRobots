using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records a round result at match end and displays the
    /// cumulative win count and win rate.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _winsLabel    → "Wins: N".
    ///   _winRateLabel → "Win Rate: N%".
    ///   _panel        → Root panel; shown when <c>_resultSO</c> is assigned.
    ///                   Hidden when <c>_resultSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one results panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRoundResultController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRoundResultSO _resultSO;
        [SerializeField] private ZoneControlScoreboardSO  _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers RecordResult from the scoreboard.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised by ZoneControlRoundResultSO after RecordResult; refreshes the HUD.")]
        [SerializeField] private VoidGameEvent _onResultRecorded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _winsLabel;
        [SerializeField] private Text       _winRateLabel;
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
            _onResultRecorded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onResultRecorded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the scoreboard, records the round outcome, and refreshes the HUD.
        /// No-op when either SO is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_resultSO == null || _scoreboardSO == null) { Refresh(); return; }

            bool playerWon  = _scoreboardSO.PlayerScore > _scoreboardSO.GetBotScore(0);
            int  scoreDelta = _scoreboardSO.PlayerScore - _scoreboardSO.GetBotScore(0);
            _resultSO.RecordResult(playerWon, scoreDelta);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds win / win-rate labels from the current result history.
        /// Hides the panel when <c>_resultSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_resultSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            var results = _resultSO.GetResults();
            int wins    = 0;
            foreach (var r in results)
                if (r.PlayerWon) wins++;

            if (_winsLabel != null)
                _winsLabel.text = $"Wins: {wins}";

            if (_winRateLabel != null)
                _winRateLabel.text = $"Win Rate: {Mathf.RoundToInt(_resultSO.WinRate * 100)}%";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound round result SO (may be null).</summary>
        public ZoneControlRoundResultSO ResultSO     => _resultSO;

        /// <summary>The bound scoreboard SO (may be null).</summary>
        public ZoneControlScoreboardSO  ScoreboardSO => _scoreboardSO;
    }
}

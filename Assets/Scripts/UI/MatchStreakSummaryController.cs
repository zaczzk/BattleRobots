using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match summary card that aggregates win-streak, star rating, and score
    /// trend into a single end-of-session panel.
    ///
    /// ── Data sources ─────────────────────────────────────────────────────────
    ///   • <see cref="WinStreakSO"/>            — CurrentStreak / BestStreak.
    ///   • <see cref="MatchRatingController"/>  — CurrentStars (read after match end).
    ///   • <see cref="ScoreHistorySO"/>         — TrendDelta for direction arrow.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────
    ///   MatchManager raises <c>_onMatchEnded</c>.  This controller subscribes that
    ///   event, calls <see cref="Refresh"/>, and shows <c>_summaryPanel</c>.
    ///   Refresh also runs on <c>OnEnable</c> so the panel reflects the most-recent
    ///   match when navigating back to a career screen.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one summary panel per canvas.
    ///   - All inspector fields optional — assign only those present in the scene.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _winStreak           → shared WinStreakSO asset.
    ///   _matchRating         → the MatchRatingController MB on the same canvas
    ///                          (reads CurrentStars after match end).
    ///   _scoreHistory        → shared ScoreHistorySO asset.
    ///   _onMatchEnded        → same VoidGameEvent as MatchManager raises.
    ///   _summaryPanel        → root panel (shown after match end).
    ///   _currentStreakText   → Text receiving "Streak: N".
    ///   _bestStreakText      → Text receiving "Best: N".
    ///   _starsText           → Text receiving "N / 5 ★".
    ///   _trendLabel          → Text receiving "↑ Improving", "↓ Declining", "↔ Steady", or "—".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchStreakSummaryController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("WinStreakSO providing CurrentStreak and BestStreak.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("MatchRatingController on the canvas — CurrentStars is read after match end.")]
        [SerializeField] private MatchRatingController _matchRating;

        [Tooltip("ScoreHistorySO providing TrendDelta for the direction arrow.")]
        [SerializeField] private ScoreHistorySO _scoreHistory;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by MatchManager at the end of each match.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root summary panel. Shown after each match end.")]
        [SerializeField] private GameObject _summaryPanel;

        [Tooltip("Text receiving 'Streak: N'.")]
        [SerializeField] private Text _currentStreakText;

        [Tooltip("Text receiving 'Best: N'.")]
        [SerializeField] private Text _bestStreakText;

        [Tooltip("Text receiving 'N / 5 \u2605'.")]
        [SerializeField] private Text _starsText;

        [Tooltip("Text receiving the score trend arrow and label.")]
        [SerializeField] private Text _trendLabel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads all data sources and updates every UI label.
        /// Safe to call with any combination of null references.
        /// </summary>
        public void Refresh()
        {
            int currentStreak = _winStreak?.CurrentStreak ?? 0;
            int bestStreak    = _winStreak?.BestStreak    ?? 0;
            int stars         = _matchRating?.CurrentStars ?? 0;

            if (_currentStreakText != null)
                _currentStreakText.text = string.Format("Streak: {0}", currentStreak);

            if (_bestStreakText != null)
                _bestStreakText.text = string.Format("Best: {0}", bestStreak);

            if (_starsText != null)
                _starsText.text = string.Format("{0} / 5 \u2605", stars);

            if (_trendLabel != null)
                _trendLabel.text = BuildTrendText();

            _summaryPanel?.SetActive(true);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private string BuildTrendText()
        {
            if (_scoreHistory == null || _scoreHistory.Scores.Count < 2)
                return "\u2014"; // em-dash

            int delta = _scoreHistory.TrendDelta;
            if      (delta > 0) return "\u2191 Improving";
            else if (delta < 0) return "\u2193 Declining";
            else                return "\u2194 Steady";
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="WinStreakSO"/>. May be null.</summary>
        public WinStreakSO WinStreak => _winStreak;

        /// <summary>The assigned <see cref="MatchRatingController"/>. May be null.</summary>
        public MatchRatingController MatchRating => _matchRating;

        /// <summary>The assigned <see cref="ScoreHistorySO"/>. May be null.</summary>
        public ScoreHistorySO ScoreHistory => _scoreHistory;
    }
}

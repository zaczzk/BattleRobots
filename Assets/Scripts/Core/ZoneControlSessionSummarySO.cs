using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that aggregates zone-control career statistics across
    /// multiple matches in a single session (or across persisted sessions when
    /// snapshot-loaded by a bootstrapper).
    ///
    /// ── Tracked metrics ────────────────────────────────────────────────────────
    ///   • TotalZonesCaptured    — cumulative zone captures across all matches.
    ///   • MatchesPlayed         — total number of matches recorded.
    ///   • MatchesWithDominance  — matches where the player held a zone majority.
    ///   • BestCaptureStreak     — highest consecutive capture streak ever recorded.
    ///   • AverageZonesPerMatch  — TotalZonesCaptured / MatchesPlayed (safe).
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Call <see cref="AddMatch"/> at match end from
    ///     <see cref="ZoneControlSessionSummaryController"/>.
    ///   • Call <see cref="LoadSnapshot"/> at startup from a bootstrapper to
    ///     restore persisted career figures.
    ///   • Call <see cref="Reset"/> to wipe all counters (career reset flow).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — relies on LoadSnapshot for persistence.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSessionSummary.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSessionSummary", order = 22)]
    public sealed class ZoneControlSessionSummarySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each AddMatch and after Reset.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _totalZonesCaptured;
        private int   _matchesPlayed;
        private int   _matchesWithDominance;
        private int   _bestCaptureStreak;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Cumulative zones captured across all recorded matches.</summary>
        public int TotalZonesCaptured => _totalZonesCaptured;

        /// <summary>Number of matches recorded since last Reset or LoadSnapshot.</summary>
        public int MatchesPlayed => _matchesPlayed;

        /// <summary>Number of matches where the player held a zone majority.</summary>
        public int MatchesWithDominance => _matchesWithDominance;

        /// <summary>Highest consecutive zone-capture streak ever recorded.</summary>
        public int BestCaptureStreak => _bestCaptureStreak;

        /// <summary>
        /// Average zones captured per match.
        /// 0 when no matches have been recorded.
        /// </summary>
        public float AverageZonesPerMatch =>
            _matchesPlayed > 0 ? (float)_totalZonesCaptured / _matchesPlayed : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records the results of one match.
        /// </summary>
        /// <param name="capturedThisMatch">
        /// Number of zone captures in the match (clamped to ≥ 0).
        /// </param>
        /// <param name="hadDominance">
        /// True if the player held a zone majority at any point during the match.
        /// </param>
        /// <param name="captureStreak">
        /// Highest consecutive capture streak achieved this match (clamped to ≥ 0).
        /// </param>
        public void AddMatch(int capturedThisMatch, bool hadDominance, int captureStreak)
        {
            _totalZonesCaptured  += Mathf.Max(0, capturedThisMatch);
            _matchesPlayed++;
            if (hadDominance) _matchesWithDominance++;
            _bestCaptureStreak    = Mathf.Max(_bestCaptureStreak, Mathf.Max(0, captureStreak));
            _onSummaryUpdated?.Raise();
        }

        /// <summary>
        /// Restores persisted career figures without firing any events.
        /// Bootstrapper-safe.
        /// All parameters are clamped to ≥ 0.
        /// </summary>
        public void LoadSnapshot(int totalCaptured, int matchesPlayed,
                                 int matchesWithDominance, int bestStreak)
        {
            _totalZonesCaptured  = Mathf.Max(0, totalCaptured);
            _matchesPlayed       = Mathf.Max(0, matchesPlayed);
            _matchesWithDominance = Mathf.Max(0, matchesWithDominance);
            _bestCaptureStreak   = Mathf.Max(0, bestStreak);
        }

        /// <summary>
        /// Zeros all counters and fires <see cref="_onSummaryUpdated"/>.
        /// </summary>
        public void Reset()
        {
            _totalZonesCaptured  = 0;
            _matchesPlayed       = 0;
            _matchesWithDominance = 0;
            _bestCaptureStreak   = 0;
            _onSummaryUpdated?.Raise();
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that records a snapshot of zone-control state at
    /// the end of each match: player/enemy scores, dominance ratio, capture streak,
    /// and whether the zone objective was completed.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   Call <see cref="Record"/> from <see cref="ZoneControlMatchSummaryController"/>
    ///   on <c>_onMatchEnded</c> to populate all fields before the post-match UI reads
    ///   them. Wire <see cref="_onSummaryUpdated"/> to any HUD controller that needs a
    ///   reactive refresh after recording.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on all hot-path methods.
    ///   - Runtime state is not serialised — resets on domain reload.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchSummary.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchSummary", order = 21)]
    public sealed class ZoneControlMatchSummarySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by Record() after all fields are populated. " +
                 "Wire to ZoneControlMatchSummaryController.Refresh.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _playerScore;
        private float _enemyScore;
        private float _dominanceRatio;
        private int   _captureStreak;
        private bool  _objectiveComplete;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Player zone-control score for the match.</summary>
        public float PlayerScore => _playerScore;

        /// <summary>Enemy zone-control score for the match.</summary>
        public float EnemyScore => _enemyScore;

        /// <summary>Player dominance ratio at match end (0–1).</summary>
        public float DominanceRatio => _dominanceRatio;

        /// <summary>Player's consecutive capture streak at match end.</summary>
        public int CaptureStreak => _captureStreak;

        /// <summary>True when the zone objective was completed this match.</summary>
        public bool ObjectiveComplete => _objectiveComplete;

        /// <summary>Event raised by <see cref="Record"/> after all fields are populated.</summary>
        public VoidGameEvent OnSummaryUpdated => _onSummaryUpdated;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Captures the current zone-control state from the supplied SOs.
        /// Any null argument is treated as its zero/false default.
        /// Raises <see cref="_onSummaryUpdated"/> after recording.
        /// Zero allocation.
        /// </summary>
        public void Record(
            ZoneScoreTrackerSO  tracker,
            ZoneDominanceSO     dominance,
            ZoneCaptureStreakSO streak,
            ZoneObjectiveSO     objective)
        {
            _playerScore       = tracker   != null ? tracker.PlayerScore        : 0f;
            _enemyScore        = tracker   != null ? tracker.EnemyScore         : 0f;
            _dominanceRatio    = dominance != null ? dominance.DominanceRatio   : 0f;
            _captureStreak     = streak    != null ? streak.CurrentStreak       : 0;
            _objectiveComplete = objective != null && objective.IsComplete;
            _onSummaryUpdated?.Raise();
        }

        /// <summary>
        /// Clears all recorded fields without firing any events.
        /// Safe to call at match start or OnEnable.
        /// </summary>
        public void Reset()
        {
            _playerScore       = 0f;
            _enemyScore        = 0f;
            _dominanceRatio    = 0f;
            _captureStreak     = 0;
            _objectiveComplete = false;
        }
    }
}

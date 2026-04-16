using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that aggregates per-match reward amounts across a session.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="AddMatchReward(int)"/> at match end to accumulate the reward.
    ///   <see cref="TotalReward"/> is the session running total.
    ///   <see cref="MatchCount"/> is the number of matches recorded this session.
    ///   <see cref="AverageReward"/> is the mean reward (0 when no matches yet).
    ///   <see cref="BestMatchReward"/> is the highest single-match reward.
    ///   Call <see cref="Reset"/> to clear all accumulators at session start.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — reset at session start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRewardSummary.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRewardSummary", order = 58)]
    public sealed class ZoneControlRewardSummarySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each AddMatchReward call.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _totalReward;
        private int _matchCount;
        private int _bestMatchReward;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Running total reward earned across all recorded matches.</summary>
        public int TotalReward => _totalReward;

        /// <summary>Number of matches recorded this session.</summary>
        public int MatchCount => _matchCount;

        /// <summary>Highest reward earned in a single match.</summary>
        public int BestMatchReward => _bestMatchReward;

        /// <summary>Mean reward per match; 0 when no matches recorded.</summary>
        public float AverageReward =>
            _matchCount == 0 ? 0f : (float)_totalReward / _matchCount;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records <paramref name="amount"/> (clamped to ≥ 0) for a completed match.
        /// Updates totals, match count, and best-match reward.
        /// Fires <see cref="_onSummaryUpdated"/>.
        /// </summary>
        public void AddMatchReward(int amount)
        {
            int clamped = Mathf.Max(0, amount);
            _totalReward += clamped;
            _matchCount++;
            if (clamped > _bestMatchReward)
                _bestMatchReward = clamped;
            _onSummaryUpdated?.Raise();
        }

        /// <summary>
        /// Clears all accumulators silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalReward     = 0;
            _matchCount      = 0;
            _bestMatchReward = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks a per-session live scoreboard for zone-control
    /// matches.  The player's score and up to <see cref="MaxBots"/> simulated bot scores
    /// are accumulated by zone-capture count.  Call <see cref="RecordPlayerCapture"/> and
    /// <see cref="RecordBotCapture"/> as captures occur.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlScoreboard.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlScoreboard", order = 38)]
    public sealed class ZoneControlScoreboardSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Number of simulated bot competitors tracked on the scoreboard.")]
        [Min(0)]
        [SerializeField] private int _maxBots = 3;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every score mutation (RecordPlayerCapture, RecordBotCapture, Reset).")]
        [SerializeField] private VoidGameEvent _onScoreboardUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _playerScore;
        private int[] _botScores;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of bot competitors.</summary>
        public int MaxBots => _maxBots;

        /// <summary>Current player zone-capture count.</summary>
        public int PlayerScore => _playerScore;

        /// <summary>
        /// Current 1-based player rank on the scoreboard (lowest = best).
        /// Returns 1 when no bots score more than the player.
        /// </summary>
        public int PlayerRank
        {
            get
            {
                if (_botScores == null) return 1;
                int rank = 1;
                foreach (int s in _botScores)
                    if (s > _playerScore) rank++;
                return rank;
            }
        }

        /// <summary>Total number of competitors tracked (player + bots).</summary>
        public int TotalCompetitors => 1 + (_botScores?.Length ?? 0);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the player's zone-capture score by one and fires
        /// <see cref="_onScoreboardUpdated"/>.
        /// </summary>
        public void RecordPlayerCapture()
        {
            _playerScore++;
            _onScoreboardUpdated?.Raise();
        }

        /// <summary>
        /// Increments the score for the bot at <paramref name="botIndex"/> and fires
        /// <see cref="_onScoreboardUpdated"/>.
        /// Out-of-range or negative <paramref name="botIndex"/> is silently ignored.
        /// </summary>
        public void RecordBotCapture(int botIndex)
        {
            if (_botScores == null || botIndex < 0 || botIndex >= _botScores.Length)
                return;
            _botScores[botIndex]++;
            _onScoreboardUpdated?.Raise();
        }

        /// <summary>
        /// Returns the score for the bot at <paramref name="botIndex"/>.
        /// Returns 0 when the index is out of range.
        /// </summary>
        public int GetBotScore(int botIndex)
        {
            if (_botScores == null || botIndex < 0 || botIndex >= _botScores.Length)
                return 0;
            return _botScores[botIndex];
        }

        /// <summary>
        /// Resets all scores to zero and fires <see cref="_onScoreboardUpdated"/>.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _playerScore = 0;
            _botScores   = new int[Mathf.Max(0, _maxBots)];
            _onScoreboardUpdated?.Raise();
        }
    }
}

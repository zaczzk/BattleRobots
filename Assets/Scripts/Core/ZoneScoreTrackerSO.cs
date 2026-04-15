using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that aggregates score points earned by the player and
    /// enemy from one or more <see cref="ControlZoneSO"/> instances during a match.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   • Wire each ControlZoneSO._onScoreTick → ZoneScoreTrackerSO.AddPlayerScore
    ///     or AddEnemyScore via a VoidGameEventListener (amount = SO.ScorePerSecond).
    ///   • Wire _onMatchEnded → Reset() to clear scores each match.
    ///   • <see cref="ZoneScoreHUDController"/> subscribes to <see cref="_onScoreUpdated"/>
    ///     to refresh the HUD bar and labels reactively.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path methods (float arithmetic only).
    ///   - Runtime state is not serialised — scores reset on domain reload.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneScoreTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneScoreTracker", order = 15)]
    public sealed class ZoneScoreTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by AddPlayerScore, AddEnemyScore, and Reset. " +
                 "Wire to ZoneScoreHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onScoreUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _playerScore;
        private float _enemyScore;

        // Career accumulators — loaded from SaveData on startup, accumulated each match.
        private float _careerPlayerScore;
        private float _careerEnemyScore;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _playerScore = 0f;
            _enemyScore  = 0f;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total score accumulated by the player from zone control this match.</summary>
        public float PlayerScore => _playerScore;

        /// <summary>Total score accumulated by the enemy from zone control this match.</summary>
        public float EnemyScore => _enemyScore;

        /// <summary>Combined player + enemy score this match.</summary>
        public float TotalScore => _playerScore + _enemyScore;

        /// <summary>Cumulative career zone score for the player across all matches.</summary>
        public float CareerPlayerScore => _careerPlayerScore;

        /// <summary>Cumulative career zone score for the enemy across all matches.</summary>
        public float CareerEnemyScore => _careerEnemyScore;

        /// <summary>Event raised whenever any score changes. May be null.</summary>
        public VoidGameEvent OnScoreUpdated => _onScoreUpdated;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> to the player's zone score.
        /// No-op for values ≤ 0. Fires <see cref="_onScoreUpdated"/> on success.
        /// Zero allocation.
        /// </summary>
        public void AddPlayerScore(float amount)
        {
            if (amount <= 0f) return;
            _playerScore += amount;
            _onScoreUpdated?.Raise();
        }

        /// <summary>
        /// Adds <paramref name="amount"/> to the enemy's zone score.
        /// No-op for values ≤ 0. Fires <see cref="_onScoreUpdated"/> on success.
        /// Zero allocation.
        /// </summary>
        public void AddEnemyScore(float amount)
        {
            if (amount <= 0f) return;
            _enemyScore += amount;
            _onScoreUpdated?.Raise();
        }

        /// <summary>
        /// Zeros both per-match scores and fires <see cref="_onScoreUpdated"/>.
        /// Does NOT clear career accumulators — call at match start or from a match-ended handler.
        /// </summary>
        public void Reset()
        {
            _playerScore = 0f;
            _enemyScore  = 0f;
            _onScoreUpdated?.Raise();
        }

        /// <summary>
        /// Adds the current match's <see cref="PlayerScore"/> and <see cref="EnemyScore"/>
        /// to the running career accumulators.
        /// Call from <see cref="ZoneCareerPersistenceController"/> at match end before
        /// persisting to <c>SaveData</c>.
        /// Zero allocation.
        /// </summary>
        public void AccumulateToCareer()
        {
            _careerPlayerScore += _playerScore;
            _careerEnemyScore  += _enemyScore;
        }

        /// <summary>
        /// Restores career accumulators from <c>SaveData</c> at startup.
        /// Bootstrapper-safe — fires no events; negative values are clamped to zero.
        /// </summary>
        /// <param name="playerScore">Persisted career player zone score.</param>
        /// <param name="enemyScore">Persisted career enemy zone score.</param>
        public void LoadSnapshot(float playerScore, float enemyScore)
        {
            _careerPlayerScore = Mathf.Max(0f, playerScore);
            _careerEnemyScore  = Mathf.Max(0f, enemyScore);
        }
    }
}

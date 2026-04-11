using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks the player's current-match and all-time
    /// best score as computed by <see cref="MatchScoreCalculator"/>.
    ///
    /// ── Mutators ──────────────────────────────────────────────────────────────
    ///   • <see cref="Submit(int)"/>       — records the score for the most recent match,
    ///     updates <see cref="BestScore"/> when a new high is set, fires two optional
    ///     VoidGameEvent channels.  Called by <see cref="MatchManager"/> in EndMatch().
    ///   • <see cref="LoadSnapshot(int)"/> — silent rehydration from SaveData (no events).
    ///     Called by <see cref="GameBootstrapper"/> on startup.
    ///   • <see cref="Reset"/>             — silent clear for fresh installs (no events).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO is mutated only through the three public mutators above (immutable otherwise).
    ///   - Both event channels are optional and null-safe.
    ///   - LoadSnapshot and Reset do NOT fire events (bootstrapper-safe).
    ///   - Submit() is called by MatchManager BEFORE _onMatchEnded fires so that
    ///     any UI subscriber already sees the updated score when the event arrives.
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PersonalBest.
    ///   2. Assign to MatchManager._personalBest.
    ///   3. Assign to GameBootstrapper._personalBest.
    ///   4. Optionally assign to PersonalBestController in the Arena scene for HUD display.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/PersonalBest",
        fileName = "PersonalBestSO")]
    public sealed class PersonalBestSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised when Submit() produces a new all-time best score. " +
                 "Use this to trigger a fanfare animation or notification. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onNewPersonalBest;

        [Tooltip("Raised every time Submit() is called (new best or not). " +
                 "Use this to refresh any score display after each match. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onScoreSubmitted;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int  _currentScore;
        private int  _bestScore;
        private bool _isNewBest;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Score from the most recently submitted match.
        /// 0 before the first submission this session.
        /// </summary>
        public int CurrentScore => _currentScore;

        /// <summary>
        /// Highest score ever submitted across all sessions.
        /// Never decreases.  Always ≥ 0.
        /// </summary>
        public int BestScore => _bestScore;

        /// <summary>
        /// <c>true</c> if the most recent <see cref="Submit(int)"/> call set a new
        /// all-time best.  Remains true until the next Submit() call overrides it.
        /// <c>false</c> on fresh instances and after <see cref="LoadSnapshot"/> /
        /// <see cref="Reset"/>.
        /// </summary>
        public bool IsNewBest => _isNewBest;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Record the score for a completed match.
        /// <para>
        /// Negative values are clamped to 0.
        /// Fires <c>_onNewPersonalBest</c> when the score strictly exceeds the current
        /// <see cref="BestScore"/>, then always fires <c>_onScoreSubmitted</c>.
        /// </para>
        /// </summary>
        /// <param name="score">
        /// Match score produced by <see cref="MatchScoreCalculator.Calculate"/>.
        /// </param>
        /// <returns><c>true</c> when this submission set a new personal best.</returns>
        public bool Submit(int score)
        {
            _currentScore = Mathf.Max(0, score);
            _isNewBest    = _currentScore > _bestScore;

            if (_isNewBest)
            {
                _bestScore = _currentScore;
                _onNewPersonalBest?.Raise();
            }

            _onScoreSubmitted?.Raise();
            return _isNewBest;
        }

        /// <summary>
        /// Silent rehydration from a <see cref="SaveData"/> snapshot.
        /// Does NOT fire any event — safe to call from <see cref="GameBootstrapper"/>.
        /// Negative values are clamped to 0.
        /// CurrentScore and IsNewBest are reset to their zero-state defaults.
        /// </summary>
        /// <param name="bestScore">
        /// Value of <c>SaveData.personalBestScore</c> loaded from disk.
        /// </param>
        public void LoadSnapshot(int bestScore)
        {
            _bestScore    = Mathf.Max(0, bestScore);
            _currentScore = 0;
            _isNewBest    = false;
        }

        /// <summary>
        /// Silently clears all fields to their defaults (all zero / false).
        /// Does NOT fire any event.  Intended for fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _currentScore = 0;
            _bestScore    = 0;
            _isNewBest    = false;
        }
    }
}

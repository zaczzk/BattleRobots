using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard that tracks the active tournament's progress.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="StartTournament"/>   — called by TournamentManager when the
    ///      player enters a tournament.  Resets all state and fires
    ///      <c>_onTournamentStarted</c>.
    ///   2. <see cref="RecordRoundResult"/> — called by TournamentManager after each
    ///      match.  On a win, increments <see cref="RoundsWon"/> and
    ///      <see cref="CurrentRound"/>; on a loss, sets
    ///      <see cref="IsEliminated"/> and ends the tournament immediately.
    ///      Fires <c>_onRoundAdvanced</c>.
    ///   3. <see cref="EndTournament"/>     — called by TournamentManager when the
    ///      player has won all rounds or been eliminated.  Clears
    ///      <see cref="IsActive"/> and fires <c>_onTournamentEnded</c>.
    ///   4. <see cref="Reset"/>             — silent clear for unit tests and fresh
    ///      installs.  Does NOT fire any event.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All VoidGameEvent channels are optional and null-safe.
    ///   - SO mutated only through the four public mutators above.
    ///   - <see cref="IsTournamentWon"/> is a pure helper, not stored state —
    ///     it needs the total-round count from <see cref="TournamentConfig"/> to decide.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TournamentState.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/TournamentState", fileName = "TournamentStateSO", order = 21)]
    public sealed class TournamentStateSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised when StartTournament() is called. " +
                 "Use to show the tournament UI or trigger camera fanfare.")]
        [SerializeField] private VoidGameEvent _onTournamentStarted;

        [Tooltip("Raised after each round result is recorded (win or loss). " +
                 "Use to refresh the bracket UI between matches.")]
        [SerializeField] private VoidGameEvent _onRoundAdvanced;

        [Tooltip("Raised when the tournament ends (victory or elimination). " +
                 "Use to show the final result screen.")]
        [SerializeField] private VoidGameEvent _onTournamentEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _isActive;
        private int  _currentRound;
        private int  _roundsWon;
        private bool _isEliminated;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>True while a tournament session is in progress.</summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// One-based index of the round currently being played or about to start.
        /// 1 before the first match, 2 after the first match, etc.
        /// </summary>
        public int CurrentRound => _currentRound;

        /// <summary>Number of individual rounds won so far (≥ 0).</summary>
        public int RoundsWon => _roundsWon;

        /// <summary>True when the player has lost a round and been knocked out.</summary>
        public bool IsEliminated => _isEliminated;

        /// <summary>
        /// Returns true when <see cref="RoundsWon"/> equals <paramref name="totalRounds"/>.
        /// Pass <see cref="TournamentConfig.RoundCount"/> as the argument.
        /// </summary>
        public bool IsTournamentWon(int totalRounds) => _roundsWon >= totalRounds && totalRounds > 0;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a new tournament session.  Resets all runtime fields, marks the
        /// tournament active, and fires <c>_onTournamentStarted</c>.
        /// </summary>
        public void StartTournament()
        {
            _isActive      = true;
            _currentRound  = 1;
            _roundsWon     = 0;
            _isEliminated  = false;

            _onTournamentStarted?.Raise();
        }

        /// <summary>
        /// Records the outcome of the most recent round.
        /// <list type="bullet">
        ///   <item>Win  — <see cref="RoundsWon"/> and <see cref="CurrentRound"/> both increment.</item>
        ///   <item>Loss — <see cref="IsEliminated"/> is set to true and <see cref="IsActive"/>
        ///                is cleared immediately (elimination is final).</item>
        /// </list>
        /// Always fires <c>_onRoundAdvanced</c>.
        /// </summary>
        public void RecordRoundResult(bool playerWon)
        {
            if (playerWon)
            {
                _roundsWon++;
                _currentRound++;
            }
            else
            {
                _isEliminated = true;
                _isActive     = false;
            }

            _onRoundAdvanced?.Raise();
        }

        /// <summary>
        /// Marks the tournament as inactive and fires <c>_onTournamentEnded</c>.
        /// Call this after the player wins all rounds to finalise the session.
        /// </summary>
        public void EndTournament()
        {
            _isActive = false;
            _onTournamentEnded?.Raise();
        }

        /// <summary>
        /// Silently resets all runtime fields to their defaults.
        /// Does NOT fire any event — safe for unit tests and fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _isActive      = false;
            _currentRound  = 0;
            _roundsWon     = 0;
            _isEliminated  = false;
        }
    }
}

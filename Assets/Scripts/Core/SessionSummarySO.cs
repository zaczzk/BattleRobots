using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Lightweight session-scoped ScriptableObject that tracks aggregate match statistics
    /// for the current play session: matches played, wins, and total currency earned.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   • <see cref="GameBootstrapper"/> calls <see cref="Reset"/> in Awake so the
    ///     summary always starts at zero for a fresh session.
    ///   • <see cref="MatchManager"/> calls <see cref="RecordMatch"/> in EndMatch after
    ///     <see cref="MatchResultSO"/> is written so all result fields are current when
    ///     the counts are updated.
    ///   • There is NO persistence — the summary is intentionally session-only.
    ///   • <see cref="Reset"/> is also silent — does not fire <c>_onSessionUpdated</c>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime fields are NOT serialized (SO asset stays immutable on disk).
    ///   - All mutators are null-safe.
    ///   - <see cref="WinRate"/> uses safe division — returns 0 when no matches played.
    ///   - <see cref="WinRatePercent"/> formats as "N%" (e.g. "50%").
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SessionSummary.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/SessionSummary",
        fileName = "SessionSummarySO")]
    public sealed class SessionSummarySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — Out")]
        [Tooltip("Raised after RecordMatch() updates the session counters. " +
                 "Use this to refresh any UI panels displaying session stats. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private VoidGameEvent _onSessionUpdated;

        // ── Runtime state (not serialized — session-only) ─────────────────────

        private int _matchesPlayed;
        private int _wins;
        private int _totalCurrencyEarned;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total matches played this session.</summary>
        public int MatchesPlayed => _matchesPlayed;

        /// <summary>Total wins recorded this session.</summary>
        public int Wins => _wins;

        /// <summary>Total currency earned across all matches this session.</summary>
        public int TotalCurrencyEarned => _totalCurrencyEarned;

        /// <summary>
        /// Win ratio in the range [0, 1].
        /// Returns 0 when no matches have been played (safe division).
        /// </summary>
        public float WinRate => _matchesPlayed > 0
            ? (float)_wins / _matchesPlayed
            : 0f;

        /// <summary>
        /// Win rate formatted as a whole-number percentage string, e.g. "50%".
        /// Rounds to nearest integer.
        /// </summary>
        public string WinRatePercent =>
            string.Format("{0}%", Mathf.RoundToInt(WinRate * 100f));

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a completed match.  Increments <see cref="MatchesPlayed"/>,
        /// conditionally increments <see cref="Wins"/> when <c>result.PlayerWon</c>
        /// is true, adds <c>result.CurrencyEarned</c> to
        /// <see cref="TotalCurrencyEarned"/>, then raises <c>_onSessionUpdated</c>.
        ///
        /// Null <paramref name="result"/> is a safe no-op.
        /// </summary>
        public void RecordMatch(MatchResultSO result)
        {
            if (result == null) return;

            _matchesPlayed++;
            if (result.PlayerWon) _wins++;
            _totalCurrencyEarned += result.CurrencyEarned;

            _onSessionUpdated?.Raise();
        }

        /// <summary>
        /// Clears all session counters.
        /// Silent — does NOT fire <c>_onSessionUpdated</c>.
        /// Called by <see cref="GameBootstrapper"/> in Awake; also useful in tests.
        /// </summary>
        public void Reset()
        {
            _matchesPlayed       = 0;
            _wins                = 0;
            _totalCurrencyEarned = 0;
        }
    }
}

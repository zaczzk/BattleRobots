using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks scores for two competing teams (Team A and Team B)
    /// and broadcasts changes via a VoidGameEvent channel.
    ///
    /// ── Use cases ────────────────────────────────────────────────────────────────
    ///   Team vs team modes, co-op scoring, or any scenario that tracks two
    ///   independent score pools against each other.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   AddTeamAScore / AddTeamBScore / ResetScores mutate the SO.
    ///   Each mutator raises _onScoreChanged → TeamScoreHUDController.Refresh().
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Negative or zero deltas are silently rejected by Add* methods.
    ///   - ResetScores always raises the event so HUD resets at match start.
    ///   - Zero allocation on every mutator path (integer arithmetic + optional Raise).
    ///   - Resets to 0 / 0 on OnEnable so Play-mode and domain-reload start clean.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TeamScore.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/TeamScore")]
    public sealed class TeamScoreSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel (optional)")]
        [Tooltip("Raised after AddTeamAScore, AddTeamBScore, and ResetScores. " +
                 "Subscribe TeamScoreHUDController to this event.")]
        [SerializeField] private VoidGameEvent _onScoreChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _teamAScore;
        private int _teamBScore;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            _teamAScore = 0;
            _teamBScore = 0;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current score for Team A.</summary>
        public int TeamAScore => _teamAScore;

        /// <summary>Current score for Team B.</summary>
        public int TeamBScore => _teamBScore;

        /// <summary>
        /// Returns "A" when Team A leads, "B" when Team B leads, or "Tie" when equal.
        /// </summary>
        public string LeadingTeam
        {
            get
            {
                if (_teamAScore > _teamBScore) return "A";
                if (_teamBScore > _teamAScore) return "B";
                return "Tie";
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="delta"/> to Team A's score.
        /// Silently ignores zero or negative deltas.
        /// Raises <c>_onScoreChanged</c> on success.
        /// </summary>
        public void AddTeamAScore(int delta)
        {
            if (delta <= 0) return;
            _teamAScore += delta;
            _onScoreChanged?.Raise();
        }

        /// <summary>
        /// Adds <paramref name="delta"/> to Team B's score.
        /// Silently ignores zero or negative deltas.
        /// Raises <c>_onScoreChanged</c> on success.
        /// </summary>
        public void AddTeamBScore(int delta)
        {
            if (delta <= 0) return;
            _teamBScore += delta;
            _onScoreChanged?.Raise();
        }

        /// <summary>
        /// Resets both scores to zero and raises <c>_onScoreChanged</c>.
        /// Call at match start (wire via VoidGameEventListener MatchStarted → ResetScores).
        /// </summary>
        public void ResetScores()
        {
            _teamAScore = 0;
            _teamBScore = 0;
            _onScoreChanged?.Raise();
        }
    }
}

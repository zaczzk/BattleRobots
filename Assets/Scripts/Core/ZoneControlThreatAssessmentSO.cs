using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Bot threat level categories used by <see cref="ZoneControlThreatAssessmentSO"/>.
    /// </summary>
    public enum ThreatLevel
    {
        Low    = 0,
        Medium = 1,
        High   = 2
    }

    /// <summary>
    /// Runtime ScriptableObject that scores the current bot threat level based on
    /// the player's scoreboard rank and whether the player holds zone dominance.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="EvaluateThreat(int, bool)"/> after every scoreboard
    ///     or dominance update.
    ///   • Threat rules (evaluated in order):
    ///       PlayerRank == 1                   → Low
    ///       PlayerRank >= _highThreatRank
    ///         AND !hasDominance              → High
    ///       PlayerRank >= _mediumThreatRank  → Medium
    ///       Otherwise                         → Low
    ///   • <see cref="_onThreatChanged"/> fires only when the level actually changes.
    ///   • Call <see cref="Reset"/> at match start (called automatically by OnEnable).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlThreatAssessment.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlThreatAssessment", order = 47)]
    public sealed class ZoneControlThreatAssessmentSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Threat Thresholds")]
        [Tooltip("Minimum player rank (1-based) at which threat level rises to Medium.")]
        [Min(2)]
        [SerializeField] private int _mediumThreatRank = 2;

        [Tooltip("Minimum player rank (1-based) at which threat level rises to High " +
                 "(only when the player does not have zone dominance).")]
        [Min(2)]
        [SerializeField] private int _highThreatRank = 3;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised whenever the threat level changes.")]
        [SerializeField] private VoidGameEvent _onThreatChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ThreatLevel _currentThreat;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current assessed threat level.</summary>
        public ThreatLevel CurrentThreat => _currentThreat;

        /// <summary>Rank threshold at which threat becomes at least Medium.</summary>
        public int MediumThreatRank => _mediumThreatRank;

        /// <summary>Rank threshold at which threat becomes High (when lacking dominance).</summary>
        public int HighThreatRank => _highThreatRank;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the threat level from the current scoreboard rank and dominance state.
        /// Fires <see cref="_onThreatChanged"/> only when the level changes.
        /// </summary>
        /// <param name="playerRank">
        /// 1-based player rank on the scoreboard (1 = leading, higher = falling behind).
        /// </param>
        /// <param name="hasDominance">
        /// True when the player holds a zone majority on the map.
        /// </param>
        public void EvaluateThreat(int playerRank, bool hasDominance)
        {
            ThreatLevel newThreat;

            if (playerRank <= 1)
            {
                newThreat = ThreatLevel.Low;
            }
            else if (playerRank >= _highThreatRank && !hasDominance)
            {
                newThreat = ThreatLevel.High;
            }
            else if (playerRank >= _mediumThreatRank)
            {
                newThreat = ThreatLevel.Medium;
            }
            else
            {
                newThreat = ThreatLevel.Low;
            }

            if (newThreat == _currentThreat) return;

            _currentThreat = newThreat;
            _onThreatChanged?.Raise();
        }

        /// <summary>
        /// Resets threat to <see cref="ThreatLevel.Low"/> silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _currentThreat = ThreatLevel.Low;
        }
    }
}

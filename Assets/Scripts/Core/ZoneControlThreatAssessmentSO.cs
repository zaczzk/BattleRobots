using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Bot threat level based on scoreboard rank and zone dominance.</summary>
    public enum ThreatLevel { Low = 0, Medium = 1, High = 2 }

    /// <summary>
    /// Runtime ScriptableObject that scores the current bot threat level based on the
    /// player's scoreboard rank and whether the player holds zone dominance.
    ///
    /// ── Threat rules ────────────────────────────────────────────────────────────
    ///   • Player has dominance OR rank 1  → <see cref="ThreatLevel.Low"/>
    ///   • Player rank 2 (no dominance)    → <see cref="ThreatLevel.Medium"/>
    ///   • Player rank ≥ 3 (no dominance)  → <see cref="ThreatLevel.High"/>
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - <see cref="_onThreatChanged"/> fires only when the level actually changes.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlThreatAssessment.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlThreatAssessment", order = 47)]
    public sealed class ZoneControlThreatAssessmentSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the computed threat level changes.")]
        [SerializeField] private VoidGameEvent _onThreatChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ThreatLevel _currentThreat = ThreatLevel.Low;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Most recently computed threat level.</summary>
        public ThreatLevel CurrentThreat => _currentThreat;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Pure function that determines threat level from scoreboard rank and dominance.
        /// Does not mutate state or fire events.
        /// </summary>
        public static ThreatLevel ComputeLevel(int playerRank, bool hasDominance)
        {
            if (hasDominance || playerRank <= 1) return ThreatLevel.Low;
            if (playerRank == 2)                 return ThreatLevel.Medium;
            return ThreatLevel.High;
        }

        /// <summary>
        /// Computes the threat level from <paramref name="playerRank"/> and
        /// <paramref name="hasDominance"/>.  Fires <see cref="_onThreatChanged"/>
        /// only when the level differs from the previous evaluation.
        /// </summary>
        /// <returns>The new (or unchanged) threat level.</returns>
        public ThreatLevel EvaluateThreat(int playerRank, bool hasDominance)
        {
            ThreatLevel newLevel = ComputeLevel(playerRank, hasDominance);
            if (newLevel != _currentThreat)
            {
                _currentThreat = newLevel;
                _onThreatChanged?.Raise();
            }
            return _currentThreat;
        }

        /// <summary>
        /// Resets the threat level to <see cref="ThreatLevel.Low"/> silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _currentThreat = ThreatLevel.Low;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that monitors the score gap between player and bot and
    /// activates a catch-up bonus when the bot leads by too many zones.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="EvaluateFairness"/> compares player and bot scores.
    ///   When the bot leads by ≥ <c>_gapThreshold</c> zones and catch-up is
    ///   not already active, fires <c>_onCatchUpActivated</c>.
    ///   When the gap closes while catch-up is active, fires
    ///   <c>_onCatchUpDeactivated</c>.
    ///   <see cref="GetCatchUpBonus"/> returns the per-capture bonus when active,
    ///   or 0 when inactive.
    ///   <see cref="Reset"/> clears runtime state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchFairness.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchFairness", order = 76)]
    public sealed class ZoneControlMatchFairnessSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Fairness Settings")]
        [Tooltip("Minimum zone-count gap (bot ahead) before catch-up activates.")]
        [Min(1)]
        [SerializeField] private int _gapThreshold = 5;

        [Tooltip("Bonus credits awarded to the player per zone captured while catch-up is active.")]
        [Min(0)]
        [SerializeField] private int _catchUpBonusPerZone = 10;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCatchUpActivated;
        [SerializeField] private VoidGameEvent _onCatchUpDeactivated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool _catchUpActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        public bool IsCatchUpActive      => _catchUpActive;
        public int  GapThreshold         => _gapThreshold;
        public int  CatchUpBonusPerZone  => _catchUpBonusPerZone;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates fairness based on current scores.
        /// Activates or deactivates catch-up and fires the appropriate events.
        /// </summary>
        public void EvaluateFairness(int playerScore, int botScore)
        {
            int gap = botScore - playerScore;
            bool shouldActivate = gap >= _gapThreshold;

            if (shouldActivate && !_catchUpActive)
            {
                _catchUpActive = true;
                _onCatchUpActivated?.Raise();
            }
            else if (!shouldActivate && _catchUpActive)
            {
                _catchUpActive = false;
                _onCatchUpDeactivated?.Raise();
            }
        }

        /// <summary>
        /// Returns the per-capture catch-up bonus when active, or 0 when inactive.
        /// </summary>
        public int GetCatchUpBonus() => _catchUpActive ? _catchUpBonusPerZone : 0;

        /// <summary>Clears all runtime state silently.  Called from <c>OnEnable</c>.</summary>
        public void Reset()
        {
            _catchUpActive = false;
        }
    }
}

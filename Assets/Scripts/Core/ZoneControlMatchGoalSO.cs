using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Defines the two supported zone-control win conditions.
    /// </summary>
    public enum ZoneControlMatchGoalType
    {
        /// <summary>The first player to reach <c>CaptureTarget</c> zone captures wins.</summary>
        FirstToCaptures  = 0,

        /// <summary>The player with the most zones when <c>TimeLimitSeconds</c> elapses wins.</summary>
        MostZonesInTime  = 1,
    }

    /// <summary>
    /// Data ScriptableObject that encodes a configurable pre-match win condition for
    /// zone-control matches.
    ///
    /// ── Goal types ──────────────────────────────────────────────────────────────
    ///   FirstToCaptures → player wins when <c>playerScore >= CaptureTarget</c>.
    ///   MostZonesInTime → goal is met (match can end) when <c>timeElapsed >= TimeLimitSeconds</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime (inspector data only).
    ///   - OnValidate clamps all fields to their valid ranges.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchGoal.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchGoal", order = 42)]
    public sealed class ZoneControlMatchGoalSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Goal Settings")]
        [Tooltip("Win condition type for this goal configuration.")]
        [SerializeField] private ZoneControlMatchGoalType _goalType = ZoneControlMatchGoalType.FirstToCaptures;

        [Tooltip("Zone-capture count required to win (FirstToCaptures mode).")]
        [Min(1)]
        [SerializeField] private int _captureTarget = 10;

        [Tooltip("Match duration in seconds before the goal is considered met (MostZonesInTime mode).")]
        [Min(1f)]
        [SerializeField] private float _timeLimitSeconds = 120f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Active win-condition type.</summary>
        public ZoneControlMatchGoalType GoalType => _goalType;

        /// <summary>Zone-capture count required for a FirstToCaptures win.</summary>
        public int CaptureTarget => _captureTarget;

        /// <summary>Match duration (seconds) for a MostZonesInTime goal.</summary>
        public float TimeLimitSeconds => _timeLimitSeconds;

        /// <summary>
        /// Human-readable one-line description of the goal, suitable for a HUD label.
        /// </summary>
        public string GoalDescription
        {
            get
            {
                return _goalType == ZoneControlMatchGoalType.FirstToCaptures
                    ? $"First to {_captureTarget} zones"
                    : $"Most zones in {_timeLimitSeconds:F0}s";
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether the match goal has been met.
        /// </summary>
        /// <param name="playerScore">Player's current zone-capture count.</param>
        /// <param name="timeElapsed">Elapsed match time in seconds.</param>
        /// <returns>
        /// <c>true</c> when the active goal condition is satisfied.
        /// </returns>
        public bool IsGoalMet(int playerScore, int timeElapsed)
        {
            return _goalType == ZoneControlMatchGoalType.FirstToCaptures
                ? playerScore >= _captureTarget
                : timeElapsed >= (int)_timeLimitSeconds;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            _captureTarget    = Mathf.Max(1, _captureTarget);
            _timeLimitSeconds = Mathf.Max(1f, _timeLimitSeconds);
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Determines which metric drives the career goal.</summary>
    public enum ZoneControlCareerGoalType
    {
        TotalCaptures = 0,
        TotalWins     = 1,
        TotalMatches  = 2,
    }

    /// <summary>
    /// Persistent cross-session career goal that tracks cumulative progress
    /// towards a configurable target metric.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="AddProgress"/> clamps the amount to zero and accumulates it.
    ///   When <see cref="AccumulatedValue"/> first reaches <c>_targetValue</c>,
    ///   <c>_isAchieved</c> is set and <c>_onGoalAchieved</c> fires (idempotent
    ///   thereafter).  <see cref="LoadSnapshot"/> restores state without firing
    ///   events (bootstrapper-safe).  <see cref="Reset"/> clears state silently.
    ///   <c>OnEnable</c> calls <see cref="Reset"/> to prevent cross-session leaks.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCareerGoal.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCareerGoal", order = 69)]
    public sealed class ZoneControlCareerGoalSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [SerializeField] private ZoneControlCareerGoalType _goalType = ZoneControlCareerGoalType.TotalCaptures;

        [Tooltip("The cumulative target value to achieve this goal.")]
        [Min(1)]
        [SerializeField] private int _targetValue = 50;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once when the goal is first achieved.")]
        [SerializeField] private VoidGameEvent _onGoalAchieved;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int  _accumulatedValue;
        private bool _isAchieved;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Which metric drives this goal.</summary>
        public ZoneControlCareerGoalType GoalType => _goalType;

        /// <summary>The cumulative target to reach.</summary>
        public int TargetValue => _targetValue;

        /// <summary>Total progress accumulated so far.</summary>
        public int AccumulatedValue => _accumulatedValue;

        /// <summary>True once <see cref="AccumulatedValue"/> has reached <see cref="TargetValue"/>.</summary>
        public bool IsAchieved => _isAchieved;

        /// <summary>Fraction complete clamped to [0, 1].</summary>
        public float Progress => Mathf.Clamp01((float)_accumulatedValue / Mathf.Max(1, _targetValue));

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="amount"/> (clamped to ≥ 0) to the accumulator.
        /// When the total first reaches <c>_targetValue</c>, <c>_onGoalAchieved</c>
        /// is fired.  Idempotent once <see cref="IsAchieved"/> is true.
        /// </summary>
        public void AddProgress(int amount)
        {
            if (_isAchieved) return;
            if (amount <= 0) return;

            _accumulatedValue += amount;

            if (_accumulatedValue >= _targetValue)
            {
                _isAchieved = true;
                _onGoalAchieved?.Raise();
            }
        }

        /// <summary>
        /// Restores accumulated state without firing events (bootstrapper-safe).
        /// </summary>
        public void LoadSnapshot(int accumulatedValue, bool isAchieved)
        {
            _accumulatedValue = Mathf.Max(0, accumulatedValue);
            _isAchieved       = isAchieved;
        }

        /// <summary>Returns the current state as a snapshot tuple.</summary>
        public (int accumulatedValue, bool isAchieved) TakeSnapshot() =>
            (_accumulatedValue, _isAchieved);

        /// <summary>
        /// Clears accumulated state silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _accumulatedValue = 0;
            _isAchieved       = false;
        }

        private void OnValidate()
        {
            _targetValue = Mathf.Max(1, _targetValue);
        }
    }
}

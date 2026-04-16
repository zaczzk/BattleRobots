using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that awards escalating rewards when the player's
    /// consecutive zone-capture streak crosses configurable milestones.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="EvaluateStreak"/> with the current streak value.
    ///   For each milestone threshold newly crossed, <see cref="_onMilestoneReached"/>
    ///   is fired once and <see cref="TotalRewardAwarded"/> accumulates the reward.
    ///   Milestones are tracked by index; already-crossed tiers are not re-fired.
    ///   Call <see cref="Reset"/> at match start to clear progress.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at session start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureStreakReward.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStreakReward", order = 53)]
    public sealed class ZoneControlCaptureStreakRewardSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Milestone Settings")]
        [Tooltip("Streak values at which rewards are granted (ascending order).")]
        [SerializeField] private int[] _streakMilestones = { 3, 5, 10, 20 };

        [Tooltip("Currency awarded each time a milestone is crossed.")]
        [Min(0)]
        [SerializeField] private int _rewardPerMilestone = 100;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once for each milestone newly crossed.")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _unlockedMilestoneCount;
        private int _totalRewardAwarded;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Streak milestones array (read-only view).</summary>
        public int[] StreakMilestones => _streakMilestones;

        /// <summary>Currency rewarded per milestone crossed.</summary>
        public int RewardPerMilestone => _rewardPerMilestone;

        /// <summary>Number of milestones crossed so far this match.</summary>
        public int UnlockedMilestoneCount => _unlockedMilestoneCount;

        /// <summary>Total reward accumulated from all crossed milestones.</summary>
        public int TotalRewardAwarded => _totalRewardAwarded;

        /// <summary>
        /// Next streak value required for a reward. Returns -1 when all milestones
        /// have been crossed.
        /// </summary>
        public int NextMilestone
        {
            get
            {
                if (_streakMilestones == null || _unlockedMilestoneCount >= _streakMilestones.Length)
                    return -1;
                return _streakMilestones[_unlockedMilestoneCount];
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the current <paramref name="streak"/> against all pending
        /// milestones. Fires <see cref="_onMilestoneReached"/> and accumulates
        /// <see cref="TotalRewardAwarded"/> for each newly crossed tier.
        /// </summary>
        public void EvaluateStreak(int streak)
        {
            if (_streakMilestones == null) return;

            while (_unlockedMilestoneCount < _streakMilestones.Length
                   && streak >= _streakMilestones[_unlockedMilestoneCount])
            {
                _unlockedMilestoneCount++;
                _totalRewardAwarded += _rewardPerMilestone;
                _onMilestoneReached?.Raise();
            }
        }

        /// <summary>
        /// Clears milestone progress and total reward silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _unlockedMilestoneCount = 0;
            _totalRewardAwarded     = 0;
        }

        private void OnValidate()
        {
            _rewardPerMilestone = Mathf.Max(0, _rewardPerMilestone);
        }
    }
}

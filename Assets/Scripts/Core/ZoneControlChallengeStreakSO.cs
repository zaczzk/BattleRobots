using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks consecutive challenge completions and provides
    /// escalating currency rewards for maintaining a streak.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="RecordCompletion"/> increments the streak, calculates the
    ///   reward (<c>_rewardBase + streak × _rewardPerStreak</c>), accumulates
    ///   <c>TotalRewardsEarned</c>, and fires <c>_onStreakIncreased</c>.
    ///   <see cref="RecordFailure"/> resets the streak to 0 and fires
    ///   <c>_onStreakBroken</c> (only when the streak was > 0).
    ///   <see cref="GetCurrentReward"/> returns the reward for the next
    ///   completion at the current streak level.
    ///   <see cref="Reset"/> clears all runtime state silently; called from
    ///   <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlChallengeStreak.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlChallengeStreak", order = 76)]
    public sealed class ZoneControlChallengeStreakSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Reward Settings")]
        [Tooltip("Base currency reward for completing a challenge.")]
        [Min(0)]
        [SerializeField] private int _rewardBase = 50;

        [Tooltip("Additional currency reward added per consecutive completion.")]
        [Min(0)]
        [SerializeField] private int _rewardPerStreak = 25;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStreakIncreased;
        [SerializeField] private VoidGameEvent _onStreakBroken;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _streakCount;
        private int _totalRewardsEarned;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        public int StreakCount        => _streakCount;
        public int TotalRewardsEarned => _totalRewardsEarned;
        public int RewardBase         => _rewardBase;
        public int RewardPerStreak    => _rewardPerStreak;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a successful challenge completion, increments the streak,
        /// accumulates rewards, and fires <c>_onStreakIncreased</c>.
        /// </summary>
        public void RecordCompletion()
        {
            _streakCount++;
            int reward = GetCurrentReward();
            _totalRewardsEarned += reward;
            _onStreakIncreased?.Raise();
        }

        /// <summary>
        /// Records a challenge failure.  Resets the streak to 0 and fires
        /// <c>_onStreakBroken</c> when the streak was > 0.
        /// </summary>
        public void RecordFailure()
        {
            if (_streakCount > 0)
            {
                _streakCount = 0;
                _onStreakBroken?.Raise();
            }
        }

        /// <summary>
        /// Returns the reward amount for completing a challenge at the current streak.
        /// </summary>
        public int GetCurrentReward() => _rewardBase + _streakCount * _rewardPerStreak;

        /// <summary>Clears all runtime state silently.  Called from <c>OnEnable</c>.</summary>
        public void Reset()
        {
            _streakCount        = 0;
            _totalRewardsEarned = 0;
        }
    }
}

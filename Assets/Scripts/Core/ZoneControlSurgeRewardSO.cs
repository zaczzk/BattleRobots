using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that awards a per-capture wallet bonus whenever a capture is
    /// recorded while a <see cref="ZoneControlSurgeDetectorSO"/> surge is active.
    ///
    /// Call <see cref="RecordCaptureDuringSurge"/> each time a capture occurs
    /// within an active surge.  The wallet credit is applied by the controller.
    /// <see cref="Reset"/> clears all runtime state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSurgeReward.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSurgeReward", order = 94)]
    public sealed class ZoneControlSurgeRewardSO : ScriptableObject
    {
        [Header("Surge Reward Settings")]
        [Min(0)]
        [SerializeField] private int _rewardPerCapture = 25;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSurgeRewardAwarded;

        private int _surgeCaptures;
        private int _totalSurgeReward;

        private void OnEnable() => Reset();

        public int RewardPerCapture => _rewardPerCapture;
        public int SurgeCaptures    => _surgeCaptures;
        public int TotalSurgeReward => _totalSurgeReward;

        /// <summary>
        /// Records one capture that occurred during an active surge.  Increments
        /// <see cref="SurgeCaptures"/>, accumulates <see cref="TotalSurgeReward"/>,
        /// and fires <c>_onSurgeRewardAwarded</c>.
        /// </summary>
        public void RecordCaptureDuringSurge()
        {
            _surgeCaptures++;
            _totalSurgeReward += _rewardPerCapture;
            _onSurgeRewardAwarded?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _surgeCaptures    = 0;
            _totalSurgeReward = 0;
        }
    }
}

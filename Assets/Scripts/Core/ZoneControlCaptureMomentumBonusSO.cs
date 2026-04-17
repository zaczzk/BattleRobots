using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that awards a per-capture wallet bonus when the player captures
    /// a zone while the <see cref="ZoneControlMomentumTrackerSO"/> is in burst mode.
    ///
    /// The controller checks <c>ZoneControlMomentumTrackerSO.IsBurst</c> before
    /// calling <see cref="RecordBurstCapture"/>; the SO itself only accumulates.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureMomentumBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMomentumBonus", order = 90)]
    public sealed class ZoneControlCaptureMomentumBonusSO : ScriptableObject
    {
        [Header("Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _rewardPerBurstCapture = 30;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBurstBonusAwarded;

        private int _burstCaptureCount;
        private int _totalBurstReward;

        private void OnEnable() => Reset();

        public int RewardPerBurstCapture => _rewardPerBurstCapture;
        public int BurstCaptureCount     => _burstCaptureCount;
        public int TotalBurstReward      => _totalBurstReward;

        /// <summary>Records one burst-capture, accumulates reward, and fires the bonus event.</summary>
        public void RecordBurstCapture()
        {
            _burstCaptureCount++;
            _totalBurstReward += _rewardPerBurstCapture;
            _onBurstBonusAwarded?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _burstCaptureCount = 0;
            _totalBurstReward  = 0;
        }
    }
}

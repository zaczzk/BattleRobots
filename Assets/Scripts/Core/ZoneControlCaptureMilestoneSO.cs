using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMilestone", order = 101)]
    public sealed class ZoneControlCaptureMilestoneSO : ScriptableObject
    {
        [Header("Milestone Settings")]
        [SerializeField] private int[] _milestones = { 5, 10, 20, 35, 50 };

        [Min(0)]
        [SerializeField] private int _bonusPerMilestone = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        private int _captureCount;
        private int _milestonesReached;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int CaptureCount      => _captureCount;
        public int MilestonesReached => _milestonesReached;
        public int TotalBonusAwarded => _totalBonusAwarded;
        public int BonusPerMilestone => _bonusPerMilestone;
        public int MilestoneCount    => _milestones?.Length ?? 0;

        public int NextMilestoneTarget =>
            (_milestones != null && _milestonesReached < _milestones.Length)
                ? _milestones[_milestonesReached]
                : -1;

        public void RecordCapture()
        {
            _captureCount++;
            if (_milestones == null) return;
            while (_milestonesReached < _milestones.Length &&
                   _captureCount >= _milestones[_milestonesReached])
            {
                _milestonesReached++;
                _totalBonusAwarded += _bonusPerMilestone;
                _onMilestoneReached?.Raise();
            }
        }

        public void Reset()
        {
            _captureCount      = 0;
            _milestonesReached = 0;
            _totalBonusAwarded = 0;
        }
    }
}

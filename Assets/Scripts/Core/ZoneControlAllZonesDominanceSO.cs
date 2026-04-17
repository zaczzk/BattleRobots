using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAllZonesDominance", order = 95)]
    public sealed class ZoneControlAllZonesDominanceSO : ScriptableObject
    {
        [Header("Dominance Settings")]
        [Min(1f)]
        [SerializeField] private float _milestoneInterval = 20f;

        [Min(0)]
        [SerializeField] private int _bonusPerMilestone = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        private bool  _isDominating;
        private float _accumulatedTime;
        private int   _milestonesReached;

        private void OnEnable() => Reset();

        public bool  IsDominating      => _isDominating;
        public float AccumulatedTime   => _accumulatedTime;
        public int   MilestonesReached => _milestonesReached;
        public float MilestoneInterval => _milestoneInterval;
        public int   BonusPerMilestone => _bonusPerMilestone;

        public float DominanceProgress => _milestoneInterval > 0f
            ? Mathf.Clamp01((_accumulatedTime % _milestoneInterval) / _milestoneInterval)
            : 0f;

        public void StartDominating()
        {
            _isDominating = true;
        }

        public void StopDominating()
        {
            _isDominating = false;
        }

        public void Tick(float dt)
        {
            if (!_isDominating) return;
            _accumulatedTime += dt;
            while (_accumulatedTime >= (_milestonesReached + 1) * _milestoneInterval)
            {
                _milestonesReached++;
                _onMilestoneReached?.Raise();
            }
        }

        public void Reset()
        {
            _isDominating      = false;
            _accumulatedTime   = 0f;
            _milestonesReached = 0;
        }
    }
}

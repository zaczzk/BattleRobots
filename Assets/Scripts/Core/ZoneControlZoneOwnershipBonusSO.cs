using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneOwnershipBonus", order = 101)]
    public sealed class ZoneControlZoneOwnershipBonusSO : ScriptableObject
    {
        [Header("Ownership Bonus Settings")]
        [Min(0.5f)]
        [SerializeField] private float _bonusInterval = 5f;

        [Min(0)]
        [SerializeField] private int _bonusPerZonePerInterval = 20;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOwnershipBonusAwarded;

        private bool  _isRunning;
        private float _elapsed;
        private int   _totalIntervalsCompleted;

        private void OnEnable() => Reset();

        public bool  IsRunning               => _isRunning;
        public float BonusInterval           => _bonusInterval;
        public int   BonusPerZonePerInterval => _bonusPerZonePerInterval;
        public int   TotalIntervalsCompleted => _totalIntervalsCompleted;

        public void StartTracking()
        {
            if (_isRunning) return;
            _isRunning = true;
        }

        public void StopTracking() => _isRunning = false;

        public void Tick(float dt)
        {
            if (!_isRunning) return;
            _elapsed += dt;
            while (_elapsed >= _bonusInterval)
            {
                _elapsed -= _bonusInterval;
                _totalIntervalsCompleted++;
                _onOwnershipBonusAwarded?.Raise();
            }
        }

        public int ComputeBonus(int zonesOwned) => zonesOwned * _bonusPerZonePerInterval;

        public void Reset()
        {
            _isRunning               = false;
            _elapsed                 = 0f;
            _totalIntervalsCompleted = 0;
        }
    }
}

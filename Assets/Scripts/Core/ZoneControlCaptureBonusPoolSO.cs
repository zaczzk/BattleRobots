using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBonusPool", order = 153)]
    public sealed class ZoneControlCaptureBonusPoolSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _fillPerCapture       = 30;
        [SerializeField, Min(0)] private int _drainPerBotCapture   = 20;
        [SerializeField, Min(1)] private int _poolCapacity         = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPoolAwarded;

        private int _currentPool;
        private int _totalAwarded;
        private int _awardCount;

        private void OnEnable() => Reset();

        public int CurrentPool   => _currentPool;
        public int PoolCapacity  => _poolCapacity;
        public int TotalAwarded  => _totalAwarded;
        public int AwardCount    => _awardCount;
        public float PoolProgress => Mathf.Clamp01((float)_currentPool / _poolCapacity);

        public void RecordPlayerCapture()
        {
            _currentPool = Mathf.Min(_currentPool + _fillPerCapture, _poolCapacity);
            if (_currentPool >= _poolCapacity)
                AwardPool();
        }

        public void RecordBotCapture()
        {
            _currentPool = Mathf.Max(0, _currentPool - _drainPerBotCapture);
        }

        public int DrainPool()
        {
            int amount = _currentPool;
            _currentPool = 0;
            return amount;
        }

        public void Reset()
        {
            _currentPool  = 0;
            _totalAwarded = 0;
            _awardCount   = 0;
        }

        private void AwardPool()
        {
            _totalAwarded += _currentPool;
            _awardCount++;
            _currentPool = 0;
            _onPoolAwarded?.Raise();
        }
    }
}

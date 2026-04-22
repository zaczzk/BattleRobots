using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCache", order = 340)]
    public sealed class ZoneControlCaptureCacheSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _slotsNeeded   = 6;
        [SerializeField, Min(1)] private int _evictPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerHit   = 1840;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCacheHit;

        private int _slots;
        private int _hitCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SlotsNeeded       => _slotsNeeded;
        public int   EvictPerBot       => _evictPerBot;
        public int   BonusPerHit       => _bonusPerHit;
        public int   Slots             => _slots;
        public int   HitCount          => _hitCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SlotProgress      => _slotsNeeded > 0
            ? Mathf.Clamp01(_slots / (float)_slotsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _slots = Mathf.Min(_slots + 1, _slotsNeeded);
            if (_slots >= _slotsNeeded)
            {
                int bonus = _bonusPerHit;
                _hitCount++;
                _totalBonusAwarded += bonus;
                _slots              = 0;
                _onCacheHit?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _slots = Mathf.Max(0, _slots - _evictPerBot);
        }

        public void Reset()
        {
            _slots             = 0;
            _hitCount          = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHash", order = 350)]
    public sealed class ZoneControlCaptureHashSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bucketsNeeded  = 7;
        [SerializeField, Min(1)] private int _collisionPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerHash   = 1990;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHashResolved;

        private int _buckets;
        private int _hashCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BucketsNeeded     => _bucketsNeeded;
        public int   CollisionPerBot   => _collisionPerBot;
        public int   BonusPerHash      => _bonusPerHash;
        public int   Buckets           => _buckets;
        public int   HashCount         => _hashCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BucketProgress    => _bucketsNeeded > 0
            ? Mathf.Clamp01(_buckets / (float)_bucketsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _buckets = Mathf.Min(_buckets + 1, _bucketsNeeded);
            if (_buckets >= _bucketsNeeded)
            {
                int bonus = _bonusPerHash;
                _hashCount++;
                _totalBonusAwarded += bonus;
                _buckets            = 0;
                _onHashResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _buckets = Mathf.Max(0, _buckets - _collisionPerBot);
        }

        public void Reset()
        {
            _buckets           = 0;
            _hashCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

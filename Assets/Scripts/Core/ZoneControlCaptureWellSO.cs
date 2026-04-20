using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureWell", order = 247)]
    public sealed class ZoneControlCaptureWellSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bucketsNeeded  = 5;
        [SerializeField, Min(1)] private int _drainPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerDraw   = 495;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWellDrawn;

        private int _buckets;
        private int _drawCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BucketsNeeded     => _bucketsNeeded;
        public int   DrainPerBot       => _drainPerBot;
        public int   BonusPerDraw      => _bonusPerDraw;
        public int   Buckets           => _buckets;
        public int   DrawCount         => _drawCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BucketProgress    => _bucketsNeeded > 0
            ? Mathf.Clamp01(_buckets / (float)_bucketsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _buckets = Mathf.Min(_buckets + 1, _bucketsNeeded);
            if (_buckets >= _bucketsNeeded)
            {
                int bonus = _bonusPerDraw;
                _drawCount++;
                _totalBonusAwarded += bonus;
                _buckets            = 0;
                _onWellDrawn?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _buckets = Mathf.Max(0, _buckets - _drainPerBot);
        }

        public void Reset()
        {
            _buckets           = 0;
            _drawCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

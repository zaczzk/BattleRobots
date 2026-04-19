using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTimingBonus", order = 159)]
    public sealed class ZoneControlCaptureTimingBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _targetGap      = 5f;
        [SerializeField, Min(0f)]   private float _tolerance      = 1f;
        [SerializeField, Min(0)]    private int   _bonusPerOnTime = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTimingBonus;

        private float _lastCaptureTime;
        private bool  _hasFirst;
        private int   _onTimeCount;
        private int   _totalTimingBonus;

        private void OnEnable() => Reset();

        public int   OnTimeCount      => _onTimeCount;
        public int   TotalTimingBonus => _totalTimingBonus;
        public float TargetGap        => _targetGap;
        public float Tolerance        => _tolerance;
        public int   BonusPerOnTime   => _bonusPerOnTime;

        public void RecordCapture(float t)
        {
            if (!_hasFirst)
            {
                _lastCaptureTime = t;
                _hasFirst        = true;
                return;
            }

            float gap = t - _lastCaptureTime;
            _lastCaptureTime = t;

            if (gap >= _targetGap - _tolerance && gap <= _targetGap + _tolerance)
            {
                _onTimeCount++;
                _totalTimingBonus += _bonusPerOnTime;
                _onTimingBonus?.Raise();
            }
        }

        public void Reset()
        {
            _lastCaptureTime  = 0f;
            _hasFirst         = false;
            _onTimeCount      = 0;
            _totalTimingBonus = 0;
        }
    }
}

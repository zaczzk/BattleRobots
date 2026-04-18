using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStreakMeter", order = 135)]
    public sealed class ZoneControlCaptureStreakMeterSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)] private float _fillPerCapture    = 20f;
        [SerializeField, Min(0f)] private float _drainOnBotCapture = 15f;
        [SerializeField, Min(0)]  private int   _bonusOnFill       = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMeterFull;

        private float _meterValue;
        private int   _fillCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float FillPerCapture    => _fillPerCapture;
        public float DrainOnBotCapture => _drainOnBotCapture;
        public int   BonusOnFill       => _bonusOnFill;
        public float MeterValue        => _meterValue;
        public float MeterProgress     => Mathf.Clamp01(_meterValue / 100f);
        public int   FillCount         => _fillCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;

        public void RecordPlayerCapture()
        {
            _meterValue = Mathf.Min(_meterValue + _fillPerCapture, 100f);
            if (_meterValue >= 100f)
                TriggerFill();
        }

        public void RecordBotCapture()
        {
            _meterValue = Mathf.Max(0f, _meterValue - _drainOnBotCapture);
        }

        public void Reset()
        {
            _meterValue        = 0f;
            _fillCount         = 0;
            _totalBonusAwarded = 0;
        }

        private void TriggerFill()
        {
            _fillCount++;
            _totalBonusAwarded += _bonusOnFill;
            _meterValue         = 0f;
            _onMeterFull?.Raise();
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePulse", order = 179)]
    public sealed class ZoneControlCapturePulseSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pulseThreshold = 5;
        [SerializeField, Min(0)] private int _bonusPerPulse  = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPulse;

        private int _chargeCount;
        private int _pulseCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PulseThreshold    => _pulseThreshold;
        public int   BonusPerPulse     => _bonusPerPulse;
        public int   ChargeCount       => _chargeCount;
        public int   PulseCount        => _pulseCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ChargeProgress    => _pulseThreshold > 0 ? Mathf.Clamp01((float)_chargeCount / _pulseThreshold) : 0f;

        public void RecordCapture()
        {
            _chargeCount++;
            if (_chargeCount >= _pulseThreshold)
            {
                _pulseCount++;
                _totalBonusAwarded += _bonusPerPulse;
                _chargeCount = 0;
                _onPulse?.Raise();
            }
        }

        public void Reset()
        {
            _chargeCount       = 0;
            _pulseCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

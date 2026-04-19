using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePrismatic", order = 190)]
    public sealed class ZoneControlCapturePrismaticSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pulseEveryN   = 8;
        [SerializeField, Min(0)] private int _bonusPerPulse = 225;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPrismaticPulse;

        private int _totalCaptures;
        private int _pulseCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PulseEveryN       => _pulseEveryN;
        public int   BonusPerPulse     => _bonusPerPulse;
        public int   TotalCaptures     => _totalCaptures;
        public int   PulseCount        => _pulseCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PulseProgress     => _pulseEveryN > 0
            ? Mathf.Clamp01((_totalCaptures % _pulseEveryN) / (float)_pulseEveryN)
            : 0f;
        public int   CapturesUntilPulse => _pulseEveryN - (_totalCaptures % _pulseEveryN);

        public void RecordCapture()
        {
            _totalCaptures++;
            if (_totalCaptures % _pulseEveryN == 0)
            {
                _pulseCount++;
                _totalBonusAwarded += _bonusPerPulse;
                _onPrismaticPulse?.Raise();
            }
        }

        public void Reset()
        {
            _totalCaptures     = 0;
            _pulseCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

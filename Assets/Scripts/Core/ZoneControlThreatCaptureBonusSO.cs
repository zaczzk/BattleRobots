using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlThreatCaptureBonus", order = 160)]
    public sealed class ZoneControlThreatCaptureBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerThreatCapture = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onThreatCaptureBonus;

        private int _threatCaptureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int ThreatCaptureCount => _threatCaptureCount;
        public int TotalBonusAwarded  => _totalBonusAwarded;
        public int BonusPerThreatCapture => _bonusPerThreatCapture;

        public void RecordThreatCapture()
        {
            _threatCaptureCount++;
            _totalBonusAwarded += _bonusPerThreatCapture;
            _onThreatCaptureBonus?.Raise();
        }

        public void Reset()
        {
            _threatCaptureCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

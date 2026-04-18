using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneFlood", order = 136)]
    public sealed class ZoneControlZoneFloodSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _totalZones    = 4;
        [SerializeField, Min(0)] private int _bonusPerFlood = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFloodDetected;

        private int  _floodCount;
        private int  _totalBonusAwarded;
        private bool _isFlooded;

        private void OnEnable() => Reset();

        public int  TotalZones        => _totalZones;
        public int  BonusPerFlood     => _bonusPerFlood;
        public int  FloodCount        => _floodCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public bool IsFlooded         => _isFlooded;

        public void RecordCapture(int playerOwnedCount)
        {
            if (playerOwnedCount >= _totalZones && !_isFlooded)
            {
                _isFlooded          = true;
                _floodCount++;
                _totalBonusAwarded += _bonusPerFlood;
                _onFloodDetected?.Raise();
            }
        }

        public void RecordLoss(int playerOwnedCount)
        {
            if (playerOwnedCount < _totalZones)
                _isFlooded = false;
        }

        public void Reset()
        {
            _floodCount        = 0;
            _totalBonusAwarded = 0;
            _isFlooded         = false;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlTurboCapture", order = 141)]
    public sealed class ZoneControlTurboCaptureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _turboInterval   = 5;
        [SerializeField, Min(0)] private int _bonusPerTurbo   = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTurbo;

        private int _totalCaptures;
        private int _turboCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int TurboInterval      => _turboInterval;
        public int BonusPerTurbo      => _bonusPerTurbo;
        public int TotalCaptures      => _totalCaptures;
        public int TurboCount         => _turboCount;
        public int TotalBonusAwarded  => _totalBonusAwarded;
        public int NextTurboIn        => _turboInterval - (_totalCaptures % _turboInterval);

        public void RecordCapture()
        {
            _totalCaptures++;
            if (_totalCaptures % _turboInterval == 0)
            {
                _turboCount++;
                _totalBonusAwarded += _bonusPerTurbo;
                _onTurbo?.Raise();
            }
        }

        public void Reset()
        {
            _totalCaptures     = 0;
            _turboCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

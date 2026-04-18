using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlLastCaptorBonus", order = 142)]
    public sealed class ZoneControlLastCaptorBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusAmount = 250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBonusAwarded;

        private bool _hasAnyCapture;
        private bool _playerWasLast;
        private int  _lastBonus;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  BonusAmount       => _bonusAmount;
        public bool HasAnyCapture     => _hasAnyCapture;
        public bool PlayerWasLast     => _playerWasLast;
        public int  LastBonus         => _lastBonus;
        public int  TotalBonusAwarded => _totalBonusAwarded;

        public void RecordPlayerCapture()
        {
            _hasAnyCapture = true;
            _playerWasLast = true;
        }

        public void RecordBotCapture()
        {
            _hasAnyCapture = true;
            _playerWasLast = false;
        }

        public int ApplyMatchEndBonus()
        {
            if (!_hasAnyCapture || !_playerWasLast)
            {
                _lastBonus = 0;
                return 0;
            }

            _lastBonus         = _bonusAmount;
            _totalBonusAwarded += _lastBonus;
            _onBonusAwarded?.Raise();
            return _lastBonus;
        }

        public void Reset()
        {
            _hasAnyCapture     = false;
            _playerWasLast     = false;
            _lastBonus         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureThrottle", order = 300)]
    public sealed class ZoneControlCaptureThrottleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _positionsNeeded = 6;
        [SerializeField, Min(1)] private int _slipPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerOpen    = 1240;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onThrottleOpened;

        private int _positions;
        private int _openCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PositionsNeeded   => _positionsNeeded;
        public int   SlipPerBot        => _slipPerBot;
        public int   BonusPerOpen      => _bonusPerOpen;
        public int   Positions         => _positions;
        public int   OpenCount         => _openCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PositionProgress  => _positionsNeeded > 0
            ? Mathf.Clamp01(_positions / (float)_positionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _positions = Mathf.Min(_positions + 1, _positionsNeeded);
            if (_positions >= _positionsNeeded)
            {
                int bonus = _bonusPerOpen;
                _openCount++;
                _totalBonusAwarded += bonus;
                _positions          = 0;
                _onThrottleOpened?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _positions = Mathf.Max(0, _positions - _slipPerBot);
        }

        public void Reset()
        {
            _positions         = 0;
            _openCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

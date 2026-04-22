using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureClock", order = 337)]
    public sealed class ZoneControlCaptureClockSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ticksNeeded      = 7;
        [SerializeField, Min(1)] private int _jitterPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerClock    = 1795;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onClockRun;

        private int _ticks;
        private int _clockCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TicksNeeded       => _ticksNeeded;
        public int   JitterPerBot      => _jitterPerBot;
        public int   BonusPerClock     => _bonusPerClock;
        public int   Ticks             => _ticks;
        public int   ClockCount        => _clockCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TickProgress      => _ticksNeeded > 0
            ? Mathf.Clamp01(_ticks / (float)_ticksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ticks = Mathf.Min(_ticks + 1, _ticksNeeded);
            if (_ticks >= _ticksNeeded)
            {
                int bonus = _bonusPerClock;
                _clockCount++;
                _totalBonusAwarded += bonus;
                _ticks              = 0;
                _onClockRun?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ticks = Mathf.Max(0, _ticks - _jitterPerBot);
        }

        public void Reset()
        {
            _ticks             = 0;
            _clockCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

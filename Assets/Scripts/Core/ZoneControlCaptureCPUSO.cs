using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCPU", order = 335)]
    public sealed class ZoneControlCaptureCPUSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cyclesNeeded    = 6;
        [SerializeField, Min(1)] private int _stallPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerCycle   = 1765;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCPUCycled;

        private int _cycles;
        private int _cycleCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CyclesNeeded      => _cyclesNeeded;
        public int   StallPerBot       => _stallPerBot;
        public int   BonusPerCycle     => _bonusPerCycle;
        public int   Cycles            => _cycles;
        public int   CycleCount        => _cycleCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CycleProgress     => _cyclesNeeded > 0
            ? Mathf.Clamp01(_cycles / (float)_cyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cycles = Mathf.Min(_cycles + 1, _cyclesNeeded);
            if (_cycles >= _cyclesNeeded)
            {
                int bonus = _bonusPerCycle;
                _cycleCount++;
                _totalBonusAwarded += bonus;
                _cycles             = 0;
                _onCPUCycled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cycles = Mathf.Max(0, _cycles - _stallPerBot);
        }

        public void Reset()
        {
            _cycles            = 0;
            _cycleCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

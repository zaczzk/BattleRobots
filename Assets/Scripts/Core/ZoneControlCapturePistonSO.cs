using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePiston", order = 297)]
    public sealed class ZoneControlCapturePistonSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _strokesNeeded  = 5;
        [SerializeField, Min(1)] private int _missPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerCycle  = 1195;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPistonCycled;

        private int _strokes;
        private int _cycleCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StrokesNeeded     => _strokesNeeded;
        public int   MissPerBot        => _missPerBot;
        public int   BonusPerCycle     => _bonusPerCycle;
        public int   Strokes           => _strokes;
        public int   CycleCount        => _cycleCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StrokeProgress    => _strokesNeeded > 0
            ? Mathf.Clamp01(_strokes / (float)_strokesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _strokes = Mathf.Min(_strokes + 1, _strokesNeeded);
            if (_strokes >= _strokesNeeded)
            {
                int bonus = _bonusPerCycle;
                _cycleCount++;
                _totalBonusAwarded += bonus;
                _strokes            = 0;
                _onPistonCycled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _strokes = Mathf.Max(0, _strokes - _missPerBot);
        }

        public void Reset()
        {
            _strokes           = 0;
            _cycleCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

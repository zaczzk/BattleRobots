using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMemory", order = 339)]
    public sealed class ZoneControlCaptureMemorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cellsNeeded    = 5;
        [SerializeField, Min(1)] private int _corruptPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerFlush  = 1825;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMemoryFlushed;

        private int _cells;
        private int _flushCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CellsNeeded       => _cellsNeeded;
        public int   CorruptPerBot     => _corruptPerBot;
        public int   BonusPerFlush     => _bonusPerFlush;
        public int   Cells             => _cells;
        public int   FlushCount        => _flushCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CellProgress      => _cellsNeeded > 0
            ? Mathf.Clamp01(_cells / (float)_cellsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cells = Mathf.Min(_cells + 1, _cellsNeeded);
            if (_cells >= _cellsNeeded)
            {
                int bonus = _bonusPerFlush;
                _flushCount++;
                _totalBonusAwarded += bonus;
                _cells              = 0;
                _onMemoryFlushed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cells = Mathf.Max(0, _cells - _corruptPerBot);
        }

        public void Reset()
        {
            _cells             = 0;
            _flushCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

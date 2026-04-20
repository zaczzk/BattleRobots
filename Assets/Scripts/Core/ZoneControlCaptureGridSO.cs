using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGrid", order = 206)]
    public sealed class ZoneControlCaptureGridSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _columns         = 3;
        [SerializeField, Min(1)] private int _rows            = 3;
        [SerializeField, Min(0)] private int _bonusPerRow     = 175;
        [SerializeField, Min(0)] private int _completionBonus = 600;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRowComplete;
        [SerializeField] private VoidGameEvent _onGridComplete;

        private int  _filledSlots;
        private int  _rowsCompleted;
        private int  _totalBonusAwarded;
        private bool _isComplete;

        private void OnEnable() => Reset();

        public int   Columns           => _columns;
        public int   Rows              => _rows;
        public int   BonusPerRow       => _bonusPerRow;
        public int   CompletionBonus   => _completionBonus;
        public int   TotalSlots        => _columns * _rows;
        public int   FilledSlots       => _filledSlots;
        public int   RowsCompleted     => _rowsCompleted;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public bool  IsComplete        => _isComplete;
        public float GridProgress      => TotalSlots > 0
            ? Mathf.Clamp01(_filledSlots / (float)TotalSlots)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_isComplete) return 0;

            _filledSlots++;
            int rowsNow  = _filledSlots / _columns;
            int rowBonus = 0;
            if (rowsNow > _rowsCompleted)
            {
                _rowsCompleted      = rowsNow;
                rowBonus            = _bonusPerRow;
                _totalBonusAwarded += rowBonus;
                _onRowComplete?.Raise();
            }

            if (_filledSlots >= TotalSlots)
            {
                _isComplete         = true;
                _totalBonusAwarded += _completionBonus;
                _onGridComplete?.Raise();
                return rowBonus + _completionBonus;
            }

            return rowBonus;
        }

        public void RecordBotCapture()
        {
            if (_isComplete || _filledSlots == 0) return;
            _filledSlots--;
            _rowsCompleted = _filledSlots / _columns;
        }

        public void Reset()
        {
            _filledSlots       = 0;
            _rowsCompleted     = 0;
            _totalBonusAwarded = 0;
            _isComplete        = false;
        }
    }
}

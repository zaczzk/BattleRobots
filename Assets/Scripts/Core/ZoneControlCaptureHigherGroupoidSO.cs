using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHigherGroupoid", order = 460)]
    public sealed class ZoneControlCaptureHigherGroupoidSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cellsNeeded   = 5;
        [SerializeField, Min(1)] private int _breakPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerInvert = 3640;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHigherGroupoidInverted;

        private int _cells;
        private int _invertCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CellsNeeded       => _cellsNeeded;
        public int   BreakPerBot       => _breakPerBot;
        public int   BonusPerInvert    => _bonusPerInvert;
        public int   Cells             => _cells;
        public int   InvertCount       => _invertCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CellProgress      => _cellsNeeded > 0
            ? Mathf.Clamp01(_cells / (float)_cellsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cells = Mathf.Min(_cells + 1, _cellsNeeded);
            if (_cells >= _cellsNeeded)
            {
                int bonus = _bonusPerInvert;
                _invertCount++;
                _totalBonusAwarded += bonus;
                _cells              = 0;
                _onHigherGroupoidInverted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cells = Mathf.Max(0, _cells - _breakPerBot);
        }

        public void Reset()
        {
            _cells             = 0;
            _invertCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

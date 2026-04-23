using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRig", order = 430)]
    public sealed class ZoneControlCaptureRigSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _rigCellsNeeded   = 8;
        [SerializeField, Min(1)] private int _absorbPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerDistribute = 3190;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDistributed;

        private int _rigCells;
        private int _distributeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RigCellsNeeded      => _rigCellsNeeded;
        public int   AbsorbPerBot        => _absorbPerBot;
        public int   BonusPerDistribute  => _bonusPerDistribute;
        public int   RigCells            => _rigCells;
        public int   DistributeCount     => _distributeCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float RigCellProgress     => _rigCellsNeeded > 0
            ? Mathf.Clamp01(_rigCells / (float)_rigCellsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rigCells = Mathf.Min(_rigCells + 1, _rigCellsNeeded);
            if (_rigCells >= _rigCellsNeeded)
            {
                int bonus = _bonusPerDistribute;
                _distributeCount++;
                _totalBonusAwarded += bonus;
                _rigCells           = 0;
                _onDistributed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _rigCells = Mathf.Max(0, _rigCells - _absorbPerBot);
        }

        public void Reset()
        {
            _rigCells          = 0;
            _distributeCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTableauxMethod", order = 550)]
    public sealed class ZoneControlCaptureTableauxMethodSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _closedBranchesNeeded = 6;
        [SerializeField, Min(1)] private int _openBranchesPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerClosure      = 4990;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTableauxMethodCompleted;

        private int _closedBranches;
        private int _closureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ClosedBranchesNeeded => _closedBranchesNeeded;
        public int   OpenBranchesPerBot   => _openBranchesPerBot;
        public int   BonusPerClosure      => _bonusPerClosure;
        public int   ClosedBranches       => _closedBranches;
        public int   ClosureCount         => _closureCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float ClosedBranchProgress => _closedBranchesNeeded > 0
            ? Mathf.Clamp01(_closedBranches / (float)_closedBranchesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _closedBranches = Mathf.Min(_closedBranches + 1, _closedBranchesNeeded);
            if (_closedBranches >= _closedBranchesNeeded)
            {
                int bonus = _bonusPerClosure;
                _closureCount++;
                _totalBonusAwarded += bonus;
                _closedBranches     = 0;
                _onTableauxMethodCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _closedBranches = Mathf.Max(0, _closedBranches - _openBranchesPerBot);
        }

        public void Reset()
        {
            _closedBranches    = 0;
            _closureCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePvsNP", order = 522)]
    public sealed class ZoneControlCapturePvsNPSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _reductionsNeeded   = 6;
        [SerializeField, Min(1)] private int _oracleBarriersPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerReduction   = 4570;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPvsNPReduced;

        private int _reductions;
        private int _reductionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ReductionsNeeded    => _reductionsNeeded;
        public int   OracleBarriersPerBot => _oracleBarriersPerBot;
        public int   BonusPerReduction   => _bonusPerReduction;
        public int   Reductions          => _reductions;
        public int   ReductionCount      => _reductionCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ReductionProgress   => _reductionsNeeded > 0
            ? Mathf.Clamp01(_reductions / (float)_reductionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _reductions = Mathf.Min(_reductions + 1, _reductionsNeeded);
            if (_reductions >= _reductionsNeeded)
            {
                int bonus = _bonusPerReduction;
                _reductionCount++;
                _totalBonusAwarded += bonus;
                _reductions         = 0;
                _onPvsNPReduced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _reductions = Mathf.Max(0, _reductions - _oracleBarriersPerBot);
        }

        public void Reset()
        {
            _reductions        = 0;
            _reductionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

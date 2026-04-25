using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNashEquilibrium", order = 523)]
    public sealed class ZoneControlCaptureNashEquilibriumSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _strategyPairsNeeded = 6;
        [SerializeField, Min(1)] private int _defectionsPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerEquilibrium = 4585;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNashEquilibriumReached;

        private int _strategyPairs;
        private int _equilibriumCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StrategyPairsNeeded  => _strategyPairsNeeded;
        public int   DefectionsPerBot     => _defectionsPerBot;
        public int   BonusPerEquilibrium  => _bonusPerEquilibrium;
        public int   StrategyPairs        => _strategyPairs;
        public int   EquilibriumCount     => _equilibriumCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float StrategyPairProgress => _strategyPairsNeeded > 0
            ? Mathf.Clamp01(_strategyPairs / (float)_strategyPairsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _strategyPairs = Mathf.Min(_strategyPairs + 1, _strategyPairsNeeded);
            if (_strategyPairs >= _strategyPairsNeeded)
            {
                int bonus = _bonusPerEquilibrium;
                _equilibriumCount++;
                _totalBonusAwarded += bonus;
                _strategyPairs      = 0;
                _onNashEquilibriumReached?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _strategyPairs = Mathf.Max(0, _strategyPairs - _defectionsPerBot);
        }

        public void Reset()
        {
            _strategyPairs     = 0;
            _equilibriumCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

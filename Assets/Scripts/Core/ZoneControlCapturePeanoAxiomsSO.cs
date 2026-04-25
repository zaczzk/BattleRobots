using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePeanoAxioms", order = 540)]
    public sealed class ZoneControlCapturePeanoAxiomsSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _successorConstructionsNeeded = 6;
        [SerializeField, Min(1)] private int _nonStandardModelsPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerAxiomSet             = 4840;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPeanoAxiomsConstructed;

        private int _successorConstructions;
        private int _axiomSetCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SuccessorConstructionsNeeded => _successorConstructionsNeeded;
        public int   NonStandardModelsPerBot      => _nonStandardModelsPerBot;
        public int   BonusPerAxiomSet             => _bonusPerAxiomSet;
        public int   SuccessorConstructions        => _successorConstructions;
        public int   AxiomSetCount                 => _axiomSetCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float SuccessorConstructionProgress => _successorConstructionsNeeded > 0
            ? Mathf.Clamp01(_successorConstructions / (float)_successorConstructionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _successorConstructions = Mathf.Min(_successorConstructions + 1, _successorConstructionsNeeded);
            if (_successorConstructions >= _successorConstructionsNeeded)
            {
                int bonus = _bonusPerAxiomSet;
                _axiomSetCount++;
                _totalBonusAwarded      += bonus;
                _successorConstructions  = 0;
                _onPeanoAxiomsConstructed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _successorConstructions = Mathf.Max(0, _successorConstructions - _nonStandardModelsPerBot);
        }

        public void Reset()
        {
            _successorConstructions = 0;
            _axiomSetCount          = 0;
            _totalBonusAwarded      = 0;
        }
    }
}

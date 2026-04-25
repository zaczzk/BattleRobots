using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHaltingProblem", order = 527)]
    public sealed class ZoneControlCaptureHaltingProblemSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _computationStepsNeeded = 6;
        [SerializeField, Min(1)] private int _infiniteLoopsPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerHalt           = 4645;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHaltingProblemDecided;

        private int _computationSteps;
        private int _decisionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComputationStepsNeeded => _computationStepsNeeded;
        public int   InfiniteLoopsPerBot    => _infiniteLoopsPerBot;
        public int   BonusPerHalt           => _bonusPerHalt;
        public int   ComputationSteps       => _computationSteps;
        public int   DecisionCount          => _decisionCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float ComputationStepProgress => _computationStepsNeeded > 0
            ? Mathf.Clamp01(_computationSteps / (float)_computationStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _computationSteps = Mathf.Min(_computationSteps + 1, _computationStepsNeeded);
            if (_computationSteps >= _computationStepsNeeded)
            {
                int bonus = _bonusPerHalt;
                _decisionCount++;
                _totalBonusAwarded  += bonus;
                _computationSteps    = 0;
                _onHaltingProblemDecided?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _computationSteps = Mathf.Max(0, _computationSteps - _infiniteLoopsPerBot);
        }

        public void Reset()
        {
            _computationSteps  = 0;
            _decisionCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

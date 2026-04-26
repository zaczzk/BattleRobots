using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBisimulation", order = 564)]
    public sealed class ZoneControlCaptureBisimulationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bisimulationStepsNeeded         = 6;
        [SerializeField, Min(1)] private int _observationalDivergencesPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerBisimulation            = 5200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBisimulationCompleted;

        private int _bisimulationSteps;
        private int _bisimulationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BisimulationStepsNeeded        => _bisimulationStepsNeeded;
        public int   ObservationalDivergencesPerBot => _observationalDivergencesPerBot;
        public int   BonusPerBisimulation           => _bonusPerBisimulation;
        public int   BisimulationSteps              => _bisimulationSteps;
        public int   BisimulationCount              => _bisimulationCount;
        public int   TotalBonusAwarded              => _totalBonusAwarded;
        public float BisimulationStepProgress => _bisimulationStepsNeeded > 0
            ? Mathf.Clamp01(_bisimulationSteps / (float)_bisimulationStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bisimulationSteps = Mathf.Min(_bisimulationSteps + 1, _bisimulationStepsNeeded);
            if (_bisimulationSteps >= _bisimulationStepsNeeded)
            {
                int bonus = _bonusPerBisimulation;
                _bisimulationCount++;
                _totalBonusAwarded  += bonus;
                _bisimulationSteps   = 0;
                _onBisimulationCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bisimulationSteps = Mathf.Max(0, _bisimulationSteps - _observationalDivergencesPerBot);
        }

        public void Reset()
        {
            _bisimulationSteps = 0;
            _bisimulationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

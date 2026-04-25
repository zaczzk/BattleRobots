using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTuringCompleteness", order = 528)]
    public sealed class ZoneControlCaptureTuringCompletenessSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _simulationStepsNeeded     = 5;
        [SerializeField, Min(1)] private int _memoryConstraintsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerSimulation        = 4660;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTuringCompletenessSimulated;

        private int _simulationSteps;
        private int _simulationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SimulationStepsNeeded   => _simulationStepsNeeded;
        public int   MemoryConstraintsPerBot => _memoryConstraintsPerBot;
        public int   BonusPerSimulation      => _bonusPerSimulation;
        public int   SimulationSteps         => _simulationSteps;
        public int   SimulationCount         => _simulationCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float SimulationStepProgress  => _simulationStepsNeeded > 0
            ? Mathf.Clamp01(_simulationSteps / (float)_simulationStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _simulationSteps = Mathf.Min(_simulationSteps + 1, _simulationStepsNeeded);
            if (_simulationSteps >= _simulationStepsNeeded)
            {
                int bonus = _bonusPerSimulation;
                _simulationCount++;
                _totalBonusAwarded += bonus;
                _simulationSteps    = 0;
                _onTuringCompletenessSimulated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _simulationSteps = Mathf.Max(0, _simulationSteps - _memoryConstraintsPerBot);
        }

        public void Reset()
        {
            _simulationSteps   = 0;
            _simulationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

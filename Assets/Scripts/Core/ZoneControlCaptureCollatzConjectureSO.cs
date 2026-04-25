using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCollatzConjecture", order = 533)]
    public sealed class ZoneControlCaptureCollatzConjectureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _convergenceStepsNeeded  = 7;
        [SerializeField, Min(1)] private int _divergentSequencesPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerConvergence      = 4735;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCollatzConjectureConverged;

        private int _convergenceSteps;
        private int _convergenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ConvergenceStepsNeeded  => _convergenceStepsNeeded;
        public int   DivergentSequencesPerBot => _divergentSequencesPerBot;
        public int   BonusPerConvergence      => _bonusPerConvergence;
        public int   ConvergenceSteps         => _convergenceSteps;
        public int   ConvergenceCount         => _convergenceCount;
        public int   TotalBonusAwarded        => _totalBonusAwarded;
        public float ConvergenceStepProgress  => _convergenceStepsNeeded > 0
            ? Mathf.Clamp01(_convergenceSteps / (float)_convergenceStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _convergenceSteps = Mathf.Min(_convergenceSteps + 1, _convergenceStepsNeeded);
            if (_convergenceSteps >= _convergenceStepsNeeded)
            {
                int bonus = _bonusPerConvergence;
                _convergenceCount++;
                _totalBonusAwarded += bonus;
                _convergenceSteps   = 0;
                _onCollatzConjectureConverged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _convergenceSteps = Mathf.Max(0, _convergenceSteps - _divergentSequencesPerBot);
        }

        public void Reset()
        {
            _convergenceSteps  = 0;
            _convergenceCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

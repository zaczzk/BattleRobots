using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCorecursion", order = 560)]
    public sealed class ZoneControlCaptureCorecursionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _corecursiveStepsNeeded      = 6;
        [SerializeField, Min(1)] private int _nonProductiveDivergencesPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerCorecursiveStep     = 5140;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCorecursionCompleted;

        private int _corecursiveSteps;
        private int _corecursionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CorecursiveStepsNeeded         => _corecursiveStepsNeeded;
        public int   NonProductiveDivergencesPerBot => _nonProductiveDivergencesPerBot;
        public int   BonusPerCorecursiveStep        => _bonusPerCorecursiveStep;
        public int   CorecursiveSteps               => _corecursiveSteps;
        public int   CorecursionCount               => _corecursionCount;
        public int   TotalBonusAwarded              => _totalBonusAwarded;
        public float CorecursiveStepProgress => _corecursiveStepsNeeded > 0
            ? Mathf.Clamp01(_corecursiveSteps / (float)_corecursiveStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _corecursiveSteps = Mathf.Min(_corecursiveSteps + 1, _corecursiveStepsNeeded);
            if (_corecursiveSteps >= _corecursiveStepsNeeded)
            {
                int bonus = _bonusPerCorecursiveStep;
                _corecursionCount++;
                _totalBonusAwarded += bonus;
                _corecursiveSteps   = 0;
                _onCorecursionCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _corecursiveSteps = Mathf.Max(0, _corecursiveSteps - _nonProductiveDivergencesPerBot);
        }

        public void Reset()
        {
            _corecursiveSteps = 0;
            _corecursionCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

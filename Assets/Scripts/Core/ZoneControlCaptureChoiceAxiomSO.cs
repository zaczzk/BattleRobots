using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChoiceAxiom", order = 538)]
    public sealed class ZoneControlCaptureChoiceAxiomSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _wellOrderingsNeeded          = 6;
        [SerializeField, Min(1)] private int _undecidableSelectionsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerChoiceFunction       = 4810;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChoiceAxiomApplied;

        private int _wellOrderings;
        private int _choiceFunctionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   WellOrderingsNeeded         => _wellOrderingsNeeded;
        public int   UndecidableSelectionsPerBot => _undecidableSelectionsPerBot;
        public int   BonusPerChoiceFunction      => _bonusPerChoiceFunction;
        public int   WellOrderings               => _wellOrderings;
        public int   ChoiceFunctionCount         => _choiceFunctionCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float WellOrderingProgress        => _wellOrderingsNeeded > 0
            ? Mathf.Clamp01(_wellOrderings / (float)_wellOrderingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _wellOrderings = Mathf.Min(_wellOrderings + 1, _wellOrderingsNeeded);
            if (_wellOrderings >= _wellOrderingsNeeded)
            {
                int bonus = _bonusPerChoiceFunction;
                _choiceFunctionCount++;
                _totalBonusAwarded += bonus;
                _wellOrderings      = 0;
                _onChoiceAxiomApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _wellOrderings = Mathf.Max(0, _wellOrderings - _undecidableSelectionsPerBot);
        }

        public void Reset()
        {
            _wellOrderings       = 0;
            _choiceFunctionCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

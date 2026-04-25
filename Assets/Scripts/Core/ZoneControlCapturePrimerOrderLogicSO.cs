using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePrimerOrderLogic", order = 545)]
    public sealed class ZoneControlCapturePrimerOrderLogicSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _validFormulasNeeded    = 6;
        [SerializeField, Min(1)] private int _countermodelsPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerCompleteness   = 4915;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFirstOrderCompletenessAchieved;

        private int _validFormulas;
        private int _completenessCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ValidFormulasNeeded  => _validFormulasNeeded;
        public int   CountermodelsPerBot  => _countermodelsPerBot;
        public int   BonusPerCompleteness => _bonusPerCompleteness;
        public int   ValidFormulas        => _validFormulas;
        public int   CompletenessCount    => _completenessCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float ValidFormulaProgress => _validFormulasNeeded > 0
            ? Mathf.Clamp01(_validFormulas / (float)_validFormulasNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _validFormulas = Mathf.Min(_validFormulas + 1, _validFormulasNeeded);
            if (_validFormulas >= _validFormulasNeeded)
            {
                int bonus = _bonusPerCompleteness;
                _completenessCount++;
                _totalBonusAwarded += bonus;
                _validFormulas      = 0;
                _onFirstOrderCompletenessAchieved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _validFormulas = Mathf.Max(0, _validFormulas - _countermodelsPerBot);
        }

        public void Reset()
        {
            _validFormulas     = 0;
            _completenessCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

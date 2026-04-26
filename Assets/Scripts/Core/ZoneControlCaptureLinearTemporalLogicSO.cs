using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLinearTemporalLogic", order = 563)]
    public sealed class ZoneControlCaptureLinearTemporalLogicSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _temporalFormulasNeeded    = 6;
        [SerializeField, Min(1)] private int _pathFalsificationsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerFormula           = 5185;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLinearTemporalLogicCompleted;

        private int _temporalFormulas;
        private int _formulaCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TemporalFormulasNeeded    => _temporalFormulasNeeded;
        public int   PathFalsificationsPerBot  => _pathFalsificationsPerBot;
        public int   BonusPerFormula           => _bonusPerFormula;
        public int   TemporalFormulas          => _temporalFormulas;
        public int   FormulaCount              => _formulaCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float TemporalFormulaProgress => _temporalFormulasNeeded > 0
            ? Mathf.Clamp01(_temporalFormulas / (float)_temporalFormulasNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _temporalFormulas = Mathf.Min(_temporalFormulas + 1, _temporalFormulasNeeded);
            if (_temporalFormulas >= _temporalFormulasNeeded)
            {
                int bonus = _bonusPerFormula;
                _formulaCount++;
                _totalBonusAwarded  += bonus;
                _temporalFormulas    = 0;
                _onLinearTemporalLogicCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _temporalFormulas = Mathf.Max(0, _temporalFormulas - _pathFalsificationsPerBot);
        }

        public void Reset()
        {
            _temporalFormulas  = 0;
            _formulaCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

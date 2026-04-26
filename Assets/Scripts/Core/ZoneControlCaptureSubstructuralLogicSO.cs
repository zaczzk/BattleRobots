using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSubstructuralLogic", order = 556)]
    public sealed class ZoneControlCaptureSubstructuralLogicSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _structuralRulesNeeded  = 6;
        [SerializeField, Min(1)] private int _ruleViolationsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerRuleApplication = 5080;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSubstructuralLogicCompleted;

        private int _structuralRules;
        private int _ruleApplicationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StructuralRulesNeeded   => _structuralRulesNeeded;
        public int   RuleViolationsPerBot    => _ruleViolationsPerBot;
        public int   BonusPerRuleApplication => _bonusPerRuleApplication;
        public int   StructuralRules         => _structuralRules;
        public int   RuleApplicationCount    => _ruleApplicationCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float StructuralRuleProgress => _structuralRulesNeeded > 0
            ? Mathf.Clamp01(_structuralRules / (float)_structuralRulesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _structuralRules = Mathf.Min(_structuralRules + 1, _structuralRulesNeeded);
            if (_structuralRules >= _structuralRulesNeeded)
            {
                int bonus = _bonusPerRuleApplication;
                _ruleApplicationCount++;
                _totalBonusAwarded += bonus;
                _structuralRules    = 0;
                _onSubstructuralLogicCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _structuralRules = Mathf.Max(0, _structuralRules - _ruleViolationsPerBot);
        }

        public void Reset()
        {
            _structuralRules      = 0;
            _ruleApplicationCount = 0;
            _totalBonusAwarded    = 0;
        }
    }
}

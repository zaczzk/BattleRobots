using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDeRhamCohomology", order = 474)]
    public sealed class ZoneControlCaptureDeRhamCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _formsNeeded        = 7;
        [SerializeField, Min(1)] private int _exactPerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerIntegration = 3850;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDeRhamCohomologyIntegrated;

        private int _forms;
        private int _integrateCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FormsNeeded        => _formsNeeded;
        public int   ExactPerBot        => _exactPerBot;
        public int   BonusPerIntegration => _bonusPerIntegration;
        public int   Forms              => _forms;
        public int   IntegrateCount     => _integrateCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float FormProgress       => _formsNeeded > 0
            ? Mathf.Clamp01(_forms / (float)_formsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _forms = Mathf.Min(_forms + 1, _formsNeeded);
            if (_forms >= _formsNeeded)
            {
                int bonus = _bonusPerIntegration;
                _integrateCount++;
                _totalBonusAwarded += bonus;
                _forms              = 0;
                _onDeRhamCohomologyIntegrated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _forms = Mathf.Max(0, _forms - _exactPerBot);
        }

        public void Reset()
        {
            _forms             = 0;
            _integrateCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

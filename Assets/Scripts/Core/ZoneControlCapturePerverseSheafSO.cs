using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePerverseSheaf", order = 498)]
    public sealed class ZoneControlCapturePerverseSheafSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stalkConditionsNeeded   = 7;
        [SerializeField, Min(1)] private int _supportConditionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerPerversification = 4210;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPerverseSheafPerversified;

        private int _stalkConditions;
        private int _perversificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StalkConditionsNeeded   => _stalkConditionsNeeded;
        public int   SupportConditionsPerBot => _supportConditionsPerBot;
        public int   BonusPerPerversification => _bonusPerPerversification;
        public int   StalkConditions         => _stalkConditions;
        public int   PerversificationCount   => _perversificationCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float StalkConditionProgress => _stalkConditionsNeeded > 0
            ? Mathf.Clamp01(_stalkConditions / (float)_stalkConditionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stalkConditions = Mathf.Min(_stalkConditions + 1, _stalkConditionsNeeded);
            if (_stalkConditions >= _stalkConditionsNeeded)
            {
                int bonus = _bonusPerPerversification;
                _perversificationCount++;
                _totalBonusAwarded  += bonus;
                _stalkConditions     = 0;
                _onPerverseSheafPerversified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stalkConditions = Mathf.Max(0, _stalkConditions - _supportConditionsPerBot);
        }

        public void Reset()
        {
            _stalkConditions       = 0;
            _perversificationCount = 0;
            _totalBonusAwarded     = 0;
        }
    }
}

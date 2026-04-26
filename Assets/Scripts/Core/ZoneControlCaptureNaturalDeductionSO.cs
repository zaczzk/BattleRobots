using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNaturalDeduction", order = 551)]
    public sealed class ZoneControlCaptureNaturalDeductionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _dischargeStepsNeeded           = 6;
        [SerializeField, Min(1)] private int _undischargedAssumptionsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerDischarge              = 5005;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNaturalDeductionCompleted;

        private int _dischargeSteps;
        private int _dischargeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DischargeStepsNeeded          => _dischargeStepsNeeded;
        public int   UndischargedAssumptionsPerBot => _undischargedAssumptionsPerBot;
        public int   BonusPerDischarge             => _bonusPerDischarge;
        public int   DischargeSteps                => _dischargeSteps;
        public int   DischargeCount                => _dischargeCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float DischargeStepProgress => _dischargeStepsNeeded > 0
            ? Mathf.Clamp01(_dischargeSteps / (float)_dischargeStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _dischargeSteps = Mathf.Min(_dischargeSteps + 1, _dischargeStepsNeeded);
            if (_dischargeSteps >= _dischargeStepsNeeded)
            {
                int bonus = _bonusPerDischarge;
                _dischargeCount++;
                _totalBonusAwarded += bonus;
                _dischargeSteps     = 0;
                _onNaturalDeductionCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _dischargeSteps = Mathf.Max(0, _dischargeSteps - _undischargedAssumptionsPerBot);
        }

        public void Reset()
        {
            _dischargeSteps    = 0;
            _dischargeCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

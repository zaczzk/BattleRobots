using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLinearLogic", order = 558)]
    public sealed class ZoneControlCaptureLinearLogicSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _resourceConsumptionsNeeded    = 6;
        [SerializeField, Min(1)] private int _exponentialContractionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerConsumption           = 5110;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLinearLogicCompleted;

        private int _resourceConsumptions;
        private int _consumptionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ResourceConsumptionsNeeded    => _resourceConsumptionsNeeded;
        public int   ExponentialContractionsPerBot => _exponentialContractionsPerBot;
        public int   BonusPerConsumption           => _bonusPerConsumption;
        public int   ResourceConsumptions          => _resourceConsumptions;
        public int   ConsumptionCount              => _consumptionCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float ResourceConsumptionProgress => _resourceConsumptionsNeeded > 0
            ? Mathf.Clamp01(_resourceConsumptions / (float)_resourceConsumptionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _resourceConsumptions = Mathf.Min(_resourceConsumptions + 1, _resourceConsumptionsNeeded);
            if (_resourceConsumptions >= _resourceConsumptionsNeeded)
            {
                int bonus = _bonusPerConsumption;
                _consumptionCount++;
                _totalBonusAwarded    += bonus;
                _resourceConsumptions  = 0;
                _onLinearLogicCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _resourceConsumptions = Mathf.Max(0, _resourceConsumptions - _exponentialContractionsPerBot);
        }

        public void Reset()
        {
            _resourceConsumptions = 0;
            _consumptionCount     = 0;
            _totalBonusAwarded    = 0;
        }
    }
}

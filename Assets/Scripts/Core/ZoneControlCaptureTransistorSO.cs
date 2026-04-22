using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTransistor", order = 322)]
    public sealed class ZoneControlCaptureTransistorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _gatesNeeded    = 5;
        [SerializeField, Min(1)] private int _leakPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerSwitch = 1570;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTransistorSwitched;

        private int _gates;
        private int _switchCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GatesNeeded      => _gatesNeeded;
        public int   LeakPerBot       => _leakPerBot;
        public int   BonusPerSwitch   => _bonusPerSwitch;
        public int   Gates            => _gates;
        public int   SwitchCount      => _switchCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float GateProgress     => _gatesNeeded > 0
            ? Mathf.Clamp01(_gates / (float)_gatesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _gates = Mathf.Min(_gates + 1, _gatesNeeded);
            if (_gates >= _gatesNeeded)
            {
                int bonus = _bonusPerSwitch;
                _switchCount++;
                _totalBonusAwarded += bonus;
                _gates              = 0;
                _onTransistorSwitched?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _gates = Mathf.Max(0, _gates - _leakPerBot);
        }

        public void Reset()
        {
            _gates             = 0;
            _switchCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

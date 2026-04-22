using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBus", order = 338)]
    public sealed class ZoneControlCaptureBusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _signalsNeeded         = 5;
        [SerializeField, Min(1)] private int _dropPerBot            = 1;
        [SerializeField, Min(0)] private int _bonusPerTransmission  = 1810;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBusTransmitted;

        private int _signals;
        private int _transmissionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SignalsNeeded        => _signalsNeeded;
        public int   DropPerBot           => _dropPerBot;
        public int   BonusPerTransmission => _bonusPerTransmission;
        public int   Signals              => _signals;
        public int   TransmissionCount    => _transmissionCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float SignalProgress       => _signalsNeeded > 0
            ? Mathf.Clamp01(_signals / (float)_signalsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _signals = Mathf.Min(_signals + 1, _signalsNeeded);
            if (_signals >= _signalsNeeded)
            {
                int bonus = _bonusPerTransmission;
                _transmissionCount++;
                _totalBonusAwarded += bonus;
                _signals            = 0;
                _onBusTransmitted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _signals = Mathf.Max(0, _signals - _dropPerBot);
        }

        public void Reset()
        {
            _signals           = 0;
            _transmissionCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

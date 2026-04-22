using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureModulator", order = 331)]
    public sealed class ZoneControlCaptureModulatorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _signalsNeeded      = 6;
        [SerializeField, Min(1)] private int _decayPerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerModulation = 1705;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onModulatorModulated;

        private int _signals;
        private int _modulationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SignalsNeeded        => _signalsNeeded;
        public int   DecayPerBot          => _decayPerBot;
        public int   BonusPerModulation   => _bonusPerModulation;
        public int   Signals              => _signals;
        public int   ModulationCount      => _modulationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float SignalProgress       => _signalsNeeded > 0
            ? Mathf.Clamp01(_signals / (float)_signalsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _signals = Mathf.Min(_signals + 1, _signalsNeeded);
            if (_signals >= _signalsNeeded)
            {
                int bonus = _bonusPerModulation;
                _modulationCount++;
                _totalBonusAwarded += bonus;
                _signals            = 0;
                _onModulatorModulated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _signals = Mathf.Max(0, _signals - _decayPerBot);
        }

        public void Reset()
        {
            _signals           = 0;
            _modulationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

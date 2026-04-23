using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCoyoneda", order = 380)]
    public sealed class ZoneControlCaptureCoyonedaSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _samplesNeeded = 6;
        [SerializeField, Min(1)] private int _discardPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerLift  = 2440;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCoyonedaLifted;

        private int _samples;
        private int _liftCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SamplesNeeded     => _samplesNeeded;
        public int   DiscardPerBot     => _discardPerBot;
        public int   BonusPerLift      => _bonusPerLift;
        public int   Samples           => _samples;
        public int   LiftCount         => _liftCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SampleProgress    => _samplesNeeded > 0
            ? Mathf.Clamp01(_samples / (float)_samplesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _samples = Mathf.Min(_samples + 1, _samplesNeeded);
            if (_samples >= _samplesNeeded)
            {
                int bonus = _bonusPerLift;
                _liftCount++;
                _totalBonusAwarded += bonus;
                _samples            = 0;
                _onCoyonedaLifted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _samples = Mathf.Max(0, _samples - _discardPerBot);
        }

        public void Reset()
        {
            _samples           = 0;
            _liftCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

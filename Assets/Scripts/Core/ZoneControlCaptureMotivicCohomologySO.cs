using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMotivicCohomology", order = 471)]
    public sealed class ZoneControlCaptureMotivicCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cyclesNeeded       = 5;
        [SerializeField, Min(1)] private int _breakPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerMotivation = 3805;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMotivicCohomologyMotivated;

        private int _cycles;
        private int _motivateCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CyclesNeeded       => _cyclesNeeded;
        public int   BreakPerBot        => _breakPerBot;
        public int   BonusPerMotivation => _bonusPerMotivation;
        public int   Cycles             => _cycles;
        public int   MotivateCount      => _motivateCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float CycleProgress      => _cyclesNeeded > 0
            ? Mathf.Clamp01(_cycles / (float)_cyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cycles = Mathf.Min(_cycles + 1, _cyclesNeeded);
            if (_cycles >= _cyclesNeeded)
            {
                int bonus = _bonusPerMotivation;
                _motivateCount++;
                _totalBonusAwarded += bonus;
                _cycles             = 0;
                _onMotivicCohomologyMotivated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cycles = Mathf.Max(0, _cycles - _breakPerBot);
        }

        public void Reset()
        {
            _cycles            = 0;
            _motivateCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

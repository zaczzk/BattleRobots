using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCyclicCohomology", order = 479)]
    public sealed class ZoneControlCaptureCyclicCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cyclesNeeded   = 5;
        [SerializeField, Min(1)] private int _degeneracyPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerTrace  = 3925;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCyclicCohomologyTraced;

        private int _cycles;
        private int _traceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CyclesNeeded      => _cyclesNeeded;
        public int   DegeneracyPerBot  => _degeneracyPerBot;
        public int   BonusPerTrace     => _bonusPerTrace;
        public int   Cycles            => _cycles;
        public int   TraceCount        => _traceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CycleProgress     => _cyclesNeeded > 0
            ? Mathf.Clamp01(_cycles / (float)_cyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cycles = Mathf.Min(_cycles + 1, _cyclesNeeded);
            if (_cycles >= _cyclesNeeded)
            {
                int bonus = _bonusPerTrace;
                _traceCount++;
                _totalBonusAwarded += bonus;
                _cycles             = 0;
                _onCyclicCohomologyTraced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cycles = Mathf.Max(0, _cycles - _degeneracyPerBot);
        }

        public void Reset()
        {
            _cycles            = 0;
            _traceCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

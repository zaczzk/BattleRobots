using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTraced", order = 423)]
    public sealed class ZoneControlCaptureTracedSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _loopsNeeded   = 5;
        [SerializeField, Min(1)] private int _unwindPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerTrace = 3085;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTraced;

        private int _loops;
        private int _traceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LoopsNeeded       => _loopsNeeded;
        public int   UnwindPerBot      => _unwindPerBot;
        public int   BonusPerTrace     => _bonusPerTrace;
        public int   Loops             => _loops;
        public int   TraceCount        => _traceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LoopProgress      => _loopsNeeded > 0
            ? Mathf.Clamp01(_loops / (float)_loopsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _loops = Mathf.Min(_loops + 1, _loopsNeeded);
            if (_loops >= _loopsNeeded)
            {
                int bonus = _bonusPerTrace;
                _traceCount++;
                _totalBonusAwarded += bonus;
                _loops              = 0;
                _onTraced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _loops = Mathf.Max(0, _loops - _unwindPerBot);
        }

        public void Reset()
        {
            _loops             = 0;
            _traceCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

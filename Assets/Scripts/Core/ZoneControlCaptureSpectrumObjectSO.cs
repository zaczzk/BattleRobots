using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpectrumObject", order = 461)]
    public sealed class ZoneControlCaptureSpectrumObjectSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _loopsNeeded   = 7;
        [SerializeField, Min(1)] private int _breakPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerDeloop = 3655;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpectrumObjectDelooped;

        private int _loops;
        private int _deloopCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LoopsNeeded       => _loopsNeeded;
        public int   BreakPerBot       => _breakPerBot;
        public int   BonusPerDeloop    => _bonusPerDeloop;
        public int   Loops             => _loops;
        public int   DeloopCount       => _deloopCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LoopProgress      => _loopsNeeded > 0
            ? Mathf.Clamp01(_loops / (float)_loopsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _loops = Mathf.Min(_loops + 1, _loopsNeeded);
            if (_loops >= _loopsNeeded)
            {
                int bonus = _bonusPerDeloop;
                _deloopCount++;
                _totalBonusAwarded += bonus;
                _loops              = 0;
                _onSpectrumObjectDelooped?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _loops = Mathf.Max(0, _loops - _breakPerBot);
        }

        public void Reset()
        {
            _loops             = 0;
            _deloopCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDelooping", order = 457)]
    public sealed class ZoneControlCaptureDeloopingSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _loopsNeeded      = 5;
        [SerializeField, Min(1)] private int _trivializePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerDeloop   = 3595;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDeloopingComplete;

        private int _loops;
        private int _deloopCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LoopsNeeded       => _loopsNeeded;
        public int   TrivializePerBot  => _trivializePerBot;
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
                _onDeloopingComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _loops = Mathf.Max(0, _loops - _trivializePerBot);
        }

        public void Reset()
        {
            _loops             = 0;
            _deloopCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

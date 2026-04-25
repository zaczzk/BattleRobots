using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAbsoluteCohomology", order = 500)]
    public sealed class ZoneControlCaptureAbsoluteCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _absoluteCyclesNeeded         = 6;
        [SerializeField, Min(1)] private int _arithmeticObstructionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerRealization           = 4240;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAbsoluteCohomologyRealized;

        private int _absoluteCycles;
        private int _realizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   AbsoluteCyclesNeeded         => _absoluteCyclesNeeded;
        public int   ArithmeticObstructionsPerBot => _arithmeticObstructionsPerBot;
        public int   BonusPerRealization           => _bonusPerRealization;
        public int   AbsoluteCycles                => _absoluteCycles;
        public int   RealizationCount              => _realizationCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float AbsoluteCycleProgress => _absoluteCyclesNeeded > 0
            ? Mathf.Clamp01(_absoluteCycles / (float)_absoluteCyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _absoluteCycles = Mathf.Min(_absoluteCycles + 1, _absoluteCyclesNeeded);
            if (_absoluteCycles >= _absoluteCyclesNeeded)
            {
                int bonus = _bonusPerRealization;
                _realizationCount++;
                _totalBonusAwarded += bonus;
                _absoluteCycles     = 0;
                _onAbsoluteCohomologyRealized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _absoluteCycles = Mathf.Max(0, _absoluteCycles - _arithmeticObstructionsPerBot);
        }

        public void Reset()
        {
            _absoluteCycles    = 0;
            _realizationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

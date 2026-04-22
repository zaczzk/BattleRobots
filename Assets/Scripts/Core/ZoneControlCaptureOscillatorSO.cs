using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOscillator", order = 319)]
    public sealed class ZoneControlCaptureOscillatorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _oscillationsNeeded = 6;
        [SerializeField, Min(1)] private int _dampPerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerCycle      = 1525;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOscillatorCycled;

        private int _oscillations;
        private int _cycleCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OscillationsNeeded => _oscillationsNeeded;
        public int   DampPerBot         => _dampPerBot;
        public int   BonusPerCycle      => _bonusPerCycle;
        public int   Oscillations       => _oscillations;
        public int   CycleCount         => _cycleCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float OscillationProgress => _oscillationsNeeded > 0
            ? Mathf.Clamp01(_oscillations / (float)_oscillationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _oscillations = Mathf.Min(_oscillations + 1, _oscillationsNeeded);
            if (_oscillations >= _oscillationsNeeded)
            {
                int bonus = _bonusPerCycle;
                _cycleCount++;
                _totalBonusAwarded += bonus;
                _oscillations       = 0;
                _onOscillatorCycled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _oscillations = Mathf.Max(0, _oscillations - _dampPerBot);
        }

        public void Reset()
        {
            _oscillations      = 0;
            _cycleCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

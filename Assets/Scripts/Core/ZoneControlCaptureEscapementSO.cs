using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEscapement", order = 310)]
    public sealed class ZoneControlCaptureEscapementSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ticksNeeded   = 7;
        [SerializeField, Min(1)] private int _slipPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerRelease = 1390;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEscapementReleased;

        private int _ticks;
        private int _releaseCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TicksNeeded       => _ticksNeeded;
        public int   SlipPerBot        => _slipPerBot;
        public int   BonusPerRelease   => _bonusPerRelease;
        public int   Ticks             => _ticks;
        public int   ReleaseCount      => _releaseCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TickProgress      => _ticksNeeded > 0
            ? Mathf.Clamp01(_ticks / (float)_ticksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ticks = Mathf.Min(_ticks + 1, _ticksNeeded);
            if (_ticks >= _ticksNeeded)
            {
                int bonus = _bonusPerRelease;
                _releaseCount++;
                _totalBonusAwarded += bonus;
                _ticks              = 0;
                _onEscapementReleased?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ticks = Mathf.Max(0, _ticks - _slipPerBot);
        }

        public void Reset()
        {
            _ticks             = 0;
            _releaseCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

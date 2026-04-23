using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTerminal", order = 408)]
    public sealed class ZoneControlCaptureTerminalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _arrowsNeeded    = 5;
        [SerializeField, Min(1)] private int _rejectPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerTerminal = 2860;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTerminalReached;

        private int _arrows;
        private int _terminalCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ArrowsNeeded      => _arrowsNeeded;
        public int   RejectPerBot      => _rejectPerBot;
        public int   BonusPerTerminal  => _bonusPerTerminal;
        public int   Arrows            => _arrows;
        public int   TerminalCount     => _terminalCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ArrowProgress     => _arrowsNeeded > 0
            ? Mathf.Clamp01(_arrows / (float)_arrowsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _arrows = Mathf.Min(_arrows + 1, _arrowsNeeded);
            if (_arrows >= _arrowsNeeded)
            {
                int bonus = _bonusPerTerminal;
                _terminalCount++;
                _totalBonusAwarded += bonus;
                _arrows             = 0;
                _onTerminalReached?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _arrows = Mathf.Max(0, _arrows - _rejectPerBot);
        }

        public void Reset()
        {
            _arrows            = 0;
            _terminalCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCurry", order = 362)]
    public sealed class ZoneControlCaptureCurrySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _argsNeeded      = 5;
        [SerializeField, Min(1)] private int _removePerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerCurry   = 2170;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCurryComplete;

        private int _args;
        private int _curryCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ArgsNeeded        => _argsNeeded;
        public int   RemovePerBot      => _removePerBot;
        public int   BonusPerCurry     => _bonusPerCurry;
        public int   Args              => _args;
        public int   CurryCount        => _curryCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ArgProgress       => _argsNeeded > 0
            ? Mathf.Clamp01(_args / (float)_argsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _args = Mathf.Min(_args + 1, _argsNeeded);
            if (_args >= _argsNeeded)
            {
                int bonus = _bonusPerCurry;
                _curryCount++;
                _totalBonusAwarded += bonus;
                _args               = 0;
                _onCurryComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _args = Mathf.Max(0, _args - _removePerBot);
        }

        public void Reset()
        {
            _args              = 0;
            _curryCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

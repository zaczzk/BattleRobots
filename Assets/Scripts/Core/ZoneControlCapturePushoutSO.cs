using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePushout", order = 400)]
    public sealed class ZoneControlCapturePushoutSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _arrowsNeeded   = 6;
        [SerializeField, Min(1)] private int _retractPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerPushout = 2740;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPushoutPushed;

        private int _arrows;
        private int _pushoutCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ArrowsNeeded      => _arrowsNeeded;
        public int   RetractPerBot     => _retractPerBot;
        public int   BonusPerPushout   => _bonusPerPushout;
        public int   Arrows            => _arrows;
        public int   PushoutCount      => _pushoutCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ArrowProgress     => _arrowsNeeded > 0
            ? Mathf.Clamp01(_arrows / (float)_arrowsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _arrows = Mathf.Min(_arrows + 1, _arrowsNeeded);
            if (_arrows >= _arrowsNeeded)
            {
                int bonus = _bonusPerPushout;
                _pushoutCount++;
                _totalBonusAwarded += bonus;
                _arrows             = 0;
                _onPushoutPushed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _arrows = Mathf.Max(0, _arrows - _retractPerBot);
        }

        public void Reset()
        {
            _arrows            = 0;
            _pushoutCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

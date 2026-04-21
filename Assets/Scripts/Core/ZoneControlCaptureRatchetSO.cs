using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRatchet", order = 294)]
    public sealed class ZoneControlCaptureRatchetSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _clicksNeeded    = 7;
        [SerializeField, Min(1)] private int _slipPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerAdvance = 1150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRatchetAdvanced;

        private int _clicks;
        private int _advanceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ClicksNeeded      => _clicksNeeded;
        public int   SlipPerBot        => _slipPerBot;
        public int   BonusPerAdvance   => _bonusPerAdvance;
        public int   Clicks            => _clicks;
        public int   AdvanceCount      => _advanceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ClickProgress     => _clicksNeeded > 0
            ? Mathf.Clamp01(_clicks / (float)_clicksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _clicks = Mathf.Min(_clicks + 1, _clicksNeeded);
            if (_clicks >= _clicksNeeded)
            {
                int bonus = _bonusPerAdvance;
                _advanceCount++;
                _totalBonusAwarded += bonus;
                _clicks             = 0;
                _onRatchetAdvanced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _clicks = Mathf.Max(0, _clicks - _slipPerBot);
        }

        public void Reset()
        {
            _clicks            = 0;
            _advanceCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

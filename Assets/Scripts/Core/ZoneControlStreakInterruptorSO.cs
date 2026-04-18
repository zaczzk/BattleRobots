using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlStreakInterruptor", order = 139)]
    public sealed class ZoneControlStreakInterruptorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)]   private int _interruptThreshold = 2;
        [SerializeField, Min(0)]   private int _bonusPerInterrupt  = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInterrupt;

        private int _botStreak;
        private int _interruptCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int InterruptThreshold  => _interruptThreshold;
        public int BonusPerInterrupt   => _bonusPerInterrupt;
        public int BotStreak           => _botStreak;
        public int InterruptCount      => _interruptCount;
        public int TotalBonusAwarded   => _totalBonusAwarded;

        public void RecordBotCapture()
        {
            _botStreak++;
        }

        public void RecordPlayerCapture()
        {
            if (_botStreak >= _interruptThreshold)
            {
                _interruptCount++;
                _totalBonusAwarded += _bonusPerInterrupt;
                _onInterrupt?.Raise();
            }
            _botStreak = 0;
        }

        public void Reset()
        {
            _botStreak         = 0;
            _interruptCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

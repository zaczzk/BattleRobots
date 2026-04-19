using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureShadow", order = 175)]
    public sealed class ZoneControlCaptureShadowSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerClear = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onShadowCleared;

        private int _shadowDebt;
        private int _clearedCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int BonusPerClear       => _bonusPerClear;
        public int ShadowDebt          => _shadowDebt;
        public int ClearedCount        => _clearedCount;
        public int TotalBonusAwarded   => _totalBonusAwarded;

        public void RecordBotCapture()
        {
            _shadowDebt++;
        }

        public void RecordPlayerCapture()
        {
            if (_shadowDebt <= 0) return;
            _shadowDebt--;
            if (_shadowDebt == 0)
            {
                _clearedCount++;
                _totalBonusAwarded += _bonusPerClear;
                _onShadowCleared?.Raise();
            }
        }

        public void Reset()
        {
            _shadowDebt        = 0;
            _clearedCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

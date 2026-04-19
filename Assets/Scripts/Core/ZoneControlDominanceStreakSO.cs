using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlDominanceStreak", order = 152)]
    public sealed class ZoneControlDominanceStreakSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _consecutiveDominanceForBonus = 3;
        [SerializeField, Min(0)] private int _bonusPerConsecutive          = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceStreakBonus;

        private int _consecutiveDominanceHolds;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int ConsecutiveDominanceHolds    => _consecutiveDominanceHolds;
        public int TotalBonusAwarded            => _totalBonusAwarded;
        public int ConsecutiveDominanceForBonus => _consecutiveDominanceForBonus;
        public int BonusPerConsecutive          => _bonusPerConsecutive;

        public void RecordMatchEnd(bool hadDominance)
        {
            if (hadDominance)
            {
                _consecutiveDominanceHolds++;
                if (_consecutiveDominanceHolds >= _consecutiveDominanceForBonus)
                {
                    _totalBonusAwarded += _bonusPerConsecutive;
                    _onDominanceStreakBonus?.Raise();
                }
            }
            else
            {
                _consecutiveDominanceHolds = 0;
            }
        }

        public void Reset()
        {
            _consecutiveDominanceHolds = 0;
            _totalBonusAwarded         = 0;
        }
    }
}

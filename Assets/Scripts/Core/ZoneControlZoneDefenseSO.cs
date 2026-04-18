using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneDefense", order = 101)]
    public sealed class ZoneControlZoneDefenseSO : ScriptableObject
    {
        [Header("Defense Settings")]
        [Min(1)]
        [SerializeField] private int _consecutiveHoldsForBonus = 3;
        [Min(0)]
        [SerializeField] private int _bonusPerConsecutiveHold = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDefenseBonus;

        private int _consecutiveHolds;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int ConsecutiveHolds         => _consecutiveHolds;
        public int TotalBonusAwarded        => _totalBonusAwarded;
        public int ConsecutiveHoldsForBonus => _consecutiveHoldsForBonus;
        public int BonusPerConsecutiveHold  => _bonusPerConsecutiveHold;

        public void RecordMatchEnd(bool majorityHeld)
        {
            if (majorityHeld)
            {
                _consecutiveHolds++;
                if (_consecutiveHolds >= _consecutiveHoldsForBonus)
                {
                    _totalBonusAwarded += _bonusPerConsecutiveHold;
                    _onDefenseBonus?.Raise();
                }
            }
            else
            {
                _consecutiveHolds = 0;
            }
        }

        public void Reset()
        {
            _consecutiveHolds  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

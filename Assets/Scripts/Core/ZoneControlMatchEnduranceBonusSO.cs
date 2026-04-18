using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that awards an end-of-match tiered bonus based on the total
    /// number of zones the player captured during the match.  Three escalating
    /// tiers are evaluated; <see cref="ApplyEndBonus"/> returns the total bonus
    /// earned and fires <c>_onBonusApplied</c> when any bonus is awarded.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchEnduranceBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchEnduranceBonus", order = 105)]
    public sealed class ZoneControlMatchEnduranceBonusSO : ScriptableObject
    {
        [Header("Tier Capture Targets")]
        [Min(1)]
        [SerializeField] private int _tier1Target = 5;
        [Min(1)]
        [SerializeField] private int _tier2Target = 10;
        [Min(1)]
        [SerializeField] private int _tier3Target = 20;

        [Header("Tier Bonuses")]
        [Min(0)]
        [SerializeField] private int _tier1Bonus = 100;
        [Min(0)]
        [SerializeField] private int _tier2Bonus = 250;
        [Min(0)]
        [SerializeField] private int _tier3Bonus = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBonusApplied;

        private int _captureCount;
        private int _tiersReached;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        private void OnValidate()
        {
            if (_tier2Target < _tier1Target) _tier2Target = _tier1Target;
            if (_tier3Target < _tier2Target) _tier3Target = _tier2Target;
        }

        public int CaptureCount      => _captureCount;
        public int TiersReached      => _tiersReached;
        public int TotalBonusAwarded => _totalBonusAwarded;
        public int Tier1Target       => _tier1Target;
        public int Tier2Target       => _tier2Target;
        public int Tier3Target       => _tier3Target;
        public int Tier1Bonus        => _tier1Bonus;
        public int Tier2Bonus        => _tier2Bonus;
        public int Tier3Bonus        => _tier3Bonus;

        /// <summary>Records a player zone capture toward end-of-match tiers.</summary>
        public void RecordCapture() => _captureCount++;

        /// <summary>Returns the current achieved tier (0–3) without awarding.</summary>
        public int ComputeTier()
        {
            if (_captureCount >= _tier3Target) return 3;
            if (_captureCount >= _tier2Target) return 2;
            if (_captureCount >= _tier1Target) return 1;
            return 0;
        }

        /// <summary>
        /// Awards cumulative bonuses for each newly achieved tier and fires
        /// <c>_onBonusApplied</c>.  Returns the total bonus awarded this call.
        /// </summary>
        public int ApplyEndBonus()
        {
            int currentTier = ComputeTier();
            int bonus       = 0;

            for (int t = _tiersReached + 1; t <= currentTier; t++)
            {
                bonus += t switch
                {
                    1 => _tier1Bonus,
                    2 => _tier2Bonus,
                    3 => _tier3Bonus,
                    _ => 0
                };
            }

            if (bonus > 0)
            {
                _tiersReached      = currentTier;
                _totalBonusAwarded += bonus;
                _onBonusApplied?.Raise();
            }

            return bonus;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _captureCount     = 0;
            _tiersReached     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

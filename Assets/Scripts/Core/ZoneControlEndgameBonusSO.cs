using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that computes a post-match bonus proportional to the number of
    /// zones the player owns at match end, provided they own at least
    /// <see cref="MinimumZonesRequired"/> zones.
    ///
    /// Call <see cref="ApplyBonus(int)"/> at match end with the current player-owned
    /// zone count.  The bonus is credited to the wallet by the controller.
    /// <see cref="Reset"/> clears all runtime state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlEndgameBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlEndgameBonus", order = 93)]
    public sealed class ZoneControlEndgameBonusSO : ScriptableObject
    {
        [Header("Endgame Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _bonusPerZone = 100;

        [Min(1)]
        [SerializeField] private int _minimumZonesRequired = 2;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBonusApplied;

        private int _lastBonusAmount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int BonusPerZone          => _bonusPerZone;
        public int MinimumZonesRequired  => _minimumZonesRequired;
        public int LastBonusAmount       => _lastBonusAmount;
        public int TotalBonusAwarded     => _totalBonusAwarded;

        /// <summary>
        /// Returns the bonus for the given zone count without side effects.
        /// Returns zero when <paramref name="zonesOwned"/> is below
        /// <see cref="MinimumZonesRequired"/>.
        /// </summary>
        public int ComputeBonus(int zonesOwned)
        {
            if (zonesOwned < _minimumZonesRequired) return 0;
            return zonesOwned * _bonusPerZone;
        }

        /// <summary>
        /// Computes the bonus for <paramref name="zonesOwned"/>, caches the result,
        /// accumulates <see cref="TotalBonusAwarded"/>, and fires
        /// <c>_onBonusApplied</c> when the bonus is greater than zero.
        /// </summary>
        public void ApplyBonus(int zonesOwned)
        {
            _lastBonusAmount = ComputeBonus(zonesOwned);
            if (_lastBonusAmount <= 0) return;

            _totalBonusAwarded += _lastBonusAmount;
            _onBonusApplied?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lastBonusAmount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

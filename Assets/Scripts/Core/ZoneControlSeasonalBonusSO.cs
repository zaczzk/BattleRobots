using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data-driven ScriptableObject that defines the currency bonus awarded at
    /// the end of each season, scaled by the player's league division.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="GetBonusForDivision"/> returns the configured bonus for a given
    ///   <see cref="ZoneControlLeagueDivision"/>.
    ///   <see cref="AwardBonus"/> calculates and records the bonus for the given
    ///   division, updates <see cref="LastBonusAmount"/> and
    ///   <see cref="TotalBonusAwarded"/>, then fires <c>_onBonusAwarded</c>.
    ///   <see cref="Reset"/> clears accumulators silently.
    ///   <c>OnEnable</c> calls <see cref="Reset"/> to prevent cross-session leaks.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlSeasonalBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlSeasonalBonus", order = 70)]
    public sealed class ZoneControlSeasonalBonusSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Division Bonuses")]
        [Tooltip("Currency bonus for Bronze division.")]
        [Min(0)]
        [SerializeField] private int _bronzeBonus = 100;

        [Tooltip("Currency bonus for Silver division.")]
        [Min(0)]
        [SerializeField] private int _silverBonus = 250;

        [Tooltip("Currency bonus for Gold division.")]
        [Min(0)]
        [SerializeField] private int _goldBonus = 500;

        [Tooltip("Currency bonus for Platinum division.")]
        [Min(0)]
        [SerializeField] private int _platinumBonus = 1000;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time a seasonal bonus is awarded.")]
        [SerializeField] private VoidGameEvent _onBonusAwarded;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _totalBonusAwarded;
        private int _lastBonusAmount;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Bonus for the Bronze division.</summary>
        public int BronzeBonus => _bronzeBonus;

        /// <summary>Bonus for the Silver division.</summary>
        public int SilverBonus => _silverBonus;

        /// <summary>Bonus for the Gold division.</summary>
        public int GoldBonus => _goldBonus;

        /// <summary>Bonus for the Platinum division.</summary>
        public int PlatinumBonus => _platinumBonus;

        /// <summary>Sum of all bonuses awarded this session.</summary>
        public int TotalBonusAwarded => _totalBonusAwarded;

        /// <summary>Amount awarded in the most recent <see cref="AwardBonus"/> call.</summary>
        public int LastBonusAmount => _lastBonusAmount;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the configured bonus amount for <paramref name="division"/>.
        /// </summary>
        public int GetBonusForDivision(ZoneControlLeagueDivision division)
        {
            switch (division)
            {
                case ZoneControlLeagueDivision.Silver:   return _silverBonus;
                case ZoneControlLeagueDivision.Gold:     return _goldBonus;
                case ZoneControlLeagueDivision.Platinum: return _platinumBonus;
                default:                                 return _bronzeBonus;
            }
        }

        /// <summary>
        /// Calculates the bonus for <paramref name="division"/>, accumulates it,
        /// and fires <c>_onBonusAwarded</c>.  Zero allocation.
        /// </summary>
        public void AwardBonus(ZoneControlLeagueDivision division)
        {
            int bonus         = GetBonusForDivision(division);
            _lastBonusAmount  = bonus;
            _totalBonusAwarded += bonus;
            _onBonusAwarded?.Raise();
        }

        /// <summary>
        /// Clears accumulators silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalBonusAwarded = 0;
            _lastBonusAmount   = 0;
        }

        private void OnValidate()
        {
            _bronzeBonus   = Mathf.Max(0, _bronzeBonus);
            _silverBonus   = Mathf.Max(0, _silverBonus);
            _goldBonus     = Mathf.Max(0, _goldBonus);
            _platinumBonus = Mathf.Max(0, _platinumBonus);
        }
    }
}

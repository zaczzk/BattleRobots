using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// End-of-match bonus scaled by the player's capture lead over the bot.
    /// <c>ComputeLeadBonus</c> returns <c>lead × _bonusPerLead</c> clamped to
    /// <c>_maxBonus</c>. <c>ApplyFinisher</c> caches the result, accumulates
    /// the total and fires <c>_onFinisherApplied</c> when the bonus is positive.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchFinisher.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchFinisher", order = 133)]
    public sealed class ZoneControlMatchFinisherSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerLead = 50;
        [SerializeField, Min(0)] private int _maxBonus     = 1000;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFinisherApplied;

        private int _lastBonus;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int BonusPerLead      => _bonusPerLead;
        public int MaxBonus          => _maxBonus;
        public int LastBonus         => _lastBonus;
        public int TotalBonusAwarded => _totalBonusAwarded;

        public int ComputeLeadBonus(int playerCaptures, int botCaptures)
        {
            int lead = playerCaptures - botCaptures;
            if (lead <= 0) return 0;
            return Mathf.Min(lead * _bonusPerLead, _maxBonus);
        }

        public int ApplyFinisher(int playerCaptures, int botCaptures)
        {
            int bonus = ComputeLeadBonus(playerCaptures, botCaptures);
            _lastBonus = bonus;
            if (bonus > 0)
            {
                _totalBonusAwarded += bonus;
                _onFinisherApplied?.Raise();
            }
            return bonus;
        }

        public void Reset()
        {
            _lastBonus         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

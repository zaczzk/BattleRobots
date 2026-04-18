using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that awards a wallet bonus each time the player captures a zone while trailing.
    /// <see cref="RecordComeback"/> must be called by the controller when a trailing-capture occurs.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlComebackBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlComebackBonus", order = 111)]
    public sealed class ZoneControlComebackBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusOnComeback = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComebackBonus;

        private int _comebackCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int BonusOnComeback   => _bonusOnComeback;
        public int ComebackCount     => _comebackCount;
        public int TotalBonusAwarded => _totalBonusAwarded;

        /// <summary>Records one comeback capture and accumulates the bonus.</summary>
        public void RecordComeback()
        {
            _comebackCount++;
            _totalBonusAwarded += _bonusOnComeback;
            _onComebackBonus?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _comebackCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

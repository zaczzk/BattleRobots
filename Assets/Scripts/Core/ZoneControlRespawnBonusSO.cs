using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks player respawns and accumulates a wallet bonus.
    ///
    /// Call <see cref="RecordRespawn"/> each time the player respawns.
    /// Fires <c>_onRespawnBonusAwarded</c> on every respawn.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRespawnBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRespawnBonus", order = 87)]
    public sealed class ZoneControlRespawnBonusSO : ScriptableObject
    {
        [Header("Respawn Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _bonusPerRespawn = 50;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRespawnBonusAwarded;

        private int _respawnCount;

        private void OnEnable() => Reset();

        public int BonusPerRespawn    => _bonusPerRespawn;
        public int RespawnCount       => _respawnCount;
        public int TotalBonusAwarded  => _respawnCount * _bonusPerRespawn;

        /// <summary>Records a respawn and fires the bonus-awarded event.</summary>
        public void RecordRespawn()
        {
            _respawnCount++;
            _onRespawnBonusAwarded?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _respawnCount = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that awards a score bonus when the player simultaneously holds
    /// at least <c>_multiHoldThreshold</c> zones.  Each call to
    /// <see cref="ApplyBonus"/> computes <c>zonesHeld × bonusPerZone</c> (when
    /// at or above the threshold) and accumulates the total.
    ///
    /// Call <see cref="SetZonesHeld"/> whenever zone ownership changes, then
    /// call <see cref="ApplyBonus"/> at the desired award moment (e.g. match end).
    /// Fires <c>_onBonusTriggered</c> when a positive bonus is awarded.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneScoreBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneScoreBonus", order = 85)]
    public sealed class ZoneControlZoneScoreBonusSO : ScriptableObject
    {
        [Header("Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _bonusPerZoneHeld = 50;

        [Min(2)]
        [SerializeField] private int _multiHoldThreshold = 2;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBonusTriggered;

        private int _zonesHeld;
        private int _totalBonusAwarded;
        private int _lastBonusAmount;

        private void OnEnable() => Reset();

        public int ZonesHeld          => _zonesHeld;
        public int MultiHoldThreshold => _multiHoldThreshold;
        public int BonusPerZoneHeld   => _bonusPerZoneHeld;
        public int TotalBonusAwarded  => _totalBonusAwarded;
        public int LastBonusAmount    => _lastBonusAmount;

        /// <summary>Updates the number of zones currently held by the player.</summary>
        public void SetZonesHeld(int count)
        {
            _zonesHeld = Mathf.Max(0, count);
        }

        /// <summary>
        /// Returns the bonus the player would receive right now based on
        /// <see cref="ZonesHeld"/>.  Returns zero when below
        /// <see cref="MultiHoldThreshold"/>.
        /// </summary>
        public int ComputeBonus()
        {
            return _zonesHeld >= _multiHoldThreshold ? _zonesHeld * _bonusPerZoneHeld : 0;
        }

        /// <summary>
        /// Computes and records the bonus.  Fires <c>_onBonusTriggered</c> when
        /// the bonus is greater than zero.
        /// </summary>
        public void ApplyBonus()
        {
            _lastBonusAmount   = ComputeBonus();
            _totalBonusAwarded += _lastBonusAmount;
            if (_lastBonusAmount > 0)
                _onBonusTriggered?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _zonesHeld         = 0;
            _totalBonusAwarded = 0;
            _lastBonusAmount   = 0;
        }
    }
}

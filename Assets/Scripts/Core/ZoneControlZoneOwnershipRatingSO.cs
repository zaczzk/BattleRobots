using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that measures how often the player holds majority control of
    /// zones during a match.  On each call to <see cref="RecordControlTick"/>,
    /// <c>_totalTicks</c> is incremented; if the player owns more than half the
    /// available zones, <c>_controlTicks</c> is also incremented.
    ///
    /// <see cref="OwnershipRating"/> is the ratio of control ticks to total ticks
    /// and ranges from 0 to 1.  Fires <c>_onRatingUpdated</c> on every tick.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneOwnershipRating.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneOwnershipRating", order = 97)]
    public sealed class ZoneControlZoneOwnershipRatingSO : ScriptableObject
    {
        [Header("Ownership Rating Settings")]
        [Min(1)]
        [SerializeField] private int _totalZones = 4;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRatingUpdated;

        private int _totalTicks;
        private int _controlTicks;

        private void OnEnable() => Reset();

        public int   TotalZones     => _totalZones;
        public int   TotalTicks     => _totalTicks;
        public int   ControlTicks   => _controlTicks;

        /// <summary>
        /// Ownership rating [0,1]: fraction of ticks in which the player held
        /// majority control.  Returns 0 when no ticks have been recorded.
        /// </summary>
        public float OwnershipRating =>
            _totalTicks > 0 ? Mathf.Clamp01((float)_controlTicks / _totalTicks) : 0f;

        /// <summary>
        /// Records one tick.  Counts as a "control tick" when
        /// <paramref name="playerZones"/> exceeds half of <see cref="TotalZones"/>.
        /// </summary>
        public void RecordControlTick(int playerZones, int totalZones)
        {
            _totalTicks++;

            if (playerZones * 2 > totalZones)
                _controlTicks++;

            _onRatingUpdated?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _totalTicks   = 0;
            _controlTicks = 0;
        }

        private void OnValidate()
        {
            _totalZones = Mathf.Max(1, _totalZones);
        }
    }
}

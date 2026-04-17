using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that counts zone ownership flips (each time any zone changes hands)
    /// and fires <c>_onFlipMilestoneReached</c> every <see cref="MilestoneInterval"/>
    /// flips, awarding a configurable wallet bonus.
    ///
    /// Call <see cref="RecordFlip"/> whenever a zone changes ownership.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneFlipTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneFlipTracker", order = 91)]
    public sealed class ZoneControlZoneFlipTrackerSO : ScriptableObject
    {
        [Header("Flip Settings")]
        [Min(1)]
        [SerializeField] private int _milestoneInterval = 5;

        [Min(0)]
        [SerializeField] private int _bonusPerMilestone = 75;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlipMilestoneReached;

        private int _totalFlips;
        private int _milestonesReached;

        private void OnEnable() => Reset();

        public int MilestoneInterval  => _milestoneInterval;
        public int BonusPerMilestone  => _bonusPerMilestone;
        public int TotalFlips         => _totalFlips;
        public int MilestonesReached  => _milestonesReached;

        /// <summary>
        /// Returns the flip count at which the next milestone fires, or
        /// <c>(_milestonesReached + 1) * _milestoneInterval</c>.
        /// </summary>
        public int NextMilestone => (_milestonesReached + 1) * _milestoneInterval;

        /// <summary>
        /// Records one zone-ownership flip.  Fires <c>_onFlipMilestoneReached</c>
        /// each time the total crosses a milestone boundary.
        /// </summary>
        public void RecordFlip()
        {
            _totalFlips++;

            while (_totalFlips >= NextMilestone)
            {
                _milestonesReached++;
                _onFlipMilestoneReached?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _totalFlips       = 0;
            _milestonesReached = 0;
        }
    }
}

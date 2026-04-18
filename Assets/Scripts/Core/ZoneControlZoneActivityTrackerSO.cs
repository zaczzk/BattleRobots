using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that counts total zone-capture activity per match and fires
    /// <c>_onActivityMilestone</c> each time <c>_milestoneStep</c> additional captures are logged
    /// (multi-milestone safe).
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneActivityTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneActivityTracker", order = 113)]
    public sealed class ZoneControlZoneActivityTrackerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _milestoneStep = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onActivityMilestone;

        private int _totalActivity;
        private int _milestonesReached;

        private void OnEnable() => Reset();

        public int MilestoneStep     => _milestoneStep;
        public int TotalActivity     => _totalActivity;
        public int MilestonesReached => _milestonesReached;
        public int NextMilestone     => (_milestonesReached + 1) * _milestoneStep;

        /// <summary>
        /// Records one capture activity event and fires <c>_onActivityMilestone</c> for each
        /// milestone crossed (multi-milestone safe).
        /// </summary>
        public void RecordActivity()
        {
            _totalActivity++;
            while (_totalActivity >= (_milestonesReached + 1) * _milestoneStep)
            {
                _milestonesReached++;
                _onActivityMilestone?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _totalActivity     = 0;
            _milestonesReached = 0;
        }
    }
}

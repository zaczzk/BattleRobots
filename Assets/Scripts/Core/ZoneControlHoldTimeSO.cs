using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks continuous zone hold time and fires
    /// <c>_onMilestoneReached</c> each time the player holds a zone for another
    /// <c>_milestoneInterval</c> seconds.
    ///
    /// Call <see cref="StartHolding"/> when the player captures a zone and
    /// <see cref="StopHolding"/> when the zone is lost.
    /// Drive <see cref="Tick"/> from a MonoBehaviour Update loop.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlHoldTime.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlHoldTime", order = 82)]
    public sealed class ZoneControlHoldTimeSO : ScriptableObject
    {
        [Header("Hold Settings")]
        [Min(1f)]
        [SerializeField] private float _milestoneInterval = 30f;

        [Min(0)]
        [SerializeField] private int _bonusPerMilestone = 75;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        private float _totalHoldTime;
        private bool  _isHolding;
        private int   _milestoneCount;
        private int   _totalBonusAwarded;
        private float _nextMilestone;

        private void OnEnable() => Reset();

        public bool  IsHolding          => _isHolding;
        public float TotalHoldTime      => _totalHoldTime;
        public int   MilestoneCount     => _milestoneCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float MilestoneInterval  => _milestoneInterval;
        public int   BonusPerMilestone  => _bonusPerMilestone;

        /// <summary>Normalised progress toward the next milestone [0, 1].</summary>
        public float HoldProgress =>
            _milestoneInterval > 0f
                ? Mathf.Clamp01((_totalHoldTime % _milestoneInterval) / _milestoneInterval)
                : 0f;

        /// <summary>Arms the hold timer.  No-op when already holding.</summary>
        public void StartHolding()
        {
            _isHolding = true;
        }

        /// <summary>Disarms the hold timer.  Accumulated time is preserved.</summary>
        public void StopHolding()
        {
            _isHolding = false;
        }

        /// <summary>
        /// Advances the hold timer by <paramref name="dt"/> seconds.
        /// No-op when not holding.  Fires a milestone event for each completed
        /// interval, supporting multi-milestone ticks.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isHolding) return;
            _totalHoldTime += dt;
            while (_totalHoldTime >= _nextMilestone)
            {
                _milestoneCount++;
                _totalBonusAwarded += _bonusPerMilestone;
                _nextMilestone      += _milestoneInterval;
                _onMilestoneReached?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _totalHoldTime     = 0f;
            _isHolding         = false;
            _milestoneCount    = 0;
            _totalBonusAwarded = 0;
            _nextMilestone     = _milestoneInterval;
        }
    }
}

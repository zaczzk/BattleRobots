using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks cumulative dominance-hold time and awards interval bonuses while
    /// the player holds zone majority.  <see cref="StartDominance"/> arms the timer;
    /// <see cref="EndDominance"/> disarms it.  <see cref="Tick"/> must be driven each frame.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlDominanceTimer.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlDominanceTimer", order = 112)]
    public sealed class ZoneControlDominanceTimerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _bonusInterval    = 15f;
        [SerializeField, Min(0)]  private int   _bonusPerInterval = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceInterval;

        private bool  _isDominating;
        private float _totalDominanceTime;
        private float _nextMilestone;
        private int   _intervalsCompleted;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public bool  IsDominating       => _isDominating;
        public float TotalDominanceTime => _totalDominanceTime;
        public int   IntervalsCompleted => _intervalsCompleted;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float BonusInterval      => _bonusInterval;
        public int   BonusPerInterval   => _bonusPerInterval;

        public float DominanceProgress =>
            _bonusInterval > 0f
                ? Mathf.Clamp01((_totalDominanceTime % _bonusInterval) / _bonusInterval)
                : 0f;

        /// <summary>Arms the dominance timer.  Idempotent when already dominating.</summary>
        public void StartDominance()
        {
            _isDominating = true;
        }

        /// <summary>Disarms the dominance timer.</summary>
        public void EndDominance()
        {
            _isDominating = false;
        }

        /// <summary>
        /// Accumulates dominance time and fires <c>_onDominanceInterval</c> for each
        /// completed interval (multi-milestone safe).
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isDominating) return;
            _totalDominanceTime += dt;
            while (_totalDominanceTime >= _nextMilestone)
            {
                _intervalsCompleted++;
                _totalBonusAwarded += _bonusPerInterval;
                _nextMilestone     += _bonusInterval;
                _onDominanceInterval?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _isDominating       = false;
            _totalDominanceTime = 0f;
            _nextMilestone      = _bonusInterval;
            _intervalsCompleted = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

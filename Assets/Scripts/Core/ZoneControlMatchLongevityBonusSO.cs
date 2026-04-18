using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that rewards the player with a bonus for every
    /// <c>_intervalSeconds</c> of match time elapsed.
    ///
    /// Call <see cref="StartTracking"/> when a match begins and
    /// <see cref="StopTracking"/> when it ends.
    /// Drive <see cref="Tick"/> from a MonoBehaviour Update loop.
    /// Fires <c>_onLongevityBonus</c> for each completed interval (multi-interval safe).
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchLongevityBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchLongevityBonus", order = 101)]
    public sealed class ZoneControlMatchLongevityBonusSO : ScriptableObject
    {
        [Header("Longevity Settings")]
        [Min(10f)]
        [SerializeField] private float _intervalSeconds = 30f;

        [Min(0)]
        [SerializeField] private int _bonusPerInterval = 75;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLongevityBonus;

        private float _elapsedTime;
        private int   _intervalsCompleted;
        private int   _totalBonusAwarded;
        private bool  _isRunning;
        private float _nextMilestone;

        private void OnEnable() => Reset();

        public float ElapsedTime          => _elapsedTime;
        public int   IntervalsCompleted   => _intervalsCompleted;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float IntervalSeconds      => _intervalSeconds;
        public int   BonusPerInterval     => _bonusPerInterval;
        public bool  IsRunning            => _isRunning;

        /// <summary>Arms the elapsed-time tracker.  No-op when already running.</summary>
        public void StartTracking()
        {
            _isRunning = true;
        }

        /// <summary>Disarms the tracker.</summary>
        public void StopTracking()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Advances the match timer by <paramref name="dt"/> seconds.
        /// No-op when not running.  Fires a bonus event for each completed
        /// interval, supporting multi-interval ticks.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isRunning) return;
            _elapsedTime += dt;
            while (_elapsedTime >= _nextMilestone)
            {
                _intervalsCompleted++;
                _totalBonusAwarded += _bonusPerInterval;
                _nextMilestone     += _intervalSeconds;
                _onLongevityBonus?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _elapsedTime        = 0f;
            _intervalsCompleted = 0;
            _totalBonusAwarded  = 0;
            _isRunning          = false;
            _nextMilestone      = _intervalSeconds;
        }
    }
}

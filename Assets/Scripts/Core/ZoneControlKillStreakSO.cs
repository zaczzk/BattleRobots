using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks consecutive bot defeats within a rolling time window.
    /// A streak is active while defeats arrive faster than <c>_streakWindowSeconds</c> apart.
    /// Fires <c>_onKillStreakStarted</c>/<c>_onKillStreakEnded</c> on transitions and
    /// <c>_onKillDuringSurge</c> for each kill recorded while the streak is active.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlKillStreak.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlKillStreak", order = 122)]
    public sealed class ZoneControlKillStreakSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)]   private int   _streakThreshold    = 3;
        [SerializeField, Min(0.1f)] private float _streakWindowSeconds = 6f;
        [SerializeField, Min(0)]   private int   _bonusPerKillInStreak = 50;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onKillStreakStarted;
        [SerializeField] private VoidGameEvent _onKillStreakEnded;
        [SerializeField] private VoidGameEvent _onKillDuringSurge;

        private readonly List<float> _killTimestamps = new List<float>();
        private bool _isStreaking;
        private int  _totalStreakKills;
        private int  _totalBonusAwarded;
        private int  _streakCount;

        private void OnEnable() => Reset();

        public int   StreakThreshold     => _streakThreshold;
        public float StreakWindowSeconds => _streakWindowSeconds;
        public int   BonusPerKillInStreak => _bonusPerKillInStreak;
        public bool  IsStreaking          => _isStreaking;
        public int   TotalStreakKills     => _totalStreakKills;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public int   StreakCount         => _streakCount;
        public int   CurrentWindowKills  => _killTimestamps.Count;

        /// <summary>Records a bot defeat at the given game time and evaluates streak state.</summary>
        public void RecordKill(float gameTime)
        {
            Prune(gameTime);
            _killTimestamps.Add(gameTime);

            if (_isStreaking)
            {
                _totalStreakKills++;
                _totalBonusAwarded += _bonusPerKillInStreak;
                _onKillDuringSurge?.Raise();
            }

            EvaluateStreak();
        }

        /// <summary>Advances the internal prune clock (call from Update).</summary>
        public void Tick(float gameTime)
        {
            Prune(gameTime);
            EvaluateStreak();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _killTimestamps.Clear();
            _isStreaking       = false;
            _totalStreakKills  = 0;
            _totalBonusAwarded = 0;
            _streakCount       = 0;
        }

        private void Prune(float gameTime)
        {
            float cutoff = gameTime - _streakWindowSeconds;
            _killTimestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateStreak()
        {
            bool shouldStreak = _killTimestamps.Count >= _streakThreshold;
            if (shouldStreak && !_isStreaking)
            {
                _isStreaking = true;
                _streakCount++;
                _onKillStreakStarted?.Raise();
            }
            else if (!shouldStreak && _isStreaking)
            {
                _isStreaking = false;
                _onKillStreakEnded?.Raise();
            }
        }
    }
}

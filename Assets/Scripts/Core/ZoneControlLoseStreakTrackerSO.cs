using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks consecutive bot zone captures without an
    /// intervening player capture.  When the streak reaches
    /// <c>_warningThreshold</c>, <c>_onLoseStreakWarning</c> fires (idempotent).
    /// Any player capture resets the streak and fires <c>_onLoseStreakReset</c>.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlLoseStreakTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlLoseStreakTracker", order = 103)]
    public sealed class ZoneControlLoseStreakTrackerSO : ScriptableObject
    {
        [Header("Lose Streak Settings")]
        [Min(1)]
        [SerializeField] private int _warningThreshold = 3;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLoseStreakWarning;
        [SerializeField] private VoidGameEvent _onLoseStreakReset;

        private int  _loseStreak;
        private bool _warnFired;

        private void OnEnable() => Reset();

        public int  LoseStreak        => _loseStreak;
        public int  WarningThreshold  => _warningThreshold;
        public bool IsWarning         => _warnFired;

        /// <summary>Records a bot zone capture, advancing the lose streak.</summary>
        public void RecordBotCapture()
        {
            _loseStreak++;
            if (!_warnFired && _loseStreak >= _warningThreshold)
            {
                _warnFired = true;
                _onLoseStreakWarning?.Raise();
            }
        }

        /// <summary>Records a player zone capture, resetting the lose streak.</summary>
        public void RecordPlayerCapture()
        {
            if (_loseStreak > 0)
            {
                _loseStreak = 0;
                _warnFired  = false;
                _onLoseStreakReset?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _loseStreak = 0;
            _warnFired  = false;
        }
    }
}

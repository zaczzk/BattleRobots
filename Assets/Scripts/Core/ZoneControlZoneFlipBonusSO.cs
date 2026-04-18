using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that awards a bonus whenever the player recaptures a zone that
    /// was most recently captured by a bot ("zone flip").
    ///
    /// Call <see cref="RecordBotCapture"/> when a bot takes a zone, then
    /// <see cref="RecordPlayerCapture"/> when the player retakes it.  If the last
    /// recorded event was a bot capture, a flip is credited and
    /// <c>_onFlipBonus</c> is fired.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneFlipBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneFlipBonus", order = 101)]
    public sealed class ZoneControlZoneFlipBonusSO : ScriptableObject
    {
        [Header("Flip Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _bonusPerFlip = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlipBonus;

        private int  _flipCount;
        private int  _totalBonusAwarded;
        private bool _botCapturedLast;

        private void OnEnable() => Reset();

        public int  FlipCount         => _flipCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public int  BonusPerFlip      => _bonusPerFlip;
        public bool BotCapturedLast   => _botCapturedLast;

        /// <summary>Records that a bot has captured a zone, arming the flip detector.</summary>
        public void RecordBotCapture()
        {
            _botCapturedLast = true;
        }

        /// <summary>
        /// Records a player capture.  If a bot capture was recorded since the last
        /// player capture, a flip is credited, the bonus is accumulated, and
        /// <c>_onFlipBonus</c> is fired.
        /// </summary>
        public void RecordPlayerCapture()
        {
            if (_botCapturedLast)
            {
                _flipCount++;
                _totalBonusAwarded += _bonusPerFlip;
                _onFlipBonus?.Raise();
            }
            _botCapturedLast = false;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _flipCount         = 0;
            _totalBonusAwarded = 0;
            _botCapturedLast   = false;
        }
    }
}

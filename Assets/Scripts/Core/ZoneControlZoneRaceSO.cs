using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that runs a first-to-N-captures race between the player and a bot.
    ///
    /// Call <see cref="AddPlayerCapture"/> and <see cref="AddBotCapture"/> to track
    /// captures.  The race ends (idempotent) when either side reaches
    /// <see cref="CaptureTarget"/>.  Fires <c>_onPlayerWon</c> or <c>_onBotWon</c>
    /// on the winning transition.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneRace.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneRace", order = 88)]
    public sealed class ZoneControlZoneRaceSO : ScriptableObject
    {
        [Header("Race Settings")]
        [Min(1)]
        [SerializeField] private int _captureTarget = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerWon;
        [SerializeField] private VoidGameEvent _onBotWon;

        private int  _playerCaptures;
        private int  _botCaptures;
        private bool _isRaceComplete;
        private bool _playerWon;

        private void OnEnable() => Reset();

        public int  CaptureTarget    => _captureTarget;
        public int  PlayerCaptures   => _playerCaptures;
        public int  BotCaptures      => _botCaptures;
        public bool IsRaceComplete   => _isRaceComplete;
        public bool PlayerWon        => _playerWon;
        public bool BotWon           => _isRaceComplete && !_playerWon;

        /// <summary>Records a player capture and evaluates the race end condition.</summary>
        public void AddPlayerCapture()
        {
            if (_isRaceComplete) return;
            _playerCaptures++;
            if (_playerCaptures >= _captureTarget)
            {
                _isRaceComplete = true;
                _playerWon      = true;
                _onPlayerWon?.Raise();
            }
        }

        /// <summary>Records a bot capture and evaluates the race end condition.</summary>
        public void AddBotCapture()
        {
            if (_isRaceComplete) return;
            _botCaptures++;
            if (_botCaptures >= _captureTarget)
            {
                _isRaceComplete = true;
                _playerWon      = false;
                _onBotWon?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _playerCaptures = 0;
            _botCaptures    = 0;
            _isRaceComplete = false;
            _playerWon      = false;
        }
    }
}

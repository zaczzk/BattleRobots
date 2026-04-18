using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Awards a one-time wallet bonus to whichever team (player or bot)
    /// captures the very first zone of the match.
    /// Fires <c>_onFirstBloodPlayer</c> when the player is first, or
    /// <c>_onFirstBloodBot</c> when the bot is first.  Idempotent thereafter.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlFirstBloodBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlFirstBloodBonus", order = 132)]
    public sealed class ZoneControlFirstBloodBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _playerFirstBloodBonus = 300;
        [SerializeField, Min(0)] private int _botFirstBloodBonus    = 0;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFirstBloodPlayer;
        [SerializeField] private VoidGameEvent _onFirstBloodBot;

        private bool _firstBloodFired;
        private bool _playerWasFirst;

        private void OnEnable() => Reset();

        public bool PlayerFirstBloodBonus => _playerFirstBloodBonus > 0;
        public int  PlayerBonus           => _playerFirstBloodBonus;
        public int  BotBonus              => _botFirstBloodBonus;
        public bool FirstBloodFired       => _firstBloodFired;
        public bool PlayerWasFirst        => _playerWasFirst;

        public int RecordPlayerCapture()
        {
            if (_firstBloodFired) return 0;
            _firstBloodFired = true;
            _playerWasFirst  = true;
            _onFirstBloodPlayer?.Raise();
            return _playerFirstBloodBonus;
        }

        public int RecordBotCapture()
        {
            if (_firstBloodFired) return 0;
            _firstBloodFired = true;
            _playerWasFirst  = false;
            _onFirstBloodBot?.Raise();
            return _botFirstBloodBonus;
        }

        public void Reset()
        {
            _firstBloodFired = false;
            _playerWasFirst  = false;
        }
    }
}

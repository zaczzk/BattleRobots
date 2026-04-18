using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Awards a wallet bonus each time the player captures a zone while the
    /// bot is ahead in total captures for the match (underdog situation).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneUnderdog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneUnderdog", order = 132)]
    public sealed class ZoneControlZoneUnderdogSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerUnderdogCapture = 125;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onUnderdogBonus;

        private int _playerCaptures;
        private int _botCaptures;
        private int _underdogCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  BonusPerUnderdogCapture => _bonusPerUnderdogCapture;
        public int  PlayerCaptures          => _playerCaptures;
        public int  BotCaptures             => _botCaptures;
        public int  UnderdogCount           => _underdogCount;
        public int  TotalBonusAwarded       => _totalBonusAwarded;
        public bool IsUnderdog              => _botCaptures > _playerCaptures;

        public void RecordPlayerCapture()
        {
            int prevPlayerCaptures = _playerCaptures;
            _playerCaptures++;

            if (_botCaptures > prevPlayerCaptures)
            {
                _underdogCount++;
                _totalBonusAwarded += _bonusPerUnderdogCapture;
                _onUnderdogBonus?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            _botCaptures++;
        }

        public void Reset()
        {
            _playerCaptures    = 0;
            _botCaptures       = 0;
            _underdogCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

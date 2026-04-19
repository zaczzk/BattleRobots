using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlEarlyBirdBonus", order = 145)]
    public sealed class ZoneControlEarlyBirdBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _requiredEarlyCaptures  = 5;
        [SerializeField, Min(0)] private int _bonusOnEarlyDominance  = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEarlyDominance;

        private int  _playerEarlyCaptures;
        private bool _earlyComplete;
        private bool _botInterrupted;

        private void OnEnable() => Reset();

        public int  RequiredEarlyCaptures => _requiredEarlyCaptures;
        public int  BonusOnEarlyDominance => _bonusOnEarlyDominance;
        public int  PlayerEarlyCaptures   => _playerEarlyCaptures;
        public bool EarlyComplete         => _earlyComplete;
        public bool BotInterrupted        => _botInterrupted;
        public float EarlyProgress        => _requiredEarlyCaptures > 0
            ? Mathf.Clamp01((float)_playerEarlyCaptures / _requiredEarlyCaptures)
            : 1f;

        public void RecordPlayerCapture()
        {
            if (_earlyComplete || _botInterrupted) return;
            _playerEarlyCaptures++;
            if (_playerEarlyCaptures >= _requiredEarlyCaptures)
            {
                _earlyComplete = true;
                _onEarlyDominance?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            if (_earlyComplete || _botInterrupted) return;
            _botInterrupted = true;
        }

        public void Reset()
        {
            _playerEarlyCaptures = 0;
            _earlyComplete       = false;
            _botInterrupted      = false;
        }
    }
}

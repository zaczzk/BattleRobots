using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks player capture balance (player captures minus bot recaptures).
    /// Fires <c>_onBalancePositive</c> / <c>_onBalanceNegative</c> on sign transitions (idempotent).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureBalance.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBalance", order = 118)]
    public sealed class ZoneControlCaptureBalanceSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBalancePositive;
        [SerializeField] private VoidGameEvent _onBalanceChanged;
        [SerializeField] private VoidGameEvent _onBalanceNegative;

        private int  _playerCaptures;
        private int  _botRecaptures;
        private bool _wasPositive;

        private void OnEnable() => Reset();

        public int  PlayerCaptures => _playerCaptures;
        public int  BotRecaptures  => _botRecaptures;
        public int  Balance        => _playerCaptures - _botRecaptures;
        public bool IsPositive     => Balance > 0;

        /// <summary>Records a player capture and evaluates sign transition.</summary>
        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            _onBalanceChanged?.Raise();
            EvaluateBalance();
        }

        /// <summary>Records a bot recapture and evaluates sign transition.</summary>
        public void RecordBotRecapture()
        {
            _botRecaptures++;
            _onBalanceChanged?.Raise();
            EvaluateBalance();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _playerCaptures = 0;
            _botRecaptures  = 0;
            _wasPositive    = false;
        }

        private void EvaluateBalance()
        {
            bool positive = Balance > 0;
            if (positive && !_wasPositive)
            {
                _wasPositive = true;
                _onBalancePositive?.Raise();
            }
            else if (!positive && _wasPositive)
            {
                _wasPositive = false;
                _onBalanceNegative?.Raise();
            }
        }
    }
}

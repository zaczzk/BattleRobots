using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks momentum shifts — changes in who holds the capture lead
    /// (player vs bot). Fires <c>_onMomentumShift</c> each time the lead holder changes
    /// (idempotent on ties; first lead established silently).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureMomentumShift.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMomentumShift", order = 114)]
    public sealed class ZoneControlCaptureMomentumShiftSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMomentumShift;
        [SerializeField] private VoidGameEvent _onCapturesUpdated;

        private int  _playerCaptures;
        private int  _botCaptures;
        private bool _leadEstablished;
        private bool _wasPlayerLeading;
        private int  _shiftCount;

        private void OnEnable() => Reset();

        public int  PlayerCaptures  => _playerCaptures;
        public int  BotCaptures     => _botCaptures;
        public int  ShiftCount      => _shiftCount;
        public bool IsPlayerLeading => _playerCaptures > _botCaptures;
        public bool IsTied          => _playerCaptures == _botCaptures;

        /// <summary>Records a player zone capture and evaluates lead transitions.</summary>
        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            _onCapturesUpdated?.Raise();
            EvaluateLead();
        }

        /// <summary>Records a bot zone capture and evaluates lead transitions.</summary>
        public void RecordBotCapture()
        {
            _botCaptures++;
            _onCapturesUpdated?.Raise();
            EvaluateLead();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _playerCaptures  = 0;
            _botCaptures     = 0;
            _shiftCount      = 0;
            _leadEstablished = false;
            _wasPlayerLeading = false;
        }

        private void EvaluateLead()
        {
            if (_playerCaptures == _botCaptures) return;

            bool playerLeading = _playerCaptures > _botCaptures;
            if (!_leadEstablished)
            {
                _leadEstablished  = true;
                _wasPlayerLeading = playerLeading;
                return;
            }

            if (playerLeading == _wasPlayerLeading) return;

            _shiftCount++;
            _wasPlayerLeading = playerLeading;
            _onMomentumShift?.Raise();
        }
    }
}

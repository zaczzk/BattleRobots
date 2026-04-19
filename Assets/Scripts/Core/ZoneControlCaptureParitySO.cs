using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureParity", order = 168)]
    public sealed class ZoneControlCaptureParitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerParity = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onParity;

        private int  _playerCaptures;
        private int  _botCaptures;
        private int  _parityCount;
        private int  _totalBonusAwarded;
        private bool _lastWasTied;

        private void OnEnable() => Reset();

        public int  PlayerCaptures    => _playerCaptures;
        public int  BotCaptures       => _botCaptures;
        public int  ParityCount       => _parityCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public int  BonusPerParity    => _bonusPerParity;
        public bool AreTied           => _playerCaptures > 0 && _playerCaptures == _botCaptures;

        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            EvaluateParity();
        }

        public void RecordBotCapture()
        {
            _botCaptures++;
            EvaluateParity();
        }

        private void EvaluateParity()
        {
            bool tied = AreTied;
            if (tied && !_lastWasTied)
            {
                _parityCount++;
                _totalBonusAwarded += _bonusPerParity;
                _onParity?.Raise();
            }
            _lastWasTied = tied;
        }

        public void Reset()
        {
            _playerCaptures    = 0;
            _botCaptures       = 0;
            _parityCount       = 0;
            _totalBonusAwarded = 0;
            _lastWasTied       = false;
        }
    }
}

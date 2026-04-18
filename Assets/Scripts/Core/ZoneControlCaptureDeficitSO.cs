using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks the capture deficit (bot captures minus player captures).
    /// Positive deficit means bots are ahead; negative means player is ahead.
    /// Fires <c>_onHighDeficit</c> when deficit first reaches <c>_highDeficitThreshold</c>
    /// and <c>_onDeficitCleared</c> when deficit drops back to zero or below.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureDeficit.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDeficit", order = 126)]
    public sealed class ZoneControlCaptureDeficitSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _highDeficitThreshold = 3;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHighDeficit;
        [SerializeField] private VoidGameEvent _onDeficitCleared;

        private int  _playerCaptures;
        private int  _botCaptures;
        private bool _isHighDeficit;

        private void OnEnable() => Reset();

        public int  PlayerCaptures      => _playerCaptures;
        public int  BotCaptures         => _botCaptures;
        public int  Deficit             => _botCaptures - _playerCaptures;
        public bool IsHighDeficit       => _isHighDeficit;
        public int  HighDeficitThreshold => _highDeficitThreshold;

        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            EvaluateDeficit();
        }

        public void RecordBotCapture()
        {
            _botCaptures++;
            EvaluateDeficit();
        }

        private void EvaluateDeficit()
        {
            int deficit = Deficit;
            if (!_isHighDeficit && deficit >= _highDeficitThreshold)
            {
                _isHighDeficit = true;
                _onHighDeficit?.Raise();
            }
            else if (_isHighDeficit && deficit <= 0)
            {
                _isHighDeficit = false;
                _onDeficitCleared?.Raise();
            }
        }

        public void Reset()
        {
            _playerCaptures = 0;
            _botCaptures    = 0;
            _isHighDeficit  = false;
        }
    }
}

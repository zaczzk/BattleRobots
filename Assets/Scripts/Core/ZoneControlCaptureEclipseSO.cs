using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEclipse", order = 180)]
    public sealed class ZoneControlCaptureEclipseSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _eclipseMargin          = 3;
        [SerializeField, Min(0)] private int _bonusPerEclipseCapture = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEclipseStarted;
        [SerializeField] private VoidGameEvent _onEclipseEnded;

        private int  _playerCaptures;
        private int  _botCaptures;
        private bool _isEclipsed;
        private int  _eclipseCaptures;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  EclipseMargin          => _eclipseMargin;
        public int  BonusPerEclipseCapture => _bonusPerEclipseCapture;
        public bool IsEclipsed             => _isEclipsed;
        public int  EclipseCaptures        => _eclipseCaptures;
        public int  TotalBonusAwarded      => _totalBonusAwarded;
        public int  PlayerCaptures         => _playerCaptures;
        public int  BotCaptures            => _botCaptures;

        public void RecordPlayerCapture()
        {
            if (_isEclipsed)
            {
                _eclipseCaptures++;
                _totalBonusAwarded += _bonusPerEclipseCapture;
            }
            _playerCaptures++;
            EvaluateEclipse();
        }

        public void RecordBotCapture()
        {
            _botCaptures++;
            EvaluateEclipse();
        }

        private void EvaluateEclipse()
        {
            bool shouldEclipse = (_botCaptures - _playerCaptures) >= _eclipseMargin;
            if (shouldEclipse && !_isEclipsed)
            {
                _isEclipsed = true;
                _onEclipseStarted?.Raise();
            }
            else if (!shouldEclipse && _isEclipsed)
            {
                _isEclipsed = false;
                _onEclipseEnded?.Raise();
            }
        }

        public void Reset()
        {
            _playerCaptures    = 0;
            _botCaptures       = 0;
            _isEclipsed        = false;
            _eclipseCaptures   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

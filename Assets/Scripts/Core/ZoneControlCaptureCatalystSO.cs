using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCatalyst", order = 193)]
    public sealed class ZoneControlCaptureCatalystSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForActivation     = 3;
        [SerializeField, Min(1)] private int _catalystDurationCaptures  = 4;
        [SerializeField, Min(0)] private int _bonusPerCatalystCapture   = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCatalystActivated;
        [SerializeField] private VoidGameEvent _onCatalystExpired;

        private int  _playerCaptureCount;
        private bool _isCatalystActive;
        private int  _catalystCapturesRemaining;
        private int  _catalystCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  CapturesForActivation    => _capturesForActivation;
        public int  CatalystDurationCaptures => _catalystDurationCaptures;
        public int  BonusPerCatalystCapture  => _bonusPerCatalystCapture;
        public bool IsCatalystActive         => _isCatalystActive;
        public int  CatalystCapturesRemaining => _catalystCapturesRemaining;
        public int  CatalystCount            => _catalystCount;
        public int  TotalBonusAwarded        => _totalBonusAwarded;
        public float ActivationProgress => _capturesForActivation > 0
            ? Mathf.Clamp01(_playerCaptureCount / (float)_capturesForActivation)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_isCatalystActive)
            {
                _catalystCapturesRemaining--;
                _totalBonusAwarded += _bonusPerCatalystCapture;
                if (_catalystCapturesRemaining <= 0)
                    Expire();
                return _bonusPerCatalystCapture;
            }

            _playerCaptureCount++;
            if (_playerCaptureCount >= _capturesForActivation)
                Activate();
            return 0;
        }

        public void RecordBotCapture()
        {
            _playerCaptureCount = 0;
        }

        private void Activate()
        {
            _isCatalystActive          = true;
            _catalystCapturesRemaining = _catalystDurationCaptures;
            _catalystCount++;
            _playerCaptureCount        = 0;
            _onCatalystActivated?.Raise();
        }

        private void Expire()
        {
            _isCatalystActive          = false;
            _catalystCapturesRemaining = 0;
            _onCatalystExpired?.Raise();
        }

        public void Reset()
        {
            _playerCaptureCount        = 0;
            _isCatalystActive          = false;
            _catalystCapturesRemaining = 0;
            _catalystCount             = 0;
            _totalBonusAwarded         = 0;
        }
    }
}

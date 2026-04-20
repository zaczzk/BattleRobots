using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTribe", order = 207)]
    public sealed class ZoneControlCaptureTribeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesPerTier     = 2;
        [SerializeField, Min(1)] private int _maxTier             = 5;
        [SerializeField, Min(0)] private int _bonusPerTierCapture = 35;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMaxTier;

        private int _currentTier;
        private int _capturesSinceLast;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CurrentTier         => _currentTier;
        public int   MaxTier             => _maxTier;
        public int   CapturesPerTier     => _capturesPerTier;
        public int   BonusPerTierCapture => _bonusPerTierCapture;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float TierProgress        => _currentTier >= _maxTier
            ? 1f
            : (_capturesPerTier > 0
                ? Mathf.Clamp01(_capturesSinceLast / (float)_capturesPerTier)
                : 0f);

        public int RecordPlayerCapture()
        {
            _capturesSinceLast++;
            if (_currentTier < _maxTier && _capturesSinceLast >= _capturesPerTier)
            {
                _currentTier++;
                _capturesSinceLast = 0;
                if (_currentTier == _maxTier)
                    _onMaxTier?.Raise();
            }
            int bonus = _bonusPerTierCapture * _currentTier;
            _totalBonusAwarded += bonus;
            return bonus;
        }

        public void RecordBotCapture()
        {
            _currentTier       = 0;
            _capturesSinceLast = 0;
        }

        public void Reset()
        {
            _currentTier       = 0;
            _capturesSinceLast = 0;
            _totalBonusAwarded = 0;
        }
    }
}

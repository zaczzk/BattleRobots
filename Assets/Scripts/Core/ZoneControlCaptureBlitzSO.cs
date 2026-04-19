using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBlitz", order = 196)]
    public sealed class ZoneControlCaptureBlitzSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _blitzTarget   = 5;
        [SerializeField, Min(0)] private int _bonusPerBlitz = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBlitz;

        private int _currentStreak;
        private int _blitzCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BlitzTarget        => _blitzTarget;
        public int   BonusPerBlitz      => _bonusPerBlitz;
        public int   CurrentStreak      => _currentStreak;
        public int   BlitzCount         => _blitzCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float BlitzProgress      => _blitzTarget > 0
            ? Mathf.Clamp01(_currentStreak / (float)_blitzTarget)
            : 0f;

        public int RecordPlayerCapture()
        {
            _currentStreak++;
            if (_currentStreak < _blitzTarget) return 0;
            _blitzCount++;
            _totalBonusAwarded += _bonusPerBlitz;
            _currentStreak      = 0;
            _onBlitz?.Raise();
            return _bonusPerBlitz;
        }

        public void RecordBotCapture()
        {
            _currentStreak = 0;
        }

        public void Reset()
        {
            _currentStreak    = 0;
            _blitzCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

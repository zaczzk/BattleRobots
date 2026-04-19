using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureWarp", order = 182)]
    public sealed class ZoneControlCaptureWarpSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)]    private int   _maxWarpLevel           = 5;
        [SerializeField, Min(0.1f)] private float _warpMultiplierPerLevel = 0.2f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWarpLevelChanged;

        private int _currentStreak;
        private int _currentWarpLevel;

        private void OnEnable() => Reset();

        public int   MaxWarpLevel           => _maxWarpLevel;
        public float WarpMultiplierPerLevel => _warpMultiplierPerLevel;
        public int   CurrentStreak          => _currentStreak;
        public int   CurrentWarpLevel       => _currentWarpLevel;
        public float WarpMultiplier         => 1f + _currentWarpLevel * _warpMultiplierPerLevel;
        public float WarpProgress           => _maxWarpLevel > 0 ? Mathf.Clamp01((float)_currentWarpLevel / _maxWarpLevel) : 0f;

        public int ComputeWarpBonus(int baseAmount)
        {
            return Mathf.RoundToInt(baseAmount * WarpMultiplier);
        }

        public void RecordPlayerCapture()
        {
            _currentStreak++;
            int newLevel = Mathf.Min(_currentStreak, _maxWarpLevel);
            if (newLevel != _currentWarpLevel)
            {
                _currentWarpLevel = newLevel;
                _onWarpLevelChanged?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            if (_currentStreak == 0 && _currentWarpLevel == 0) return;
            _currentStreak    = 0;
            _currentWarpLevel = 0;
            _onWarpLevelChanged?.Raise();
        }

        public void Reset()
        {
            _currentStreak    = 0;
            _currentWarpLevel = 0;
        }
    }
}

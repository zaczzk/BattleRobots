using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStreakBonus", order = 98)]
    public sealed class ZoneControlCaptureStreakBonusSO : ScriptableObject
    {
        [Header("Streak Settings")]
        [Min(0)]
        [SerializeField] private int _baseBonus = 50;

        [Min(0f)]
        [SerializeField] private float _bonusPerStreak = 25f;

        [Min(1)]
        [SerializeField] private int _maxStreak = 10;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStreakBonus;

        private int   _currentStreak;
        private int   _lastBonusAwarded;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CurrentStreak      => _currentStreak;
        public int   MaxStreak          => _maxStreak;
        public int   LastBonusAwarded   => _lastBonusAwarded;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public int   BaseBonus          => _baseBonus;
        public float BonusPerStreak     => _bonusPerStreak;
        public float CurrentMultiplier  => 1f + _currentStreak * (_bonusPerStreak / Mathf.Max(1f, _baseBonus));

        public int RecordCapture()
        {
            _currentStreak = Mathf.Min(_currentStreak + 1, _maxStreak);
            int bonus = Mathf.RoundToInt(_baseBonus + (_currentStreak - 1) * _bonusPerStreak);
            _lastBonusAwarded   = bonus;
            _totalBonusAwarded += bonus;
            _onStreakBonus?.Raise();
            return bonus;
        }

        public void BreakStreak()
        {
            _currentStreak    = 0;
            _lastBonusAwarded = 0;
        }

        public void Reset()
        {
            _currentStreak     = 0;
            _lastBonusAwarded  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

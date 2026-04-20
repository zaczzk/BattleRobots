using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLockbox", order = 234)]
    public sealed class ZoneControlCaptureLockboxSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _startingLocks  = 5;
        [SerializeField, Min(1)] private int _maxLocks       = 8;
        [SerializeField, Min(0)] private int _bonusPerOpen   = 350;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLockboxOpened;

        private int _currentLocks;
        private int _openCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StartingLocks     => _startingLocks;
        public int   MaxLocks          => _maxLocks;
        public int   BonusPerOpen      => _bonusPerOpen;
        public int   CurrentLocks      => _currentLocks;
        public int   OpenCount         => _openCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LockProgress      => _startingLocks > 0
            ? Mathf.Clamp01(1f - _currentLocks / (float)_startingLocks)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_currentLocks <= 0)
                return 0;

            _currentLocks--;
            if (_currentLocks <= 0)
            {
                int bonus = _bonusPerOpen;
                _openCount++;
                _totalBonusAwarded += bonus;
                _currentLocks       = _startingLocks;
                _onLockboxOpened?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _currentLocks = Mathf.Min(_currentLocks + 1, _maxLocks);
        }

        public void Reset()
        {
            _currentLocks      = _startingLocks;
            _openCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

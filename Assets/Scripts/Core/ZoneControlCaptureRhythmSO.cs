using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRhythm", order = 140)]
    public sealed class ZoneControlCaptureRhythmSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _rhythmWindowSeconds = 12f;
        [SerializeField, Min(1)]    private int   _rhythmStreakTarget   = 3;
        [SerializeField, Min(0)]    private int   _bonusPerRhythmStreak = 120;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRhythmAchieved;

        private float _lastCaptureTime   = -1f;
        private int   _rhythmStreak;
        private int   _rhythmCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float RhythmWindowSeconds  => _rhythmWindowSeconds;
        public int   RhythmStreakTarget   => _rhythmStreakTarget;
        public int   BonusPerRhythmStreak => _bonusPerRhythmStreak;
        public int   RhythmStreak         => _rhythmStreak;
        public int   RhythmCount          => _rhythmCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public bool  HasFirstCapture      => _lastCaptureTime >= 0f;

        public void RecordCapture(float currentTime)
        {
            if (HasFirstCapture)
            {
                float gap = currentTime - _lastCaptureTime;
                if (gap <= _rhythmWindowSeconds)
                {
                    _rhythmStreak++;
                    if (_rhythmStreak >= _rhythmStreakTarget)
                    {
                        _rhythmCount++;
                        _totalBonusAwarded += _bonusPerRhythmStreak;
                        _rhythmStreak = 0;
                        _onRhythmAchieved?.Raise();
                    }
                }
                else
                {
                    _rhythmStreak = 1;
                }
            }
            else
            {
                _rhythmStreak = 1;
            }
            _lastCaptureTime = currentTime;
        }

        public void Reset()
        {
            _lastCaptureTime  = -1f;
            _rhythmStreak     = 0;
            _rhythmCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

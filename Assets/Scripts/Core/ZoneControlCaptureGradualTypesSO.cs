using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGradualTypes", order = 566)]
    public sealed class ZoneControlCaptureGradualTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _consistentTypingsNeeded = 6;
        [SerializeField, Min(1)] private int _castFailuresPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerGradualStep     = 5230;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGradualTypesCompleted;

        private int _consistentTypings;
        private int _gradualStepCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ConsistentTypingsNeeded => _consistentTypingsNeeded;
        public int   CastFailuresPerBot      => _castFailuresPerBot;
        public int   BonusPerGradualStep     => _bonusPerGradualStep;
        public int   ConsistentTypings       => _consistentTypings;
        public int   GradualStepCount        => _gradualStepCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float ConsistentTypingProgress => _consistentTypingsNeeded > 0
            ? Mathf.Clamp01(_consistentTypings / (float)_consistentTypingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _consistentTypings = Mathf.Min(_consistentTypings + 1, _consistentTypingsNeeded);
            if (_consistentTypings >= _consistentTypingsNeeded)
            {
                int bonus = _bonusPerGradualStep;
                _gradualStepCount++;
                _totalBonusAwarded  += bonus;
                _consistentTypings   = 0;
                _onGradualTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _consistentTypings = Mathf.Max(0, _consistentTypings - _castFailuresPerBot);
        }

        public void Reset()
        {
            _consistentTypings = 0;
            _gradualStepCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

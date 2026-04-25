using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureResolutionOfSingularities", order = 517)]
    public sealed class ZoneControlCaptureResolutionOfSingularitiesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _blowupsNeeded               = 7;
        [SerializeField, Min(1)] private int _exceptionalDivisorsPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerResolution          = 4495;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onResolutionAchieved;

        private int _blowups;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BlowupsNeeded              => _blowupsNeeded;
        public int   ExceptionalDivisorsPerBot  => _exceptionalDivisorsPerBot;
        public int   BonusPerResolution         => _bonusPerResolution;
        public int   Blowups                    => _blowups;
        public int   ResolutionCount            => _resolutionCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float BlowupProgress             => _blowupsNeeded > 0
            ? Mathf.Clamp01(_blowups / (float)_blowupsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _blowups = Mathf.Min(_blowups + 1, _blowupsNeeded);
            if (_blowups >= _blowupsNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded += bonus;
                _blowups            = 0;
                _onResolutionAchieved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _blowups = Mathf.Max(0, _blowups - _exceptionalDivisorsPerBot);
        }

        public void Reset()
        {
            _blowups           = 0;
            _resolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

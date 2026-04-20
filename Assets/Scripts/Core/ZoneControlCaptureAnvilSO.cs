using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAnvil", order = 213)]
    public sealed class ZoneControlCaptureAnvilSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _strikesPerBlow = 3;
        [SerializeField, Min(1)] private int _maxBlows       = 5;
        [SerializeField, Min(0)] private int _bonusPerBlow   = 80;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBlow;

        private int _strikeCount;
        private int _blowCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StrikesPerBlow    => _strikesPerBlow;
        public int   MaxBlows          => _maxBlows;
        public int   BonusPerBlow      => _bonusPerBlow;
        public int   StrikeCount       => _strikeCount;
        public int   BlowCount         => _blowCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float AnvilProgress     => _strikesPerBlow > 0
            ? Mathf.Clamp01(_strikeCount / (float)_strikesPerBlow)
            : 0f;
        public float BlowProgress      => _maxBlows > 0
            ? Mathf.Clamp01(_blowCount / (float)_maxBlows)
            : 0f;

        public int RecordPlayerCapture()
        {
            _strikeCount++;
            if (_strikeCount >= _strikesPerBlow)
            {
                _strikeCount = 0;
                _blowCount   = Mathf.Min(_blowCount + 1, _maxBlows);
                int bonus    = _bonusPerBlow * _blowCount;
                _totalBonusAwarded += bonus;
                _onBlow?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _strikeCount = 0;
            _blowCount   = 0;
        }

        public void Reset()
        {
            _strikeCount       = 0;
            _blowCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

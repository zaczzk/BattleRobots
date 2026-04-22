using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBinary", order = 356)]
    public sealed class ZoneControlCaptureBinarySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bitsNeeded       = 6;
        [SerializeField, Min(1)] private int _flipPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerPattern  = 2080;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPatternMatched;

        private int _bits;
        private int _patternCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BitsNeeded        => _bitsNeeded;
        public int   FlipPerBot        => _flipPerBot;
        public int   BonusPerPattern   => _bonusPerPattern;
        public int   Bits              => _bits;
        public int   PatternCount      => _patternCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BitProgress       => _bitsNeeded > 0
            ? Mathf.Clamp01(_bits / (float)_bitsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bits = Mathf.Min(_bits + 1, _bitsNeeded);
            if (_bits >= _bitsNeeded)
            {
                int bonus = _bonusPerPattern;
                _patternCount++;
                _totalBonusAwarded += bonus;
                _bits               = 0;
                _onPatternMatched?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bits = Mathf.Max(0, _bits - _flipPerBot);
        }

        public void Reset()
        {
            _bits              = 0;
            _patternCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

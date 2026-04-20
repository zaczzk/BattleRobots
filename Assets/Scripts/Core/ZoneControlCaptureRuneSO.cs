using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRune", order = 215)]
    public sealed class ZoneControlCaptureRuneSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _runeValue  = 20;
        [SerializeField, Min(1)] private int _maxRunes   = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRuneScribed;

        private int _currentRunes;
        private int _runeCaptures;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RuneValue         => _runeValue;
        public int   MaxRunes          => _maxRunes;
        public int   CurrentRunes      => _currentRunes;
        public int   RuneCaptures      => _runeCaptures;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float RuneProgress      => _maxRunes > 0
            ? Mathf.Clamp01(_currentRunes / (float)_maxRunes)
            : 0f;

        public int RecordPlayerCapture()
        {
            int prev = _currentRunes;
            if (_currentRunes < _maxRunes)
                _currentRunes++;
            if (_currentRunes > prev)
                _onRuneScribed?.Raise();
            int bonus = _runeValue * _currentRunes;
            _totalBonusAwarded += bonus;
            _runeCaptures++;
            return bonus;
        }

        public void RecordBotCapture()
        {
            _currentRunes = Mathf.Max(0, _currentRunes - 1);
        }

        public void Reset()
        {
            _currentRunes      = 0;
            _runeCaptures      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

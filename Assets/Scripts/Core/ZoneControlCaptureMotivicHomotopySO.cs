using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMotivicHomotopy", order = 504)]
    public sealed class ZoneControlCaptureMotivicHomotopySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _a1LocalizationsNeeded    = 5;
        [SerializeField, Min(1)] private int _nonA1ObstructionsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerContraction      = 4300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMotivicHomotopyContracted;

        private int _a1Localizations;
        private int _contractionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   A1LocalizationsNeeded   => _a1LocalizationsNeeded;
        public int   NonA1ObstructionsPerBot => _nonA1ObstructionsPerBot;
        public int   BonusPerContraction     => _bonusPerContraction;
        public int   A1Localizations         => _a1Localizations;
        public int   ContractionCount        => _contractionCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float A1LocalizationProgress => _a1LocalizationsNeeded > 0
            ? Mathf.Clamp01(_a1Localizations / (float)_a1LocalizationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _a1Localizations = Mathf.Min(_a1Localizations + 1, _a1LocalizationsNeeded);
            if (_a1Localizations >= _a1LocalizationsNeeded)
            {
                int bonus = _bonusPerContraction;
                _contractionCount++;
                _totalBonusAwarded += bonus;
                _a1Localizations    = 0;
                _onMotivicHomotopyContracted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _a1Localizations = Mathf.Max(0, _a1Localizations - _nonA1ObstructionsPerBot);
        }

        public void Reset()
        {
            _a1Localizations   = 0;
            _contractionCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

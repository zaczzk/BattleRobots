using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSheave", order = 448)]
    public sealed class ZoneControlCaptureSheaveSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _sectionsNeeded = 5;
        [SerializeField, Min(1)] private int _restrictPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerGluing = 3445;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSheaveGlued;

        private int _sections;
        private int _gluingCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SectionsNeeded    => _sectionsNeeded;
        public int   RestrictPerBot    => _restrictPerBot;
        public int   BonusPerGluing    => _bonusPerGluing;
        public int   Sections          => _sections;
        public int   GluingCount       => _gluingCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SheaveProgress    => _sectionsNeeded > 0
            ? Mathf.Clamp01(_sections / (float)_sectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sections = Mathf.Min(_sections + 1, _sectionsNeeded);
            if (_sections >= _sectionsNeeded)
            {
                int bonus = _bonusPerGluing;
                _gluingCount++;
                _totalBonusAwarded += bonus;
                _sections           = 0;
                _onSheaveGlued?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sections = Mathf.Max(0, _sections - _restrictPerBot);
        }

        public void Reset()
        {
            _sections          = 0;
            _gluingCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

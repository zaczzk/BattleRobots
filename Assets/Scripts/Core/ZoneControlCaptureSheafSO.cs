using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSheaf", order = 385)]
    public sealed class ZoneControlCaptureSheafSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _sectionsNeeded = 7;
        [SerializeField, Min(1)] private int _tearPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerGlue   = 2515;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSheafGlued;

        private int _sections;
        private int _glueCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SectionsNeeded    => _sectionsNeeded;
        public int   TearPerBot        => _tearPerBot;
        public int   BonusPerGlue      => _bonusPerGlue;
        public int   Sections          => _sections;
        public int   GlueCount         => _glueCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SectionProgress   => _sectionsNeeded > 0
            ? Mathf.Clamp01(_sections / (float)_sectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sections = Mathf.Min(_sections + 1, _sectionsNeeded);
            if (_sections >= _sectionsNeeded)
            {
                int bonus = _bonusPerGlue;
                _glueCount++;
                _totalBonusAwarded += bonus;
                _sections           = 0;
                _onSheafGlued?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sections = Mathf.Max(0, _sections - _tearPerBot);
        }

        public void Reset()
        {
            _sections          = 0;
            _glueCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

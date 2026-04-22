using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMap", order = 359)]
    public sealed class ZoneControlCaptureMapSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _mappingsNeeded = 5;
        [SerializeField, Min(1)] private int _unmapPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerMap    = 2125;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMapBuilt;

        private int _mappings;
        private int _mapCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MappingsNeeded    => _mappingsNeeded;
        public int   UnmapPerBot       => _unmapPerBot;
        public int   BonusPerMap       => _bonusPerMap;
        public int   Mappings          => _mappings;
        public int   MapCount          => _mapCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MappingProgress   => _mappingsNeeded > 0
            ? Mathf.Clamp01(_mappings / (float)_mappingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _mappings = Mathf.Min(_mappings + 1, _mappingsNeeded);
            if (_mappings >= _mappingsNeeded)
            {
                int bonus = _bonusPerMap;
                _mapCount++;
                _totalBonusAwarded += bonus;
                _mappings           = 0;
                _onMapBuilt?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _mappings = Mathf.Max(0, _mappings - _unmapPerBot);
        }

        public void Reset()
        {
            _mappings          = 0;
            _mapCount          = 0;
            _totalBonusAwarded = 0;
        }
    }
}

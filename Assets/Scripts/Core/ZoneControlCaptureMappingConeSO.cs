using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMappingCone", order = 469)]
    public sealed class ZoneControlCaptureMappingConeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chainMapsNeeded = 5;
        [SerializeField, Min(1)] private int _breakPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerCone    = 3775;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMappingConeConed;

        private int _chainMaps;
        private int _coneCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChainMapsNeeded   => _chainMapsNeeded;
        public int   BreakPerBot       => _breakPerBot;
        public int   BonusPerCone      => _bonusPerCone;
        public int   ChainMaps         => _chainMaps;
        public int   ConeCount         => _coneCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ChainMapProgress  => _chainMapsNeeded > 0
            ? Mathf.Clamp01(_chainMaps / (float)_chainMapsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _chainMaps = Mathf.Min(_chainMaps + 1, _chainMapsNeeded);
            if (_chainMaps >= _chainMapsNeeded)
            {
                int bonus = _bonusPerCone;
                _coneCount++;
                _totalBonusAwarded += bonus;
                _chainMaps          = 0;
                _onMappingConeConed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _chainMaps = Mathf.Max(0, _chainMaps - _breakPerBot);
        }

        public void Reset()
        {
            _chainMaps         = 0;
            _coneCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

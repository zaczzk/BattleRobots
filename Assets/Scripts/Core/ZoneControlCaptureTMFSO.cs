using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTMF", order = 491)]
    public sealed class ZoneControlCaptureTMFSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _levelStructuresNeeded = 6;
        [SerializeField, Min(1)] private int _cuspsPerBot           = 1;
        [SerializeField, Min(0)] private int _bonusPerResolution    = 4105;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTMFResolved;

        private int _levelStructures;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LevelStructuresNeeded => _levelStructuresNeeded;
        public int   CuspsPerBot           => _cuspsPerBot;
        public int   BonusPerResolution    => _bonusPerResolution;
        public int   LevelStructures       => _levelStructures;
        public int   ResolutionCount       => _resolutionCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float LevelStructureProgress => _levelStructuresNeeded > 0
            ? Mathf.Clamp01(_levelStructures / (float)_levelStructuresNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _levelStructures = Mathf.Min(_levelStructures + 1, _levelStructuresNeeded);
            if (_levelStructures >= _levelStructuresNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded += bonus;
                _levelStructures    = 0;
                _onTMFResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _levelStructures = Mathf.Max(0, _levelStructures - _cuspsPerBot);
        }

        public void Reset()
        {
            _levelStructures   = 0;
            _resolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

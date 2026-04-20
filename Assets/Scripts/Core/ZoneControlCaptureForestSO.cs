using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureForest", order = 255)]
    public sealed class ZoneControlCaptureForestSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _treesNeeded       = 5;
        [SerializeField, Min(1)] private int _clearPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerFlourish  = 565;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onForestFlourished;

        private int _trees;
        private int _flourishCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TreesNeeded        => _treesNeeded;
        public int   ClearPerBot        => _clearPerBot;
        public int   BonusPerFlourish   => _bonusPerFlourish;
        public int   Trees              => _trees;
        public int   FlourishCount      => _flourishCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float TreeProgress       => _treesNeeded > 0
            ? Mathf.Clamp01(_trees / (float)_treesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _trees = Mathf.Min(_trees + 1, _treesNeeded);
            if (_trees >= _treesNeeded)
            {
                int bonus = _bonusPerFlourish;
                _flourishCount++;
                _totalBonusAwarded += bonus;
                _trees              = 0;
                _onForestFlourished?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _trees = Mathf.Max(0, _trees - _clearPerBot);
        }

        public void Reset()
        {
            _trees             = 0;
            _flourishCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

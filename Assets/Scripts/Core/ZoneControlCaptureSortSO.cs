using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSort", order = 358)]
    public sealed class ZoneControlCaptureSortSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _comparisonsNeeded = 5;
        [SerializeField, Min(1)] private int _swapPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerSort      = 2110;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSortComplete;

        private int _comparisons;
        private int _sortCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComparisonsNeeded   => _comparisonsNeeded;
        public int   SwapPerBot          => _swapPerBot;
        public int   BonusPerSort        => _bonusPerSort;
        public int   Comparisons         => _comparisons;
        public int   SortCount           => _sortCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ComparisonProgress  => _comparisonsNeeded > 0
            ? Mathf.Clamp01(_comparisons / (float)_comparisonsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _comparisons = Mathf.Min(_comparisons + 1, _comparisonsNeeded);
            if (_comparisons >= _comparisonsNeeded)
            {
                int bonus = _bonusPerSort;
                _sortCount++;
                _totalBonusAwarded += bonus;
                _comparisons        = 0;
                _onSortComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _comparisons = Mathf.Max(0, _comparisons - _swapPerBot);
        }

        public void Reset()
        {
            _comparisons       = 0;
            _sortCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

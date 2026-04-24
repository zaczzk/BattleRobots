using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFilterBase", order = 443)]
    public sealed class ZoneControlCaptureFilterBaseSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded = 5;
        [SerializeField, Min(1)] private int _coarsenPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerRefine = 3385;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFilterRefined;

        private int _elements;
        private int _refineCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded    => _elementsNeeded;
        public int   CoarsenPerBot     => _coarsenPerBot;
        public int   BonusPerRefine    => _bonusPerRefine;
        public int   Elements          => _elements;
        public int   RefineCount       => _refineCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float FilterProgress    => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerRefine;
                _refineCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onFilterRefined?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _coarsenPerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _refineCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

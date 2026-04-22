using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSemigroup", order = 369)]
    public sealed class ZoneControlCaptureSemigroupSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded  = 5;
        [SerializeField, Min(1)] private int _splitPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerCombine = 2275;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSemigroupCombined;

        private int _elements;
        private int _combineCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded     => _elementsNeeded;
        public int   SplitPerBot        => _splitPerBot;
        public int   BonusPerCombine    => _bonusPerCombine;
        public int   Elements           => _elements;
        public int   CombineCount       => _combineCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ElementProgress    => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerCombine;
                _combineCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onSemigroupCombined?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _splitPerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _combineCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

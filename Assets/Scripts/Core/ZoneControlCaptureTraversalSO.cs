using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTraversal", order = 376)]
    public sealed class ZoneControlCaptureTraversalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded   = 6;
        [SerializeField, Min(1)] private int _skipPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerTraverse = 2380;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTraversalComplete;

        private int _elements;
        private int _traverseCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded     => _elementsNeeded;
        public int   SkipPerBot         => _skipPerBot;
        public int   BonusPerTraverse   => _bonusPerTraverse;
        public int   Elements           => _elements;
        public int   TraverseCount      => _traverseCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ElementProgress    => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerTraverse;
                _traverseCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onTraversalComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _skipPerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _traverseCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureReduce", order = 360)]
    public sealed class ZoneControlCaptureReduceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded  = 5;
        [SerializeField, Min(1)] private int _expandPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerReduce  = 2140;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onReduceComplete;

        private int _elements;
        private int _reduceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded    => _elementsNeeded;
        public int   ExpandPerBot      => _expandPerBot;
        public int   BonusPerReduce    => _bonusPerReduce;
        public int   Elements          => _elements;
        public int   ReduceCount       => _reduceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ElementProgress   => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerReduce;
                _reduceCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onReduceComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _expandPerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _reduceCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

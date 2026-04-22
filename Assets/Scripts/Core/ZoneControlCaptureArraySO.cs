using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureArray", order = 353)]
    public sealed class ZoneControlCaptureArraySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded = 7;
        [SerializeField, Min(1)] private int _clearPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerFill   = 2035;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onArrayFilled;

        private int _elements;
        private int _fillCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded    => _elementsNeeded;
        public int   ClearPerBot       => _clearPerBot;
        public int   BonusPerFill      => _bonusPerFill;
        public int   Elements          => _elements;
        public int   FillCount         => _fillCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ElementProgress   => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerFill;
                _fillCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onArrayFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _clearPerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _fillCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFunctor", order = 366)]
    public sealed class ZoneControlCaptureFunctorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded = 7;
        [SerializeField, Min(1)] private int _removePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerLift   = 2230;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFunctorLifted;

        private int _elements;
        private int _liftCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded     => _elementsNeeded;
        public int   RemovePerBot       => _removePerBot;
        public int   BonusPerLift       => _bonusPerLift;
        public int   Elements           => _elements;
        public int   LiftCount          => _liftCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ElementProgress    => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerLift;
                _liftCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onFunctorLifted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _removePerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _liftCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

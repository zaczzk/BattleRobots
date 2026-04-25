using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureArakelovTheory", order = 510)]
    public sealed class ZoneControlCaptureArakelovTheorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _arithmeticDivisorsNeeded = 5;
        [SerializeField, Min(1)] private int _badReductionsPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerArakelovIntersection = 4390;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onArakelovTheoryIntersected;

        private int _arithmeticDivisors;
        private int _arakelovIntersectionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ArithmeticDivisorsNeeded      => _arithmeticDivisorsNeeded;
        public int   BadReductionsPerBot            => _badReductionsPerBot;
        public int   BonusPerArakelovIntersection   => _bonusPerArakelovIntersection;
        public int   ArithmeticDivisors             => _arithmeticDivisors;
        public int   ArakelovIntersectionCount      => _arakelovIntersectionCount;
        public int   TotalBonusAwarded              => _totalBonusAwarded;
        public float ArithmeticDivisorProgress => _arithmeticDivisorsNeeded > 0
            ? Mathf.Clamp01(_arithmeticDivisors / (float)_arithmeticDivisorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _arithmeticDivisors = Mathf.Min(_arithmeticDivisors + 1, _arithmeticDivisorsNeeded);
            if (_arithmeticDivisors >= _arithmeticDivisorsNeeded)
            {
                int bonus = _bonusPerArakelovIntersection;
                _arakelovIntersectionCount++;
                _totalBonusAwarded  += bonus;
                _arithmeticDivisors  = 0;
                _onArakelovTheoryIntersected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _arithmeticDivisors = Mathf.Max(0, _arithmeticDivisors - _badReductionsPerBot);
        }

        public void Reset()
        {
            _arithmeticDivisors        = 0;
            _arakelovIntersectionCount = 0;
            _totalBonusAwarded         = 0;
        }
    }
}

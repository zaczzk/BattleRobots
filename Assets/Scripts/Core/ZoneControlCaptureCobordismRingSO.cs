using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCobordismRing", order = 487)]
    public sealed class ZoneControlCaptureCobordismRingSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _generatorsNeeded        = 6;
        [SerializeField, Min(1)] private int _relationsPerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerMultiplication  = 4045;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCobordismRingMultiplied;

        private int _generators;
        private int _multiplicationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GeneratorsNeeded       => _generatorsNeeded;
        public int   RelationsPerBot        => _relationsPerBot;
        public int   BonusPerMultiplication => _bonusPerMultiplication;
        public int   Generators             => _generators;
        public int   MultiplicationCount    => _multiplicationCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float GeneratorProgress      => _generatorsNeeded > 0
            ? Mathf.Clamp01(_generators / (float)_generatorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _generators = Mathf.Min(_generators + 1, _generatorsNeeded);
            if (_generators >= _generatorsNeeded)
            {
                int bonus = _bonusPerMultiplication;
                _multiplicationCount++;
                _totalBonusAwarded += bonus;
                _generators         = 0;
                _onCobordismRingMultiplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _generators = Mathf.Max(0, _generators - _relationsPerBot);
        }

        public void Reset()
        {
            _generators          = 0;
            _multiplicationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

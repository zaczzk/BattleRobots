using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureContinuumHypothesis", order = 525)]
    public sealed class ZoneControlCaptureContinuumHypothesisSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cardinalWitnessesNeeded  = 7;
        [SerializeField, Min(1)] private int _forcingObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerCardinalClass    = 4615;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCardinalClassified;

        private int _cardinalWitnesses;
        private int _cardinalClassCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CardinalWitnessesNeeded  => _cardinalWitnessesNeeded;
        public int   ForcingObstructionsPerBot => _forcingObstructionsPerBot;
        public int   BonusPerCardinalClass    => _bonusPerCardinalClass;
        public int   CardinalWitnesses        => _cardinalWitnesses;
        public int   CardinalClassCount       => _cardinalClassCount;
        public int   TotalBonusAwarded        => _totalBonusAwarded;
        public float CardinalWitnessProgress  => _cardinalWitnessesNeeded > 0
            ? Mathf.Clamp01(_cardinalWitnesses / (float)_cardinalWitnessesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cardinalWitnesses = Mathf.Min(_cardinalWitnesses + 1, _cardinalWitnessesNeeded);
            if (_cardinalWitnesses >= _cardinalWitnessesNeeded)
            {
                int bonus = _bonusPerCardinalClass;
                _cardinalClassCount++;
                _totalBonusAwarded += bonus;
                _cardinalWitnesses  = 0;
                _onCardinalClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cardinalWitnesses = Mathf.Max(0, _cardinalWitnesses - _forcingObstructionsPerBot);
        }

        public void Reset()
        {
            _cardinalWitnesses  = 0;
            _cardinalClassCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

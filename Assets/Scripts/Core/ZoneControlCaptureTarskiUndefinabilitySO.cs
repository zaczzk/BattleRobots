using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTarskiUndefinability", order = 544)]
    public sealed class ZoneControlCaptureTarskiUndefinabilitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _truthPredicatesNeeded                = 6;
        [SerializeField, Min(1)] private int _selfReferentialContradictionsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerUndefinability               = 4900;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTarskiUndefinabilityReached;

        private int _truthPredicates;
        private int _undefinabilityCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TruthPredicatesNeeded               => _truthPredicatesNeeded;
        public int   SelfReferentialContradictionsPerBot => _selfReferentialContradictionsPerBot;
        public int   BonusPerUndefinability              => _bonusPerUndefinability;
        public int   TruthPredicates                     => _truthPredicates;
        public int   UndefinabilityCount                 => _undefinabilityCount;
        public int   TotalBonusAwarded                   => _totalBonusAwarded;
        public float TruthPredicateProgress              => _truthPredicatesNeeded > 0
            ? Mathf.Clamp01(_truthPredicates / (float)_truthPredicatesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _truthPredicates = Mathf.Min(_truthPredicates + 1, _truthPredicatesNeeded);
            if (_truthPredicates >= _truthPredicatesNeeded)
            {
                int bonus = _bonusPerUndefinability;
                _undefinabilityCount++;
                _totalBonusAwarded += bonus;
                _truthPredicates    = 0;
                _onTarskiUndefinabilityReached?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _truthPredicates = Mathf.Max(0, _truthPredicates - _selfReferentialContradictionsPerBot);
        }

        public void Reset()
        {
            _truthPredicates     = 0;
            _undefinabilityCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

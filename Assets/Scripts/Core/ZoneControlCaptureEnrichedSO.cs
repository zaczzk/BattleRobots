using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEnriched", order = 419)]
    public sealed class ZoneControlCaptureEnrichedSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _morphismsNeeded  = 5;
        [SerializeField, Min(1)] private int _dilutionPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerEnriched = 3025;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEnrichedCategoryFormed;

        private int _morphisms;
        private int _enrichedCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MorphismsNeeded     => _morphismsNeeded;
        public int   DilutionPerBot      => _dilutionPerBot;
        public int   BonusPerEnriched    => _bonusPerEnriched;
        public int   Morphisms           => _morphisms;
        public int   EnrichedCount       => _enrichedCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float MorphismProgress    => _morphismsNeeded > 0
            ? Mathf.Clamp01(_morphisms / (float)_morphismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _morphisms = Mathf.Min(_morphisms + 1, _morphismsNeeded);
            if (_morphisms >= _morphismsNeeded)
            {
                int bonus = _bonusPerEnriched;
                _enrichedCount++;
                _totalBonusAwarded += bonus;
                _morphisms          = 0;
                _onEnrichedCategoryFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _morphisms = Mathf.Max(0, _morphisms - _dilutionPerBot);
        }

        public void Reset()
        {
            _morphisms         = 0;
            _enrichedCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

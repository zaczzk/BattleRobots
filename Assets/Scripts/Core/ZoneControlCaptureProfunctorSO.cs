using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureProfunctor", order = 372)]
    public sealed class ZoneControlCaptureProfunctorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _projectionsNeeded = 6;
        [SerializeField, Min(1)] private int _invertPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerDimap     = 2320;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onProfunctorDimapped;

        private int _projections;
        private int _dimapCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ProjectionsNeeded  => _projectionsNeeded;
        public int   InvertPerBot       => _invertPerBot;
        public int   BonusPerDimap      => _bonusPerDimap;
        public int   Projections        => _projections;
        public int   DimapCount         => _dimapCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ProjectionProgress => _projectionsNeeded > 0
            ? Mathf.Clamp01(_projections / (float)_projectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _projections = Mathf.Min(_projections + 1, _projectionsNeeded);
            if (_projections >= _projectionsNeeded)
            {
                int bonus = _bonusPerDimap;
                _dimapCount++;
                _totalBonusAwarded += bonus;
                _projections        = 0;
                _onProfunctorDimapped?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _projections = Mathf.Max(0, _projections - _invertPerBot);
        }

        public void Reset()
        {
            _projections       = 0;
            _dimapCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

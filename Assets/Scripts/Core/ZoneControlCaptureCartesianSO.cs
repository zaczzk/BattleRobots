using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCartesian", order = 426)]
    public sealed class ZoneControlCaptureCartesianSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _projectionsNeeded    = 7;
        [SerializeField, Min(1)] private int _deletePerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerDiagonalize  = 3130;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDiagonalized;

        private int _projections;
        private int _diagonalizeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ProjectionsNeeded   => _projectionsNeeded;
        public int   DeletePerBot        => _deletePerBot;
        public int   BonusPerDiagonalize => _bonusPerDiagonalize;
        public int   Projections         => _projections;
        public int   DiagonalizeCount    => _diagonalizeCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ProjectionProgress  => _projectionsNeeded > 0
            ? Mathf.Clamp01(_projections / (float)_projectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _projections = Mathf.Min(_projections + 1, _projectionsNeeded);
            if (_projections >= _projectionsNeeded)
            {
                int bonus = _bonusPerDiagonalize;
                _diagonalizeCount++;
                _totalBonusAwarded += bonus;
                _projections        = 0;
                _onDiagonalized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _projections = Mathf.Max(0, _projections - _deletePerBot);
        }

        public void Reset()
        {
            _projections       = 0;
            _diagonalizeCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

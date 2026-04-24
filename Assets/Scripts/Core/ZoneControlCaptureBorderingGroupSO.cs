using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBorderingGroup", order = 486)]
    public sealed class ZoneControlCaptureBorderingGroupSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _manifoldsNeeded          = 7;
        [SerializeField, Min(1)] private int _boundaryPerBot           = 2;
        [SerializeField, Min(0)] private int _bonusPerClassification   = 4030;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBorderingGroupClassified;

        private int _manifolds;
        private int _classificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ManifoldsNeeded        => _manifoldsNeeded;
        public int   BoundaryPerBot         => _boundaryPerBot;
        public int   BonusPerClassification => _bonusPerClassification;
        public int   Manifolds              => _manifolds;
        public int   ClassificationCount    => _classificationCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float ManifoldProgress       => _manifoldsNeeded > 0
            ? Mathf.Clamp01(_manifolds / (float)_manifoldsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _manifolds = Mathf.Min(_manifolds + 1, _manifoldsNeeded);
            if (_manifolds >= _manifoldsNeeded)
            {
                int bonus = _bonusPerClassification;
                _classificationCount++;
                _totalBonusAwarded += bonus;
                _manifolds          = 0;
                _onBorderingGroupClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _manifolds = Mathf.Max(0, _manifolds - _boundaryPerBot);
        }

        public void Reset()
        {
            _manifolds           = 0;
            _classificationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

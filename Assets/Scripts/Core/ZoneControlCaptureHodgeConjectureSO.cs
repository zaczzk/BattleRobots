using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHodgeConjecture", order = 520)]
    public sealed class ZoneControlCaptureHodgeConjectureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _hodgeCyclesNeeded             = 5;
        [SerializeField, Min(1)] private int _nonAlgebraicObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerClassification         = 4540;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHodgeConjectureClassified;

        private int _hodgeCycles;
        private int _classificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HodgeCyclesNeeded              => _hodgeCyclesNeeded;
        public int   NonAlgebraicObstructionsPerBot => _nonAlgebraicObstructionsPerBot;
        public int   BonusPerClassification         => _bonusPerClassification;
        public int   HodgeCycles                    => _hodgeCycles;
        public int   ClassificationCount            => _classificationCount;
        public int   TotalBonusAwarded              => _totalBonusAwarded;
        public float HodgeCycleProgress             => _hodgeCyclesNeeded > 0
            ? Mathf.Clamp01(_hodgeCycles / (float)_hodgeCyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _hodgeCycles = Mathf.Min(_hodgeCycles + 1, _hodgeCyclesNeeded);
            if (_hodgeCycles >= _hodgeCyclesNeeded)
            {
                int bonus = _bonusPerClassification;
                _classificationCount++;
                _totalBonusAwarded += bonus;
                _hodgeCycles        = 0;
                _onHodgeConjectureClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _hodgeCycles = Mathf.Max(0, _hodgeCycles - _nonAlgebraicObstructionsPerBot);
        }

        public void Reset()
        {
            _hodgeCycles         = 0;
            _classificationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

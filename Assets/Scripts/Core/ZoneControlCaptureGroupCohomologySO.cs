using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGroupCohomology", order = 475)]
    public sealed class ZoneControlCaptureGroupCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cocyclesNeeded         = 5;
        [SerializeField, Min(1)] private int _coboundaryPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerClassification = 3865;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGroupCohomologyClassified;

        private int _cocycles;
        private int _classifyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CocyclesNeeded        => _cocyclesNeeded;
        public int   CoboundaryPerBot      => _coboundaryPerBot;
        public int   BonusPerClassification => _bonusPerClassification;
        public int   Cocycles              => _cocycles;
        public int   ClassifyCount         => _classifyCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float CocycleProgress       => _cocyclesNeeded > 0
            ? Mathf.Clamp01(_cocycles / (float)_cocyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cocycles = Mathf.Min(_cocycles + 1, _cocyclesNeeded);
            if (_cocycles >= _cocyclesNeeded)
            {
                int bonus = _bonusPerClassification;
                _classifyCount++;
                _totalBonusAwarded += bonus;
                _cocycles           = 0;
                _onGroupCohomologyClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cocycles = Mathf.Max(0, _cocycles - _coboundaryPerBot);
        }

        public void Reset()
        {
            _cocycles          = 0;
            _classifyCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

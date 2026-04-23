using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCohomology", order = 391)]
    public sealed class ZoneControlCaptureCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cocyclesNeeded      = 5;
        [SerializeField, Min(1)] private int _boundaryPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerCohomology  = 2605;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCohomologyComputed;

        private int _cocycles;
        private int _computationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CocyclesNeeded      => _cocyclesNeeded;
        public int   BoundaryPerBot      => _boundaryPerBot;
        public int   BonusPerCohomology  => _bonusPerCohomology;
        public int   Cocycles            => _cocycles;
        public int   ComputationCount    => _computationCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float CocycleProgress     => _cocyclesNeeded > 0
            ? Mathf.Clamp01(_cocycles / (float)_cocyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cocycles = Mathf.Min(_cocycles + 1, _cocyclesNeeded);
            if (_cocycles >= _cocyclesNeeded)
            {
                int bonus = _bonusPerCohomology;
                _computationCount++;
                _totalBonusAwarded += bonus;
                _cocycles           = 0;
                _onCohomologyComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cocycles = Mathf.Max(0, _cocycles - _boundaryPerBot);
        }

        public void Reset()
        {
            _cocycles          = 0;
            _computationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

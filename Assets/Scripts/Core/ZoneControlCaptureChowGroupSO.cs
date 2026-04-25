using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChowGroup", order = 507)]
    public sealed class ZoneControlCaptureChowGroupSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _rationalCyclesNeeded        = 5;
        [SerializeField, Min(1)] private int _rationalEquivalencesPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerIntersection         = 4345;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChowGroupIntersected;

        private int _rationalCycles;
        private int _intersectionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RationalCyclesNeeded      => _rationalCyclesNeeded;
        public int   RationalEquivalencesPerBot => _rationalEquivalencesPerBot;
        public int   BonusPerIntersection       => _bonusPerIntersection;
        public int   RationalCycles             => _rationalCycles;
        public int   IntersectionCount          => _intersectionCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float RationalCycleProgress => _rationalCyclesNeeded > 0
            ? Mathf.Clamp01(_rationalCycles / (float)_rationalCyclesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rationalCycles = Mathf.Min(_rationalCycles + 1, _rationalCyclesNeeded);
            if (_rationalCycles >= _rationalCyclesNeeded)
            {
                int bonus = _bonusPerIntersection;
                _intersectionCount++;
                _totalBonusAwarded += bonus;
                _rationalCycles     = 0;
                _onChowGroupIntersected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _rationalCycles = Mathf.Max(0, _rationalCycles - _rationalEquivalencesPerBot);
        }

        public void Reset()
        {
            _rationalCycles    = 0;
            _intersectionCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

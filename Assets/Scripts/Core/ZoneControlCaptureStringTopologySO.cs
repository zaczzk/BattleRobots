using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStringTopology", order = 493)]
    public sealed class ZoneControlCaptureStringTopologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _loopsNeeded             = 7;
        [SerializeField, Min(1)] private int _nullHomotopiesPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerIntersection    = 4135;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStringTopologyIntersected;

        private int _loops;
        private int _intersectionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LoopsNeeded          => _loopsNeeded;
        public int   NullHomotopiesPerBot => _nullHomotopiesPerBot;
        public int   BonusPerIntersection => _bonusPerIntersection;
        public int   Loops                => _loops;
        public int   IntersectionCount    => _intersectionCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float LoopProgress         => _loopsNeeded > 0
            ? Mathf.Clamp01(_loops / (float)_loopsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _loops = Mathf.Min(_loops + 1, _loopsNeeded);
            if (_loops >= _loopsNeeded)
            {
                int bonus = _bonusPerIntersection;
                _intersectionCount++;
                _totalBonusAwarded += bonus;
                _loops              = 0;
                _onStringTopologyIntersected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _loops = Mathf.Max(0, _loops - _nullHomotopiesPerBot);
        }

        public void Reset()
        {
            _loops             = 0;
            _intersectionCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

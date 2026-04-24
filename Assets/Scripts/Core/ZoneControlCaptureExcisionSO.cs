using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureExcision", order = 482)]
    public sealed class ZoneControlCaptureExcisionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _subsetsNeeded      = 7;
        [SerializeField, Min(1)] private int _reintroducePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerExcision   = 3970;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onExcisionComplete;

        private int _subsets;
        private int _excisionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SubsetsNeeded     => _subsetsNeeded;
        public int   ReintroducePerBot => _reintroducePerBot;
        public int   BonusPerExcision  => _bonusPerExcision;
        public int   Subsets           => _subsets;
        public int   ExcisionCount     => _excisionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SubsetProgress    => _subsetsNeeded > 0
            ? Mathf.Clamp01(_subsets / (float)_subsetsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _subsets = Mathf.Min(_subsets + 1, _subsetsNeeded);
            if (_subsets >= _subsetsNeeded)
            {
                int bonus = _bonusPerExcision;
                _excisionCount++;
                _totalBonusAwarded += bonus;
                _subsets            = 0;
                _onExcisionComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _subsets = Mathf.Max(0, _subsets - _reintroducePerBot);
        }

        public void Reset()
        {
            _subsets           = 0;
            _excisionCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

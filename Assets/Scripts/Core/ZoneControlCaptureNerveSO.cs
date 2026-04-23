using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNerve", order = 389)]
    public sealed class ZoneControlCaptureNerveSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _simplicesNeeded      = 7;
        [SerializeField, Min(1)] private int _collapsePerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerRealization  = 2575;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNerveRealized;

        private int _simplices;
        private int _realizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SimplicesNeeded    => _simplicesNeeded;
        public int   CollapsePerBot     => _collapsePerBot;
        public int   BonusPerRealization => _bonusPerRealization;
        public int   Simplices          => _simplices;
        public int   RealizationCount   => _realizationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float SimplexProgress    => _simplicesNeeded > 0
            ? Mathf.Clamp01(_simplices / (float)_simplicesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _simplices = Mathf.Min(_simplices + 1, _simplicesNeeded);
            if (_simplices >= _simplicesNeeded)
            {
                int bonus = _bonusPerRealization;
                _realizationCount++;
                _totalBonusAwarded += bonus;
                _simplices          = 0;
                _onNerveRealized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _simplices = Mathf.Max(0, _simplices - _collapsePerBot);
        }

        public void Reset()
        {
            _simplices         = 0;
            _realizationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

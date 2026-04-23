using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFiltration", order = 397)]
    public sealed class ZoneControlCaptureFiltrationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _levelsNeeded       = 7;
        [SerializeField, Min(1)] private int _collapsePerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerFiltration = 2695;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFiltrationAscended;

        private int _levels;
        private int _filtrationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LevelsNeeded        => _levelsNeeded;
        public int   CollapsePerBot      => _collapsePerBot;
        public int   BonusPerFiltration  => _bonusPerFiltration;
        public int   Levels              => _levels;
        public int   FiltrationCount     => _filtrationCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float LevelProgress       => _levelsNeeded > 0
            ? Mathf.Clamp01(_levels / (float)_levelsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _levels = Mathf.Min(_levels + 1, _levelsNeeded);
            if (_levels >= _levelsNeeded)
            {
                int bonus = _bonusPerFiltration;
                _filtrationCount++;
                _totalBonusAwarded += bonus;
                _levels             = 0;
                _onFiltrationAscended?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _levels = Mathf.Max(0, _levels - _collapsePerBot);
        }

        public void Reset()
        {
            _levels            = 0;
            _filtrationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

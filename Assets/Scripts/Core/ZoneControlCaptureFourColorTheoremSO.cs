using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFourColorTheorem", order = 530)]
    public sealed class ZoneControlCaptureFourColorTheoremSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _reducibleConfigsNeeded  = 5;
        [SerializeField, Min(1)] private int _unavoidableSetsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerColoring        = 4690;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFourColorTheoremColored;

        private int _reducibleConfigs;
        private int _coloringCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ReducibleConfigsNeeded => _reducibleConfigsNeeded;
        public int   UnavoidableSetsPerBot  => _unavoidableSetsPerBot;
        public int   BonusPerColoring       => _bonusPerColoring;
        public int   ReducibleConfigs       => _reducibleConfigs;
        public int   ColoringCount          => _coloringCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float ReducibleConfigProgress => _reducibleConfigsNeeded > 0
            ? Mathf.Clamp01(_reducibleConfigs / (float)_reducibleConfigsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _reducibleConfigs = Mathf.Min(_reducibleConfigs + 1, _reducibleConfigsNeeded);
            if (_reducibleConfigs >= _reducibleConfigsNeeded)
            {
                int bonus = _bonusPerColoring;
                _coloringCount++;
                _totalBonusAwarded += bonus;
                _reducibleConfigs   = 0;
                _onFourColorTheoremColored?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _reducibleConfigs = Mathf.Max(0, _reducibleConfigs - _unavoidableSetsPerBot);
        }

        public void Reset()
        {
            _reducibleConfigs  = 0;
            _coloringCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

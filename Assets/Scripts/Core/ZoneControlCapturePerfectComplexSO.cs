using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePerfectComplex", order = 503)]
    public sealed class ZoneControlCapturePerfectComplexSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _perfectModulesNeeded   = 6;
        [SerializeField, Min(1)] private int _quasiIsoFailuresPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerResolution     = 4285;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPerfectComplexResolved;

        private int _perfectModules;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PerfectModulesNeeded   => _perfectModulesNeeded;
        public int   QuasiIsoFailuresPerBot => _quasiIsoFailuresPerBot;
        public int   BonusPerResolution     => _bonusPerResolution;
        public int   PerfectModules         => _perfectModules;
        public int   ResolutionCount        => _resolutionCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float PerfectModuleProgress => _perfectModulesNeeded > 0
            ? Mathf.Clamp01(_perfectModules / (float)_perfectModulesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _perfectModules = Mathf.Min(_perfectModules + 1, _perfectModulesNeeded);
            if (_perfectModules >= _perfectModulesNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded += bonus;
                _perfectModules     = 0;
                _onPerfectComplexResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _perfectModules = Mathf.Max(0, _perfectModules - _quasiIsoFailuresPerBot);
        }

        public void Reset()
        {
            _perfectModules    = 0;
            _resolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

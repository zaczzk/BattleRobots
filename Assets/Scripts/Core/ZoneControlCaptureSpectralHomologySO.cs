using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpectralHomology", order = 484)]
    public sealed class ZoneControlCaptureSpectralHomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stemsNeeded          = 6;
        [SerializeField, Min(1)] private int _differentialPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerConvergence  = 4000;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpectralHomologyConverged;

        private int _stems;
        private int _convergenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StemsNeeded        => _stemsNeeded;
        public int   DifferentialPerBot => _differentialPerBot;
        public int   BonusPerConvergence => _bonusPerConvergence;
        public int   Stems              => _stems;
        public int   ConvergenceCount   => _convergenceCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float StemProgress       => _stemsNeeded > 0
            ? Mathf.Clamp01(_stems / (float)_stemsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stems = Mathf.Min(_stems + 1, _stemsNeeded);
            if (_stems >= _stemsNeeded)
            {
                int bonus = _bonusPerConvergence;
                _convergenceCount++;
                _totalBonusAwarded += bonus;
                _stems              = 0;
                _onSpectralHomologyConverged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stems = Mathf.Max(0, _stems - _differentialPerBot);
        }

        public void Reset()
        {
            _stems             = 0;
            _convergenceCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

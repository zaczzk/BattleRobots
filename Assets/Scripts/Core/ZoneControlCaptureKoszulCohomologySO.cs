using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureKoszulCohomology", order = 480)]
    public sealed class ZoneControlCaptureKoszulCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _complexesNeeded    = 6;
        [SerializeField, Min(1)] private int _syzygyPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerResolution = 3940;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onKoszulCohomologyResolved;

        private int _complexes;
        private int _resolveCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComplexesNeeded    => _complexesNeeded;
        public int   SyzygyPerBot       => _syzygyPerBot;
        public int   BonusPerResolution => _bonusPerResolution;
        public int   Complexes          => _complexes;
        public int   ResolveCount       => _resolveCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ComplexProgress    => _complexesNeeded > 0
            ? Mathf.Clamp01(_complexes / (float)_complexesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _complexes = Mathf.Min(_complexes + 1, _complexesNeeded);
            if (_complexes >= _complexesNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolveCount++;
                _totalBonusAwarded += bonus;
                _complexes          = 0;
                _onKoszulCohomologyResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _complexes = Mathf.Max(0, _complexes - _syzygyPerBot);
        }

        public void Reset()
        {
            _complexes         = 0;
            _resolveCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

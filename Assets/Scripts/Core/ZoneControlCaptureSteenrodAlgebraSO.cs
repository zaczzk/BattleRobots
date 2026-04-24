using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSteenrodAlgebra", order = 485)]
    public sealed class ZoneControlCaptureSteenrodAlgebraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _sqOpsNeeded          = 5;
        [SerializeField, Min(1)] private int _instabilityPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerApplication  = 4015;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSteenrodAlgebraApplied;

        private int _sqOps;
        private int _applicationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SqOpsNeeded        => _sqOpsNeeded;
        public int   InstabilityPerBot  => _instabilityPerBot;
        public int   BonusPerApplication => _bonusPerApplication;
        public int   SqOps              => _sqOps;
        public int   ApplicationCount   => _applicationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float SqOpProgress       => _sqOpsNeeded > 0
            ? Mathf.Clamp01(_sqOps / (float)_sqOpsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sqOps = Mathf.Min(_sqOps + 1, _sqOpsNeeded);
            if (_sqOps >= _sqOpsNeeded)
            {
                int bonus = _bonusPerApplication;
                _applicationCount++;
                _totalBonusAwarded += bonus;
                _sqOps              = 0;
                _onSteenrodAlgebraApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sqOps = Mathf.Max(0, _sqOps - _instabilityPerBot);
        }

        public void Reset()
        {
            _sqOps             = 0;
            _applicationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

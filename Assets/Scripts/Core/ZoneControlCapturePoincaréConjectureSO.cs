using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePoincaréConjecture", order = 532)]
    public sealed class ZoneControlCapturePoincaréConjectureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ricciFlowStepsNeeded = 5;
        [SerializeField, Min(1)] private int _singularitiesPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerProof        = 4720;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPoincaréConjectureProved;

        private int _ricciFlowSteps;
        private int _proofCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RicciFlowStepsNeeded  => _ricciFlowStepsNeeded;
        public int   SingularitiesPerBot   => _singularitiesPerBot;
        public int   BonusPerProof         => _bonusPerProof;
        public int   RicciFlowSteps        => _ricciFlowSteps;
        public int   ProofCount            => _proofCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float RicciFlowStepProgress => _ricciFlowStepsNeeded > 0
            ? Mathf.Clamp01(_ricciFlowSteps / (float)_ricciFlowStepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ricciFlowSteps = Mathf.Min(_ricciFlowSteps + 1, _ricciFlowStepsNeeded);
            if (_ricciFlowSteps >= _ricciFlowStepsNeeded)
            {
                int bonus = _bonusPerProof;
                _proofCount++;
                _totalBonusAwarded += bonus;
                _ricciFlowSteps     = 0;
                _onPoincaréConjectureProved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ricciFlowSteps = Mathf.Max(0, _ricciFlowSteps - _singularitiesPerBot);
        }

        public void Reset()
        {
            _ricciFlowSteps    = 0;
            _proofCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

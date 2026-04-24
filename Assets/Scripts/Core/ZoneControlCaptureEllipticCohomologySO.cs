using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEllipticCohomology", order = 490)]
    public sealed class ZoneControlCaptureEllipticCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _modularFormsNeeded  = 6;
        [SerializeField, Min(1)] private int _cuspsPerBot         = 1;
        [SerializeField, Min(0)] private int _bonusPerComputation = 4090;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEllipticCohomologyComputed;

        private int _modularForms;
        private int _computationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ModularFormsNeeded  => _modularFormsNeeded;
        public int   CuspsPerBot         => _cuspsPerBot;
        public int   BonusPerComputation => _bonusPerComputation;
        public int   ModularForms        => _modularForms;
        public int   ComputationCount    => _computationCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ModularFormProgress => _modularFormsNeeded > 0
            ? Mathf.Clamp01(_modularForms / (float)_modularFormsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _modularForms = Mathf.Min(_modularForms + 1, _modularFormsNeeded);
            if (_modularForms >= _modularFormsNeeded)
            {
                int bonus = _bonusPerComputation;
                _computationCount++;
                _totalBonusAwarded += bonus;
                _modularForms       = 0;
                _onEllipticCohomologyComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _modularForms = Mathf.Max(0, _modularForms - _cuspsPerBot);
        }

        public void Reset()
        {
            _modularForms      = 0;
            _computationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

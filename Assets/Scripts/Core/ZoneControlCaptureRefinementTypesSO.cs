using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRefinementTypes", order = 562)]
    public sealed class ZoneControlCaptureRefinementTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _verifiedRefinementsNeeded    = 6;
        [SerializeField, Min(1)] private int _predicateFalsificationsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerRefinement           = 5170;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRefinementTypesCompleted;

        private int _verifiedRefinements;
        private int _refinementCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VerifiedRefinementsNeeded     => _verifiedRefinementsNeeded;
        public int   PredicateFalsificationsPerBot => _predicateFalsificationsPerBot;
        public int   BonusPerRefinement            => _bonusPerRefinement;
        public int   VerifiedRefinements           => _verifiedRefinements;
        public int   RefinementCount               => _refinementCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float VerifiedRefinementProgress => _verifiedRefinementsNeeded > 0
            ? Mathf.Clamp01(_verifiedRefinements / (float)_verifiedRefinementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _verifiedRefinements = Mathf.Min(_verifiedRefinements + 1, _verifiedRefinementsNeeded);
            if (_verifiedRefinements >= _verifiedRefinementsNeeded)
            {
                int bonus = _bonusPerRefinement;
                _refinementCount++;
                _totalBonusAwarded   += bonus;
                _verifiedRefinements  = 0;
                _onRefinementTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _verifiedRefinements = Mathf.Max(0, _verifiedRefinements - _predicateFalsificationsPerBot);
        }

        public void Reset()
        {
            _verifiedRefinements = 0;
            _refinementCount     = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureWeilConjectures", order = 518)]
    public sealed class ZoneControlCaptureWeilConjecturesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _zetaTermsNeeded         = 6;
        [SerializeField, Min(1)] private int _counterexamplesPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerVerification    = 4510;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWeilConjecturesVerified;

        private int _zetaTerms;
        private int _verificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ZetaTermsNeeded       => _zetaTermsNeeded;
        public int   CounterexamplesPerBot => _counterexamplesPerBot;
        public int   BonusPerVerification  => _bonusPerVerification;
        public int   ZetaTerms             => _zetaTerms;
        public int   VerificationCount     => _verificationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float ZetaTermProgress      => _zetaTermsNeeded > 0
            ? Mathf.Clamp01(_zetaTerms / (float)_zetaTermsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _zetaTerms = Mathf.Min(_zetaTerms + 1, _zetaTermsNeeded);
            if (_zetaTerms >= _zetaTermsNeeded)
            {
                int bonus = _bonusPerVerification;
                _verificationCount++;
                _totalBonusAwarded += bonus;
                _zetaTerms          = 0;
                _onWeilConjecturesVerified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _zetaTerms = Mathf.Max(0, _zetaTerms - _counterexamplesPerBot);
        }

        public void Reset()
        {
            _zetaTerms         = 0;
            _verificationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

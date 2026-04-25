using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRiemannHypothesis", order = 521)]
    public sealed class ZoneControlCaptureRiemannHypothesisSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _verifiedZerosNeeded      = 7;
        [SerializeField, Min(1)] private int _offLineDeviationsPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerVerification      = 4555;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRiemannHypothesisVerified;

        private int _verifiedZeros;
        private int _verificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VerifiedZerosNeeded    => _verifiedZerosNeeded;
        public int   OffLineDeviationsPerBot => _offLineDeviationsPerBot;
        public int   BonusPerVerification   => _bonusPerVerification;
        public int   VerifiedZeros          => _verifiedZeros;
        public int   VerificationCount      => _verificationCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float VerifiedZeroProgress   => _verifiedZerosNeeded > 0
            ? Mathf.Clamp01(_verifiedZeros / (float)_verifiedZerosNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _verifiedZeros = Mathf.Min(_verifiedZeros + 1, _verifiedZerosNeeded);
            if (_verifiedZeros >= _verifiedZerosNeeded)
            {
                int bonus = _bonusPerVerification;
                _verificationCount++;
                _totalBonusAwarded += bonus;
                _verifiedZeros      = 0;
                _onRiemannHypothesisVerified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _verifiedZeros = Mathf.Max(0, _verifiedZeros - _offLineDeviationsPerBot);
        }

        public void Reset()
        {
            _verifiedZeros     = 0;
            _verificationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

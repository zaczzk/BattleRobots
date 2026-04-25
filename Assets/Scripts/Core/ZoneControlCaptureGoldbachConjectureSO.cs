using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGoldbachConjecture", order = 531)]
    public sealed class ZoneControlCaptureGoldbachConjectureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _primePairsNeeded           = 6;
        [SerializeField, Min(1)] private int _compositeObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerVerification        = 4705;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGoldbachConjectureVerified;

        private int _primePairs;
        private int _verificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PrimePairsNeeded             => _primePairsNeeded;
        public int   CompositeObstructionsPerBot  => _compositeObstructionsPerBot;
        public int   BonusPerVerification         => _bonusPerVerification;
        public int   PrimePairs                   => _primePairs;
        public int   VerificationCount            => _verificationCount;
        public int   TotalBonusAwarded            => _totalBonusAwarded;
        public float PrimePairProgress            => _primePairsNeeded > 0
            ? Mathf.Clamp01(_primePairs / (float)_primePairsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _primePairs = Mathf.Min(_primePairs + 1, _primePairsNeeded);
            if (_primePairs >= _primePairsNeeded)
            {
                int bonus = _bonusPerVerification;
                _verificationCount++;
                _totalBonusAwarded += bonus;
                _primePairs         = 0;
                _onGoldbachConjectureVerified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _primePairs = Mathf.Max(0, _primePairs - _compositeObstructionsPerBot);
        }

        public void Reset()
        {
            _primePairs        = 0;
            _verificationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

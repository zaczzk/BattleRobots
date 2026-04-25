using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBirchSwinnertonDyer", order = 514)]
    public sealed class ZoneControlCaptureBirchSwinnertonDyerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _rationalPointsNeeded    = 6;
        [SerializeField, Min(1)] private int _tshObstructionsPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerVerification     = 4450;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBirchSwinnertonDyerVerified;

        private int _rationalPoints;
        private int _verificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RationalPointsNeeded   => _rationalPointsNeeded;
        public int   TshObstructionsPerBot   => _tshObstructionsPerBot;
        public int   BonusPerVerification    => _bonusPerVerification;
        public int   RationalPoints          => _rationalPoints;
        public int   VerificationCount       => _verificationCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float RationalPointProgress => _rationalPointsNeeded > 0
            ? Mathf.Clamp01(_rationalPoints / (float)_rationalPointsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rationalPoints = Mathf.Min(_rationalPoints + 1, _rationalPointsNeeded);
            if (_rationalPoints >= _rationalPointsNeeded)
            {
                int bonus = _bonusPerVerification;
                _verificationCount++;
                _totalBonusAwarded += bonus;
                _rationalPoints     = 0;
                _onBirchSwinnertonDyerVerified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _rationalPoints = Mathf.Max(0, _rationalPoints - _tshObstructionsPerBot);
        }

        public void Reset()
        {
            _rationalPoints    = 0;
            _verificationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

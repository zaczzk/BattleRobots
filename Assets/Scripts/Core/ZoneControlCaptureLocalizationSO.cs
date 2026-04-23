using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLocalization", order = 394)]
    public sealed class ZoneControlCaptureLocalizationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _primesNeeded          = 5;
        [SerializeField, Min(1)] private int _globalPerBot          = 1;
        [SerializeField, Min(0)] private int _bonusPerLocalization  = 2650;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLocalizationApplied;

        private int _primes;
        private int _localizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PrimesNeeded         => _primesNeeded;
        public int   GlobalPerBot         => _globalPerBot;
        public int   BonusPerLocalization => _bonusPerLocalization;
        public int   Primes               => _primes;
        public int   LocalizationCount    => _localizationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float PrimeProgress        => _primesNeeded > 0
            ? Mathf.Clamp01(_primes / (float)_primesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _primes = Mathf.Min(_primes + 1, _primesNeeded);
            if (_primes >= _primesNeeded)
            {
                int bonus = _bonusPerLocalization;
                _localizationCount++;
                _totalBonusAwarded += bonus;
                _primes             = 0;
                _onLocalizationApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _primes = Mathf.Max(0, _primes - _globalPerBot);
        }

        public void Reset()
        {
            _primes            = 0;
            _localizationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

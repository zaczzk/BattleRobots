using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureConcurrentType", order = 565)]
    public sealed class ZoneControlCaptureConcurrentTypeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _raceFreeDerivationsNeeded = 6;
        [SerializeField, Min(1)] private int _dataRacesPerBot           = 1;
        [SerializeField, Min(0)] private int _bonusPerDerivation        = 5215;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onConcurrentTypeCompleted;

        private int _raceFreeDerivations;
        private int _derivationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RaceFreeDerivationsNeeded => _raceFreeDerivationsNeeded;
        public int   DataRacesPerBot           => _dataRacesPerBot;
        public int   BonusPerDerivation        => _bonusPerDerivation;
        public int   RaceFreeDerivations       => _raceFreeDerivations;
        public int   DerivationCount           => _derivationCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float RaceFreeDerivationProgress => _raceFreeDerivationsNeeded > 0
            ? Mathf.Clamp01(_raceFreeDerivations / (float)_raceFreeDerivationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _raceFreeDerivations = Mathf.Min(_raceFreeDerivations + 1, _raceFreeDerivationsNeeded);
            if (_raceFreeDerivations >= _raceFreeDerivationsNeeded)
            {
                int bonus = _bonusPerDerivation;
                _derivationCount++;
                _totalBonusAwarded    += bonus;
                _raceFreeDerivations   = 0;
                _onConcurrentTypeCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _raceFreeDerivations = Mathf.Max(0, _raceFreeDerivations - _dataRacesPerBot);
        }

        public void Reset()
        {
            _raceFreeDerivations = 0;
            _derivationCount     = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

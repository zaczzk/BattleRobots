using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTypeTheory", order = 552)]
    public sealed class ZoneControlCaptureTypeTheorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _typeDerivationsNeeded = 6;
        [SerializeField, Min(1)] private int _typeErrorsPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerDerivation    = 5020;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTypeTheoryCompleted;

        private int _typeDerivations;
        private int _derivationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TypeDerivationsNeeded => _typeDerivationsNeeded;
        public int   TypeErrorsPerBot      => _typeErrorsPerBot;
        public int   BonusPerDerivation    => _bonusPerDerivation;
        public int   TypeDerivations       => _typeDerivations;
        public int   DerivationCount       => _derivationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float TypeDerivationProgress => _typeDerivationsNeeded > 0
            ? Mathf.Clamp01(_typeDerivations / (float)_typeDerivationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _typeDerivations = Mathf.Min(_typeDerivations + 1, _typeDerivationsNeeded);
            if (_typeDerivations >= _typeDerivationsNeeded)
            {
                int bonus = _bonusPerDerivation;
                _derivationCount++;
                _totalBonusAwarded += bonus;
                _typeDerivations    = 0;
                _onTypeTheoryCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _typeDerivations = Mathf.Max(0, _typeDerivations - _typeErrorsPerBot);
        }

        public void Reset()
        {
            _typeDerivations   = 0;
            _derivationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

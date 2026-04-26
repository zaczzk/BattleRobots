using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSequentCalculus", order = 548)]
    public sealed class ZoneControlCaptureSequentCalculusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _sequentDerivationsNeeded    = 6;
        [SerializeField, Min(1)] private int _structuralViolationsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerDerivation          = 4960;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSequentDerivationCompleted;

        private int _sequentDerivations;
        private int _derivationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SequentDerivationsNeeded   => _sequentDerivationsNeeded;
        public int   StructuralViolationsPerBot => _structuralViolationsPerBot;
        public int   BonusPerDerivation         => _bonusPerDerivation;
        public int   SequentDerivations         => _sequentDerivations;
        public int   DerivationCount            => _derivationCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float SequentDerivationProgress => _sequentDerivationsNeeded > 0
            ? Mathf.Clamp01(_sequentDerivations / (float)_sequentDerivationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sequentDerivations = Mathf.Min(_sequentDerivations + 1, _sequentDerivationsNeeded);
            if (_sequentDerivations >= _sequentDerivationsNeeded)
            {
                int bonus = _bonusPerDerivation;
                _derivationCount++;
                _totalBonusAwarded  += bonus;
                _sequentDerivations  = 0;
                _onSequentDerivationCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sequentDerivations = Mathf.Max(0, _sequentDerivations - _structuralViolationsPerBot);
        }

        public void Reset()
        {
            _sequentDerivations = 0;
            _derivationCount    = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

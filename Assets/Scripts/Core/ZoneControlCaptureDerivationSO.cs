using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDerivation", order = 395)]
    public sealed class ZoneControlCaptureDerivationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _operandsNeeded       = 5;
        [SerializeField, Min(1)] private int _dissolvePerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerDerivation   = 2665;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDerivationComputed;

        private int _operands;
        private int _derivationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OperandsNeeded       => _operandsNeeded;
        public int   DissolvePerBot       => _dissolvePerBot;
        public int   BonusPerDerivation   => _bonusPerDerivation;
        public int   Operands             => _operands;
        public int   DerivationCount      => _derivationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float OperandProgress      => _operandsNeeded > 0
            ? Mathf.Clamp01(_operands / (float)_operandsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _operands = Mathf.Min(_operands + 1, _operandsNeeded);
            if (_operands >= _operandsNeeded)
            {
                int bonus = _bonusPerDerivation;
                _derivationCount++;
                _totalBonusAwarded += bonus;
                _operands           = 0;
                _onDerivationComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _operands = Mathf.Max(0, _operands - _dissolvePerBot);
        }

        public void Reset()
        {
            _operands          = 0;
            _derivationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

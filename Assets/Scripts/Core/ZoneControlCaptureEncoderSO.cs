using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEncoder", order = 328)]
    public sealed class ZoneControlCaptureEncoderSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _symbolsNeeded  = 5;
        [SerializeField, Min(1)] private int _errorPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerEncode = 1660;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEncoderEncoded;

        private int _symbols;
        private int _encodeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SymbolsNeeded     => _symbolsNeeded;
        public int   ErrorPerBot       => _errorPerBot;
        public int   BonusPerEncode    => _bonusPerEncode;
        public int   Symbols           => _symbols;
        public int   EncodeCount       => _encodeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SymbolProgress    => _symbolsNeeded > 0
            ? Mathf.Clamp01(_symbols / (float)_symbolsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _symbols = Mathf.Min(_symbols + 1, _symbolsNeeded);
            if (_symbols >= _symbolsNeeded)
            {
                int bonus = _bonusPerEncode;
                _encodeCount++;
                _totalBonusAwarded += bonus;
                _symbols            = 0;
                _onEncoderEncoded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _symbols = Mathf.Max(0, _symbols - _errorPerBot);
        }

        public void Reset()
        {
            _symbols           = 0;
            _encodeCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

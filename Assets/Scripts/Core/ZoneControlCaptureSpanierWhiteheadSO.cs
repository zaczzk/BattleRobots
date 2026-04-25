using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpanierWhitehead", order = 505)]
    public sealed class ZoneControlCaptureSpanierWhiteheadSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _dualPairsNeeded              = 7;
        [SerializeField, Min(1)] private int _suspensionInstabilitiesPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerDualization           = 4315;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpanierWhiteheadDualized;

        private int _dualPairs;
        private int _dualizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DualPairsNeeded              => _dualPairsNeeded;
        public int   SuspensionInstabilitiesPerBot => _suspensionInstabilitiesPerBot;
        public int   BonusPerDualization           => _bonusPerDualization;
        public int   DualPairs                     => _dualPairs;
        public int   DualizationCount              => _dualizationCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float DualPairProgress => _dualPairsNeeded > 0
            ? Mathf.Clamp01(_dualPairs / (float)_dualPairsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _dualPairs = Mathf.Min(_dualPairs + 1, _dualPairsNeeded);
            if (_dualPairs >= _dualPairsNeeded)
            {
                int bonus = _bonusPerDualization;
                _dualizationCount++;
                _totalBonusAwarded += bonus;
                _dualPairs          = 0;
                _onSpanierWhiteheadDualized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _dualPairs = Mathf.Max(0, _dualPairs - _suspensionInstabilitiesPerBot);
        }

        public void Reset()
        {
            _dualPairs         = 0;
            _dualizationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

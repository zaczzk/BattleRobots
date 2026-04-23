using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCompact", order = 421)]
    public sealed class ZoneControlCaptureCompactSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _factorsNeeded      = 4;
        [SerializeField, Min(1)] private int _compressionPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerCompact    = 3055;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCompactObjectFormed;

        private int _factors;
        private int _compactCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FactorsNeeded       => _factorsNeeded;
        public int   CompressionPerBot   => _compressionPerBot;
        public int   BonusPerCompact     => _bonusPerCompact;
        public int   Factors             => _factors;
        public int   CompactCount        => _compactCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float FactorProgress      => _factorsNeeded > 0
            ? Mathf.Clamp01(_factors / (float)_factorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _factors = Mathf.Min(_factors + 1, _factorsNeeded);
            if (_factors >= _factorsNeeded)
            {
                int bonus = _bonusPerCompact;
                _compactCount++;
                _totalBonusAwarded += bonus;
                _factors            = 0;
                _onCompactObjectFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _factors = Mathf.Max(0, _factors - _compressionPerBot);
        }

        public void Reset()
        {
            _factors           = 0;
            _compactCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

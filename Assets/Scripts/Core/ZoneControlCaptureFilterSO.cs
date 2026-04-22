using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFilter", order = 330)]
    public sealed class ZoneControlCaptureFilterSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bandsNeeded    = 5;
        [SerializeField, Min(1)] private int _noisePerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerFilter = 1690;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFilterApplied;

        private int _bands;
        private int _filterCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BandsNeeded       => _bandsNeeded;
        public int   NoisePerBot       => _noisePerBot;
        public int   BonusPerFilter    => _bonusPerFilter;
        public int   Bands             => _bands;
        public int   FilterCount       => _filterCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BandProgress      => _bandsNeeded > 0
            ? Mathf.Clamp01(_bands / (float)_bandsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bands = Mathf.Min(_bands + 1, _bandsNeeded);
            if (_bands >= _bandsNeeded)
            {
                int bonus = _bonusPerFilter;
                _filterCount++;
                _totalBonusAwarded += bonus;
                _bands              = 0;
                _onFilterApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bands = Mathf.Max(0, _bands - _noisePerBot);
        }

        public void Reset()
        {
            _bands             = 0;
            _filterCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

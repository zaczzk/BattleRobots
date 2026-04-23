using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpectrum", order = 393)]
    public sealed class ZoneControlCaptureSpectrumSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bandsNeeded        = 7;
        [SerializeField, Min(1)] private int _collapsePerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerResolution = 2635;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpectrumResolved;

        private int _bands;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BandsNeeded        => _bandsNeeded;
        public int   CollapsePerBot     => _collapsePerBot;
        public int   BonusPerResolution => _bonusPerResolution;
        public int   Bands              => _bands;
        public int   ResolutionCount    => _resolutionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float BandProgress       => _bandsNeeded > 0
            ? Mathf.Clamp01(_bands / (float)_bandsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bands = Mathf.Min(_bands + 1, _bandsNeeded);
            if (_bands >= _bandsNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded += bonus;
                _bands              = 0;
                _onSpectrumResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bands = Mathf.Max(0, _bands - _collapsePerBot);
        }

        public void Reset()
        {
            _bands             = 0;
            _resolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

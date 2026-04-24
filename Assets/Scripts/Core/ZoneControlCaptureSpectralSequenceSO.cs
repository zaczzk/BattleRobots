using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpectralSequence", order = 467)]
    public sealed class ZoneControlCaptureSpectralSequenceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pagesNeeded          = 5;
        [SerializeField, Min(1)] private int _differentialsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerConvergence  = 3745;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpectralSequenceConverged;

        private int _pages;
        private int _convergeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PagesNeeded         => _pagesNeeded;
        public int   DifferentialsPerBot => _differentialsPerBot;
        public int   BonusPerConvergence => _bonusPerConvergence;
        public int   Pages               => _pages;
        public int   ConvergeCount       => _convergeCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float PageProgress        => _pagesNeeded > 0
            ? Mathf.Clamp01(_pages / (float)_pagesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pages = Mathf.Min(_pages + 1, _pagesNeeded);
            if (_pages >= _pagesNeeded)
            {
                int bonus = _bonusPerConvergence;
                _convergeCount++;
                _totalBonusAwarded += bonus;
                _pages              = 0;
                _onSpectralSequenceConverged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pages = Mathf.Max(0, _pages - _differentialsPerBot);
        }

        public void Reset()
        {
            _pages             = 0;
            _convergeCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

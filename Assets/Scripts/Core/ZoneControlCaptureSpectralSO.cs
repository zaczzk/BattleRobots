using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpectral", order = 388)]
    public sealed class ZoneControlCaptureSpectralSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pagesNeeded         = 5;
        [SerializeField, Min(1)] private int _degradePerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerConvergence = 2560;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpectralConverged;

        private int _pages;
        private int _convergenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PagesNeeded        => _pagesNeeded;
        public int   DegradePerBot      => _degradePerBot;
        public int   BonusPerConvergence => _bonusPerConvergence;
        public int   Pages              => _pages;
        public int   ConvergenceCount   => _convergenceCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float PageProgress       => _pagesNeeded > 0
            ? Mathf.Clamp01(_pages / (float)_pagesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pages = Mathf.Min(_pages + 1, _pagesNeeded);
            if (_pages >= _pagesNeeded)
            {
                int bonus = _bonusPerConvergence;
                _convergenceCount++;
                _totalBonusAwarded += bonus;
                _pages              = 0;
                _onSpectralConverged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pages = Mathf.Max(0, _pages - _degradePerBot);
        }

        public void Reset()
        {
            _pages             = 0;
            _convergenceCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

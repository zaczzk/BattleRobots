using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureScroll", order = 254)]
    public sealed class ZoneControlCaptureScrollSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _scrollsNeeded  = 4;
        [SerializeField, Min(1)] private int _lostPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerCodex  = 550;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCodexComplete;

        private int _scrolls;
        private int _codexCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ScrollsNeeded     => _scrollsNeeded;
        public int   LostPerBot        => _lostPerBot;
        public int   BonusPerCodex     => _bonusPerCodex;
        public int   Scrolls           => _scrolls;
        public int   CodexCount        => _codexCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ScrollProgress    => _scrollsNeeded > 0
            ? Mathf.Clamp01(_scrolls / (float)_scrollsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _scrolls = Mathf.Min(_scrolls + 1, _scrollsNeeded);
            if (_scrolls >= _scrollsNeeded)
            {
                int bonus = _bonusPerCodex;
                _codexCount++;
                _totalBonusAwarded += bonus;
                _scrolls            = 0;
                _onCodexComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _scrolls = Mathf.Max(0, _scrolls - _lostPerBot);
        }

        public void Reset()
        {
            _scrolls           = 0;
            _codexCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

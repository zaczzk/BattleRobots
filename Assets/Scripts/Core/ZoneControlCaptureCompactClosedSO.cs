using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCompactClosed", order = 429)]
    public sealed class ZoneControlCaptureCompactClosedSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cupsNeeded     = 6;
        [SerializeField, Min(1)] private int _cancelPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerCompact = 3175;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCompacted;

        private int _cups;
        private int _compactCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CupsNeeded        => _cupsNeeded;
        public int   CancelPerBot      => _cancelPerBot;
        public int   BonusPerCompact   => _bonusPerCompact;
        public int   Cups              => _cups;
        public int   CompactCount      => _compactCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CupProgress       => _cupsNeeded > 0
            ? Mathf.Clamp01(_cups / (float)_cupsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cups = Mathf.Min(_cups + 1, _cupsNeeded);
            if (_cups >= _cupsNeeded)
            {
                int bonus = _bonusPerCompact;
                _compactCount++;
                _totalBonusAwarded += bonus;
                _cups               = 0;
                _onCompacted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cups = Mathf.Max(0, _cups - _cancelPerBot);
        }

        public void Reset()
        {
            _cups              = 0;
            _compactCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

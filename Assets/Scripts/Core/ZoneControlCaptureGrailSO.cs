using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGrail", order = 260)]
    public sealed class ZoneControlCaptureGrailSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _dropsNeeded  = 6;
        [SerializeField, Min(1)] private int _spillPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerFill = 640;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGrailFilled;

        private int _drops;
        private int _fillCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DropsNeeded       => _dropsNeeded;
        public int   SpillPerBot       => _spillPerBot;
        public int   BonusPerFill      => _bonusPerFill;
        public int   Drops             => _drops;
        public int   FillCount         => _fillCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float DropProgress      => _dropsNeeded > 0
            ? Mathf.Clamp01(_drops / (float)_dropsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _drops = Mathf.Min(_drops + 1, _dropsNeeded);
            if (_drops >= _dropsNeeded)
            {
                int bonus = _bonusPerFill;
                _fillCount++;
                _totalBonusAwarded += bonus;
                _drops              = 0;
                _onGrailFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _drops = Mathf.Max(0, _drops - _spillPerBot);
        }

        public void Reset()
        {
            _drops             = 0;
            _fillCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

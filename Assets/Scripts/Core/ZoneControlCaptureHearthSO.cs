using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHearth", order = 231)]
    public sealed class ZoneControlCaptureHearthSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesPerLog  = 3;
        [SerializeField, Min(1)] private int _maxLogs         = 5;
        [SerializeField, Min(0)] private int _bonusPerIgnite  = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onIgnite;

        private int _rawCaptures;
        private int _logCount;
        private int _igniteCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesPerLog    => _capturesPerLog;
        public int   MaxLogs           => _maxLogs;
        public int   BonusPerIgnite    => _bonusPerIgnite;
        public int   RawCaptures       => _rawCaptures;
        public int   LogCount          => _logCount;
        public int   IgniteCount       => _igniteCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LogProgress       => _maxLogs > 0
            ? Mathf.Clamp01(_logCount / (float)_maxLogs)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rawCaptures++;
            if (_rawCaptures >= _capturesPerLog)
            {
                _rawCaptures = 0;
                _logCount++;
                if (_logCount >= _maxLogs)
                {
                    int bonus = _bonusPerIgnite;
                    _igniteCount++;
                    _totalBonusAwarded += bonus;
                    _logCount           = 0;
                    _onIgnite?.Raise();
                    return bonus;
                }
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _logCount    = Mathf.Max(0, _logCount - 1);
            _rawCaptures = 0;
        }

        public void Reset()
        {
            _rawCaptures       = 0;
            _logCount          = 0;
            _igniteCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

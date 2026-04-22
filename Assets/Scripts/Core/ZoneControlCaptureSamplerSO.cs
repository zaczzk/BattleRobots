using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSampler", order = 333)]
    public sealed class ZoneControlCaptureSamplerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _recordingsNeeded   = 7;
        [SerializeField, Min(1)] private int _glitchPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerRecording  = 1735;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSamplerRecorded;

        private int _recordings;
        private int _recordCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RecordingsNeeded   => _recordingsNeeded;
        public int   GlitchPerBot       => _glitchPerBot;
        public int   BonusPerRecording  => _bonusPerRecording;
        public int   Recordings         => _recordings;
        public int   RecordCount        => _recordCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float RecordingProgress  => _recordingsNeeded > 0
            ? Mathf.Clamp01(_recordings / (float)_recordingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _recordings = Mathf.Min(_recordings + 1, _recordingsNeeded);
            if (_recordings >= _recordingsNeeded)
            {
                int bonus = _bonusPerRecording;
                _recordCount++;
                _totalBonusAwarded += bonus;
                _recordings         = 0;
                _onSamplerRecorded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _recordings = Mathf.Max(0, _recordings - _glitchPerBot);
        }

        public void Reset()
        {
            _recordings        = 0;
            _recordCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRotor", order = 306)]
    public sealed class ZoneControlCaptureRotorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _segmentsNeeded      = 7;
        [SerializeField, Min(1)] private int _dragPerBot          = 2;
        [SerializeField, Min(0)] private int _bonusPerRevolution  = 1330;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRotorRevolved;

        private int _segments;
        private int _revolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SegmentsNeeded    => _segmentsNeeded;
        public int   DragPerBot        => _dragPerBot;
        public int   BonusPerRevolution => _bonusPerRevolution;
        public int   Segments          => _segments;
        public int   RevolutionCount   => _revolutionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SegmentProgress   => _segmentsNeeded > 0
            ? Mathf.Clamp01(_segments / (float)_segmentsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _segments = Mathf.Min(_segments + 1, _segmentsNeeded);
            if (_segments >= _segmentsNeeded)
            {
                int bonus = _bonusPerRevolution;
                _revolutionCount++;
                _totalBonusAwarded += bonus;
                _segments           = 0;
                _onRotorRevolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _segments = Mathf.Max(0, _segments - _dragPerBot);
        }

        public void Reset()
        {
            _segments          = 0;
            _revolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

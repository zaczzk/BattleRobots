using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks the consistency of zone captures by measuring the average
    /// time gap between captures. Fires <c>_onConsistentCapture</c> whenever
    /// a gap falls below <c>_consistencyThreshold</c> seconds.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureConsistency.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureConsistency", order = 131)]
    public sealed class ZoneControlCaptureConsistencySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _consistencyThreshold = 8f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onConsistentCapture;

        private float _lastCaptureTime = -1f;
        private int   _gapCount;
        private float _totalGapTime;
        private int   _consistentCaptures;

        private void OnEnable() => Reset();

        public float ConsistencyThreshold => _consistencyThreshold;
        public int   GapCount             => _gapCount;
        public float TotalGapTime         => _totalGapTime;
        public int   ConsistentCaptures   => _consistentCaptures;
        public bool  HasFirstCapture      => _lastCaptureTime >= 0f;

        /// <summary>Average gap in seconds between captures; 0 before the second capture.</summary>
        public float AverageGap => _gapCount > 0
            ? _totalGapTime / _gapCount
            : 0f;

        public void RecordCapture(float time)
        {
            if (HasFirstCapture)
            {
                float gap = time - _lastCaptureTime;
                _gapCount++;
                _totalGapTime += gap;

                if (gap < _consistencyThreshold)
                {
                    _consistentCaptures++;
                    _onConsistentCapture?.Raise();
                }
            }

            _lastCaptureTime = time;
        }

        public void Reset()
        {
            _lastCaptureTime    = -1f;
            _gapCount           = 0;
            _totalGapTime       = 0f;
            _consistentCaptures = 0;
        }
    }
}

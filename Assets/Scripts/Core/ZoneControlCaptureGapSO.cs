using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that measures the elapsed time between consecutive zone captures.
    /// When the gap is less than or equal to <c>_fastGapThreshold</c>, it is
    /// classified as a fast capture and <c>_onFastCapture</c> is fired.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureGap.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGap", order = 104)]
    public sealed class ZoneControlCaptureGapSO : ScriptableObject
    {
        [Header("Gap Settings")]
        [Min(0.1f)]
        [SerializeField] private float _fastGapThreshold = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFastCapture;

        private float _lastCaptureTime = -1f;
        private float _lastGap;
        private int   _fastCaptureCount;

        private void OnEnable() => Reset();

        public float FastGapThreshold  => _fastGapThreshold;
        public float LastGap           => _lastGap;
        public int   FastCaptureCount  => _fastCaptureCount;
        public bool  HasFirstCapture   => _lastCaptureTime >= 0f;

        /// <summary>
        /// Records a capture at <paramref name="timestamp"/>.  The gap to the
        /// previous capture is measured; gaps within the threshold are fast.
        /// </summary>
        public void RecordCapture(float timestamp)
        {
            if (_lastCaptureTime >= 0f)
            {
                float gap = timestamp - _lastCaptureTime;
                _lastGap  = gap;
                if (gap <= _fastGapThreshold)
                {
                    _fastCaptureCount++;
                    _onFastCapture?.Raise();
                }
            }
            _lastCaptureTime = timestamp;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lastCaptureTime = -1f;
            _lastGap         = 0f;
            _fastCaptureCount = 0;
        }
    }
}

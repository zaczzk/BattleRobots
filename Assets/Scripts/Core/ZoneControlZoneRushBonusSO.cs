using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that rewards rapid consecutive zone captures ("rushes"). Each call to
    /// <c>RecordCapture(t)</c> checks whether it arrives within <c>_maxGapSeconds</c> of the
    /// previous capture. When <c>_rushTargetCount</c> consecutive fast captures accumulate,
    /// <c>_onRushAchieved</c> fires, the counter resets, and a new rush sequence can begin.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneRushBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneRushBonus", order = 117)]
    public sealed class ZoneControlZoneRushBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)]    private int   _rushTargetCount = 3;
        [SerializeField, Min(0.1f)] private float _maxGapSeconds   = 5f;
        [SerializeField, Min(0)]    private int   _rushBonus       = 250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRushAchieved;

        private float _lastCaptureTime;
        private bool  _hasPrevious;
        private int   _fastCaptureCount;
        private int   _rushCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RushTargetCount   => _rushTargetCount;
        public float MaxGapSeconds     => _maxGapSeconds;
        public int   RushBonus         => _rushBonus;
        public int   RushCount         => _rushCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public int   FastCaptureCount  => _fastCaptureCount;

        /// <summary>
        /// Records a capture at time <c>t</c>. Increments the fast-capture counter when the gap
        /// to the previous capture is within <c>_maxGapSeconds</c>; otherwise restarts at 1.
        /// Fires <c>_onRushAchieved</c> when the counter reaches <c>_rushTargetCount</c>.
        /// </summary>
        public void RecordCapture(float t)
        {
            if (_hasPrevious && (t - _lastCaptureTime) <= _maxGapSeconds)
            {
                _fastCaptureCount++;
                if (_fastCaptureCount >= _rushTargetCount)
                {
                    _rushCount++;
                    _totalBonusAwarded += _rushBonus;
                    _onRushAchieved?.Raise();
                    _fastCaptureCount = 0;
                }
            }
            else
            {
                _fastCaptureCount = 1;
            }

            _lastCaptureTime = t;
            _hasPrevious     = true;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lastCaptureTime   = 0f;
            _hasPrevious       = false;
            _fastCaptureCount  = 0;
            _rushCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

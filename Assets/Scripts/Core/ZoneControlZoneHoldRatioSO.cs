using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks the ratio of match time during which the player holds zone majority.
    /// Call <c>StartMatch()</c> at match start, <c>SetMajority(bool)</c> on dominance changes,
    /// <c>Tick(dt)</c> each frame, and <c>EndMatch()</c> to stop accumulation.
    /// <c>HoldRatio</c> returns [0,1]: hold time / total match time.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneHoldRatio.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneHoldRatio", order = 123)]
    public sealed class ZoneControlZoneHoldRatioSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRatioUpdated;

        private bool  _isRunning;
        private bool  _hasMajority;
        private float _totalMatchTime;
        private float _totalHoldTime;

        private void OnEnable() => Reset();

        public bool  IsRunning      => _isRunning;
        public bool  HasMajority    => _hasMajority;
        public float TotalMatchTime => _totalMatchTime;
        public float TotalHoldTime  => _totalHoldTime;

        /// <summary>
        /// Ratio of match time spent holding majority [0,1].
        /// Returns 0 when no match time has elapsed.
        /// </summary>
        public float HoldRatio =>
            _totalMatchTime > 0f ? Mathf.Clamp01(_totalHoldTime / _totalMatchTime) : 0f;

        /// <summary>Begins tracking match time.</summary>
        public void StartMatch()
        {
            _isRunning      = true;
            _totalMatchTime = 0f;
            _totalHoldTime  = 0f;
            _hasMajority    = false;
        }

        /// <summary>Stops accumulation (call on match end).</summary>
        public void EndMatch() => _isRunning = false;

        /// <summary>Updates majority hold state. Call on dominance change events.</summary>
        public void SetMajority(bool hasMajority) => _hasMajority = hasMajority;

        /// <summary>Advances time accumulators. Call from Update.</summary>
        public void Tick(float dt)
        {
            if (!_isRunning || dt <= 0f) return;
            _totalMatchTime += dt;
            if (_hasMajority)
                _totalHoldTime += dt;
            _onRatioUpdated?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _isRunning      = false;
            _hasMajority    = false;
            _totalMatchTime = 0f;
            _totalHoldTime  = 0f;
        }
    }
}

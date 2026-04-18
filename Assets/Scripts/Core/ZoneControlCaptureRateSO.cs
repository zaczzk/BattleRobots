using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks the match-wide average zone-capture rate (captures per minute).
    /// Call <see cref="StartMatch"/> when a match begins and <see cref="RecordCapture"/> on
    /// every player zone capture.  <see cref="GetAverageRate"/> returns captures/minute since
    /// match start, or 0 when no time has elapsed.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureRate.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRate", order = 106)]
    public sealed class ZoneControlCaptureRateSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRateUpdated;

        private float _matchStartTime = -1f;
        private int   _captureCount;

        private void OnEnable() => Reset();

        public int  CaptureCount  => _captureCount;
        public bool MatchStarted  => _matchStartTime >= 0f;

        /// <summary>Records match start time.</summary>
        public void StartMatch(float gameTime)
        {
            _matchStartTime = gameTime;
            _captureCount   = 0;
        }

        /// <summary>Increments capture count and fires <c>_onRateUpdated</c>.</summary>
        public void RecordCapture(float gameTime)
        {
            _captureCount++;
            _onRateUpdated?.Raise();
        }

        /// <summary>
        /// Returns average captures per minute since match start.
        /// Returns 0 when the match has not started or elapsed time is zero.
        /// </summary>
        public float GetAverageRate(float currentTime)
        {
            if (!MatchStarted) return 0f;
            float elapsedMinutes = (currentTime - _matchStartTime) / 60f;
            if (elapsedMinutes <= 0f) return 0f;
            return _captureCount / elapsedMinutes;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _matchStartTime = -1f;
            _captureCount   = 0;
        }
    }
}

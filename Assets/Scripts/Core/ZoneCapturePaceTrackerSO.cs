using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks the player's zone-capture rate within a
    /// configurable rolling time window and fires pace events when the rate crosses
    /// defined thresholds.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="RecordCapture"/> each time the player captures a zone,
    ///     passing <c>Time.time</c> as the timestamp.
    ///   • Old entries outside the rolling window are pruned automatically on each
    ///     <see cref="RecordCapture"/> call.
    ///   • <see cref="GetCapturesPerMinute"/> prunes stale entries and returns the
    ///     current rate; call with <c>Time.time</c> from a HUD controller's Update
    ///     or a Tick method.
    ///   • <see cref="_onFastPace"/> is raised when
    ///     rate ≥ <see cref="FastPaceThreshold"/>.
    ///   • <see cref="_onSlowPace"/> is raised when
    ///     rate ≤ <see cref="SlowPaceThreshold"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on domain reload.
    ///   - <see cref="List{T}"/> of <c>float</c> is used for the rolling window;
    ///     this may allocate on resize but is not called in a hot loop.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneCapturePaceTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneCapturePaceTracker", order = 25)]
    public sealed class ZoneCapturePaceTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Pace Settings")]
        [Tooltip("Duration of the rolling window in seconds.")]
        [SerializeField, Min(1f)] private float _windowDuration = 60f;

        [Tooltip("Captures-per-minute rate at or above which _onFastPace is raised.")]
        [SerializeField, Min(0f)] private float _fastPaceThreshold = 2f;

        [Tooltip("Captures-per-minute rate at or below which _onSlowPace is raised.")]
        [SerializeField, Min(0f)] private float _slowPaceThreshold = 0.5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised by RecordCapture and Reset.")]
        [SerializeField] private VoidGameEvent _onPaceUpdated;

        [Tooltip("Raised when the current rate is at or above FastPaceThreshold.")]
        [SerializeField] private VoidGameEvent _onFastPace;

        [Tooltip("Raised when the current rate is at or below SlowPaceThreshold.")]
        [SerializeField] private VoidGameEvent _onSlowPace;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<float> _timestamps = new List<float>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Rolling window duration in seconds.</summary>
        public float WindowDuration => _windowDuration;

        /// <summary>Rate threshold (captures/min) that triggers <c>_onFastPace</c>.</summary>
        public float FastPaceThreshold => _fastPaceThreshold;

        /// <summary>Rate threshold (captures/min) that triggers <c>_onSlowPace</c>.</summary>
        public float SlowPaceThreshold => _slowPaceThreshold;

        /// <summary>Number of capture timestamps currently in the rolling window.</summary>
        public int CaptureCount => _timestamps.Count;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a zone capture at the given <paramref name="timestamp"/> (use
        /// <c>Time.time</c>), prunes stale entries, and fires pace events.
        /// </summary>
        public void RecordCapture(float timestamp)
        {
            _timestamps.Add(timestamp);
            Prune(timestamp);

            float rate = ComputeRate();
            if (rate >= _fastPaceThreshold) _onFastPace?.Raise();
            if (rate <= _slowPaceThreshold) _onSlowPace?.Raise();
            _onPaceUpdated?.Raise();
        }

        /// <summary>
        /// Returns the current zone-capture rate in captures per minute.
        /// Prunes entries outside the rolling window before computing.
        /// Zero allocation (operates on the existing list).
        /// </summary>
        /// <param name="currentTime">Current game time (<c>Time.time</c>).</param>
        public float GetCapturesPerMinute(float currentTime)
        {
            Prune(currentTime);
            return ComputeRate();
        }

        /// <summary>
        /// Clears all recorded timestamps.
        /// Fires <see cref="_onPaceUpdated"/>.
        /// </summary>
        public void Reset()
        {
            _timestamps.Clear();
            _onPaceUpdated?.Raise();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void Prune(float currentTime)
        {
            float cutoff = currentTime - _windowDuration;
            // Remove from front (oldest entries are added first via Add).
            while (_timestamps.Count > 0 && _timestamps[0] < cutoff)
                _timestamps.RemoveAt(0);
        }

        private float ComputeRate()
        {
            if (_windowDuration <= 0f) return 0f;
            return (float)_timestamps.Count / _windowDuration * 60f;
        }
    }
}

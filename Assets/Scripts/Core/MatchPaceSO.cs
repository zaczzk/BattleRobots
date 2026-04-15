using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that measures the pace of a match by counting discrete
    /// events (damage hits, captures, ability activations, etc.) within a rolling time
    /// window, then classifying the window as fast or slow.
    ///
    /// ── Pace window rules ──────────────────────────────────────────────────────
    ///   • Each call to <see cref="IncrementEvent"/> increments an internal counter.
    ///   • <see cref="Tick"/> advances the window timer by <c>deltaTime</c>.
    ///     When the window closes (<c>elapsed ≥ WindowDuration</c>) the counter is
    ///     evaluated against <see cref="FastThreshold"/> and <see cref="SlowThreshold"/>,
    ///     then reset for the next window.
    ///   • <see cref="Evaluate"/> fires <see cref="_onFastPace"/> when
    ///     <c>eventCount ≥ FastThreshold</c>, or <see cref="_onSlowPace"/> when
    ///     <c>eventCount ≤ SlowThreshold</c>.  Only one event fires per window.
    ///   • <see cref="Reset"/> silently clears the counter and elapsed time.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on all hot-path methods.
    ///   - Tick must be driven externally (from a MatchPaceController.Update).
    ///   - SO assets are immutable at runtime — only IncrementEvent/Tick/Reset mutate.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Match ▶ MatchPace.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Match/MatchPace", order = 10)]
    public sealed class MatchPaceSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Pace Window Settings")]
        [Tooltip("Length of the evaluation window in seconds. " +
                 "Events are counted over this period then reset.")]
        [SerializeField, Min(5f)] private float _windowDuration = 10f;

        [Header("Thresholds")]
        [Tooltip("Events per window that classify the match as fast-paced.")]
        [SerializeField, Min(1)] private int _fastThreshold = 5;

        [Tooltip("Events per window at or below which the match is classified as slow.")]
        [SerializeField, Min(0)] private int _slowThreshold = 1;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised at window close when event count ≥ FastThreshold.")]
        [SerializeField] private VoidGameEvent _onFastPace;

        [Tooltip("Raised at window close when event count ≤ SlowThreshold.")]
        [SerializeField] private VoidGameEvent _onSlowPace;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _eventCount;
        private float _windowElapsed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of events counted in the current window.</summary>
        public int EventCount => _eventCount;

        /// <summary>Duration of each evaluation window in seconds.</summary>
        public float WindowDuration => _windowDuration;

        /// <summary>Event count threshold for a fast-pace classification.</summary>
        public int FastThreshold => _fastThreshold;

        /// <summary>Event count threshold at or below which the match is slow.</summary>
        public int SlowThreshold => _slowThreshold;

        /// <summary>
        /// Approximate events per second for the current window.
        /// Returns 0 when <see cref="WindowDuration"/> is zero or window is fresh.
        /// </summary>
        public float EventRate =>
            _windowDuration > 0f ? _eventCount / _windowDuration : 0f;

        /// <summary>Seconds elapsed in the current window.</summary>
        public float WindowElapsed => _windowElapsed;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Counts one match event toward the current window total.
        /// Zero allocation.
        /// </summary>
        public void IncrementEvent()
        {
            _eventCount++;
        }

        /// <summary>
        /// Advances the window timer by <paramref name="dt"/> seconds.
        /// When the window closes, calls <see cref="Evaluate"/> then resets the
        /// counter and timer for the next window.
        /// Zero allocation — integer and float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            _windowElapsed += dt;

            if (_windowElapsed >= _windowDuration)
            {
                Evaluate();
                _eventCount    = 0;
                _windowElapsed = 0f;
            }
        }

        /// <summary>
        /// Fires the appropriate pace channel based on the current event count.
        /// Fast and slow are mutually exclusive; fast takes priority.
        /// Zero allocation.
        /// </summary>
        public void Evaluate()
        {
            if (_eventCount >= _fastThreshold)
                _onFastPace?.Raise();
            else if (_eventCount <= _slowThreshold)
                _onSlowPace?.Raise();
        }

        /// <summary>
        /// Silently resets the event counter and window timer.
        /// Does NOT fire any events — safe to call at match start.
        /// </summary>
        public void Reset()
        {
            _eventCount    = 0;
            _windowElapsed = 0f;
        }
    }
}

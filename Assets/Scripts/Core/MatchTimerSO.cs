using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks match time — both elapsed and remaining.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="StartTimer"/> at match start (wire MatchStarted event).
    ///   2. Call <see cref="Tick"/> each frame from a driving MonoBehaviour's Update.
    ///   3. Call <see cref="StopTimer"/> on match end.
    ///   4. Call <see cref="Reset"/> to restore to full duration for next match.
    ///
    /// ── Events ──────────────────────────────────────────────────────────────────
    ///   <see cref="_onTimerUpdated"/> — FloatGameEvent raised each Tick while running;
    ///     payload = seconds remaining. Wire to UI controllers and MatchTimerWarningSO.
    ///   <see cref="_onTimerExpired"/> — VoidGameEvent raised once when remaining hits zero.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path Tick() (float arithmetic only).
    ///   - SO assets are immutable at runtime — only timer-state fields mutate.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchTimer.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/MatchTimer")]
    public sealed class MatchTimerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Timer Settings")]
        [Tooltip("Full match duration in seconds.")]
        [SerializeField, Min(1f)] private float _duration = 120f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised every Tick while the timer is running. Payload = seconds remaining.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Tooltip("Raised once when the timer reaches zero.")]
        [SerializeField] private VoidGameEvent _onTimerExpired;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _timeRemaining;
        private bool  _isRunning;
        private bool  _expired;   // edge-guard for _onTimerExpired

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Configured match duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Seconds remaining in the current match. Always ≥ 0.</summary>
        public float TimeRemaining => _timeRemaining;

        /// <summary>Seconds elapsed since the timer was last started.</summary>
        public float TimeElapsed => _duration - _timeRemaining;

        /// <summary>True while the timer is counting down.</summary>
        public bool IsRunning => _isRunning;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _timeRemaining = _duration;
            _isRunning     = false;
            _expired       = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins countdown. No-op if already running.
        /// </summary>
        public void StartTimer()
        {
            _isRunning = true;
        }

        /// <summary>
        /// Pauses countdown. No-op if already stopped.
        /// </summary>
        public void StopTimer()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Advances the timer by <paramref name="deltaTime"/> seconds.
        /// No-op when not running or already expired.
        /// Fires <see cref="_onTimerUpdated"/> with remaining seconds.
        /// Fires <see cref="_onTimerExpired"/> once when remaining reaches zero.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isRunning || _expired) return;

            _timeRemaining = Mathf.Max(0f, _timeRemaining - deltaTime);
            _onTimerUpdated?.Raise(_timeRemaining);

            if (_timeRemaining <= 0f && !_expired)
            {
                _expired   = true;
                _isRunning = false;
                _onTimerExpired?.Raise();
            }
        }

        /// <summary>
        /// Resets the timer to full <see cref="Duration"/> and clears running/expired state.
        /// Does not fire any event channels.
        /// </summary>
        public void Reset()
        {
            _timeRemaining = _duration;
            _isRunning     = false;
            _expired       = false;
        }
    }
}

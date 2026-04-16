using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pace advice category returned by <see cref="ZoneControlMatchPaceSO.EvaluatePace"/>.
    /// </summary>
    public enum ZoneControlPaceAdvice
    {
        OnTarget,
        AheadOfPace,
        BehindPace
    }

    /// <summary>
    /// Runtime ScriptableObject that compares the player's current capture rate
    /// against a configurable target pace and fires directional events.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="EvaluatePace"/> with the captures-per-minute value from
    ///   <c>ZoneCapturePaceTrackerSO.GetCapturesPerMinute</c>.
    ///   The SO fires <see cref="_onAheadOfPace"/> when the rate exceeds
    ///   <c>TargetPace + AheadThreshold</c> and <see cref="_onBehindPace"/>
    ///   when it falls below <c>TargetPace - BehindThreshold</c>.
    ///   <see cref="LastAdvice"/> caches the most recent result.
    ///   Call <see cref="Reset"/> at match start.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at session start.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchPace.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchPace", order = 55)]
    public sealed class ZoneControlMatchPaceSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Pace Settings")]
        [Tooltip("Target zone captures per minute.")]
        [Min(0.1f)]
        [SerializeField] private float _targetPace = 1.5f;

        [Tooltip("Rate margin above target that triggers _onAheadOfPace.")]
        [Min(0.1f)]
        [SerializeField] private float _aheadThreshold = 0.5f;

        [Tooltip("Rate margin below target that triggers _onBehindPace.")]
        [Min(0.1f)]
        [SerializeField] private float _behindThreshold = 0.5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the capture rate exceeds TargetPace + AheadThreshold.")]
        [SerializeField] private VoidGameEvent _onAheadOfPace;

        [Tooltip("Raised when the capture rate falls below TargetPace - BehindThreshold.")]
        [SerializeField] private VoidGameEvent _onBehindPace;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ZoneControlPaceAdvice _lastAdvice = ZoneControlPaceAdvice.OnTarget;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Target captures-per-minute value.</summary>
        public float TargetPace => _targetPace;

        /// <summary>Rate margin above target that triggers AheadOfPace.</summary>
        public float AheadThreshold => _aheadThreshold;

        /// <summary>Rate margin below target that triggers BehindPace.</summary>
        public float BehindThreshold => _behindThreshold;

        /// <summary>Most recent pace advice computed by <see cref="EvaluatePace"/>.</summary>
        public ZoneControlPaceAdvice LastAdvice => _lastAdvice;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates <paramref name="capturesPerMinute"/> against the target pace
        /// thresholds. Caches the result in <see cref="LastAdvice"/> and fires the
        /// appropriate event channel.
        /// </summary>
        /// <returns>The pace advice for the given rate.</returns>
        public ZoneControlPaceAdvice EvaluatePace(float capturesPerMinute)
        {
            if (capturesPerMinute >= _targetPace + _aheadThreshold)
            {
                _lastAdvice = ZoneControlPaceAdvice.AheadOfPace;
                _onAheadOfPace?.Raise();
            }
            else if (capturesPerMinute <= _targetPace - _behindThreshold)
            {
                _lastAdvice = ZoneControlPaceAdvice.BehindPace;
                _onBehindPace?.Raise();
            }
            else
            {
                _lastAdvice = ZoneControlPaceAdvice.OnTarget;
            }

            return _lastAdvice;
        }

        /// <summary>
        /// Resets <see cref="LastAdvice"/> to <c>OnTarget</c> silently.
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _lastAdvice = ZoneControlPaceAdvice.OnTarget;
        }

        private void OnValidate()
        {
            _targetPace      = Mathf.Max(0.1f, _targetPace);
            _aheadThreshold  = Mathf.Max(0.1f, _aheadThreshold);
            _behindThreshold = Mathf.Max(0.1f, _behindThreshold);
        }
    }
}

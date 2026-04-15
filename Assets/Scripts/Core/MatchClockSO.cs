using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Authoritative in-match countdown timer implemented as a ScriptableObject so that
    /// any number of UI controllers can query it without referencing each other.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   StartClock() → Tick(dt) × N → StopClock() / _onTimeExpired fires
    ///
    /// ── Event channels ────────────────────────────────────────────────────────
    ///   _onTimeWarning  — raised once when <see cref="TimeRemaining"/> drops to or
    ///                     below <see cref="WarningThreshold"/> seconds.
    ///   _onTimeExpired  — raised once when <see cref="TimeRemaining"/> reaches 0.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • Runtime state (_elapsed, _isRunning, _warningFired) is NOT serialised so
    ///     the SO asset file is never dirtied during play.
    ///   • OnEnable calls <see cref="Reset"/> for a clean baseline on domain reload.
    ///   • <see cref="Tick"/> is the only mutating entry point; all other state is
    ///     driven by <see cref="StartClock"/> / <see cref="StopClock"/> / <see cref="Reset"/>.
    ///   • Warning fires only once per clock run (idempotent guard via _warningFired).
    ///   • Time-expired fires only once and stops the clock automatically.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Match ▶ MatchClock.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Match/MatchClock", order = 20)]
    public sealed class MatchClockSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Clock Settings")]
        [Tooltip("Total match duration in seconds. Minimum 1 second.")]
        [SerializeField, Min(1f)] private float _duration = 180f;

        [Tooltip("Seconds remaining at which the time-warning event fires once. " +
                 "Set to 0 to disable. Minimum 0.")]
        [SerializeField, Min(0f)] private float _warningThreshold = 30f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once when TimeRemaining drops to or below WarningThreshold.")]
        [SerializeField] private VoidGameEvent _onTimeWarning;

        [Tooltip("Raised once when the clock reaches zero (TimeRemaining = 0).")]
        [SerializeField] private VoidGameEvent _onTimeExpired;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isRunning;
        private float _elapsed;
        private bool  _warningFired;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the clock is ticking.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Total clock duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Seconds remaining on the clock. Clamped to [0, Duration].</summary>
        public float TimeRemaining => Mathf.Max(0f, _duration - _elapsed);

        /// <summary>Ratio of time remaining: TimeRemaining / Duration ∈ [0, 1].</summary>
        public float TimeRatio => (_duration > 0f) ? (TimeRemaining / _duration) : 0f;

        /// <summary>Seconds below which the warning event fires.</summary>
        public float WarningThreshold => _warningThreshold;

        /// <summary>True once the warning event has fired during the current clock run.</summary>
        public bool WarningFired => _warningFired;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the clock from the beginning.
        /// Resets elapsed time, clears the warning flag, and sets <see cref="IsRunning"/> true.
        /// </summary>
        public void StartClock()
        {
            _elapsed      = 0f;
            _warningFired = false;
            _isRunning    = true;
        }

        /// <summary>
        /// Pauses / stops the clock without firing any event.
        /// Safe to call multiple times.
        /// </summary>
        public void StopClock()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Advances the clock by <paramref name="dt"/> seconds.
        /// Fires <c>_onTimeWarning</c> (once) when remaining time crosses the
        /// warning threshold, then fires <c>_onTimeExpired</c> and stops the clock
        /// when time reaches zero.
        /// No-op when <see cref="IsRunning"/> is false.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isRunning) return;

            _elapsed += dt;

            // Check warning threshold (fires at most once per run).
            if (!_warningFired && _warningThreshold > 0f && TimeRemaining <= _warningThreshold)
            {
                _warningFired = true;
                _onTimeWarning?.Raise();
            }

            // Check expiry.
            if (_elapsed >= _duration)
            {
                _elapsed   = _duration;
                _isRunning = false;
                _onTimeExpired?.Raise();
            }
        }

        /// <summary>
        /// Resets runtime state silently (no events fired, <see cref="IsRunning"/> false).
        /// Used by OnEnable and test teardown.
        /// </summary>
        public void Reset()
        {
            _isRunning    = false;
            _elapsed      = 0f;
            _warningFired = false;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_warningThreshold > _duration)
                Debug.LogWarning($"[MatchClockSO] '{name}': " +
                                 $"_warningThreshold ({_warningThreshold}s) exceeds " +
                                 $"_duration ({_duration}s) — the warning will fire immediately.");
        }
#endif
    }
}

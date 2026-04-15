using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject representing a capturable arena control zone.
    ///
    /// ── Capture rules ──────────────────────────────────────────────────────────
    ///   • <see cref="CaptureProgress"/> accumulates elapsed time each frame
    ///     while a robot occupies the zone.
    ///   • When elapsed time reaches <see cref="CaptureTime"/>, the zone becomes
    ///     captured and fires <see cref="_onCaptured"/>.
    ///   • <see cref="Lose"/> reverses a capture (if already captured) or cancels
    ///     progress (if only capturing), then fires <see cref="_onLost"/> if it
    ///     was previously captured.
    ///   • <see cref="ScoreTick"/> fires <see cref="_onScoreTick"/> once per score
    ///     interval while the zone is captured; the controller drives the cadence.
    ///   • <see cref="Reset"/> silently clears all runtime state — no events fired.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path methods (float arithmetic only).
    ///   - SO assets are immutable at runtime — only Capture/Lose/ScoreTick/Reset
    ///     mutate state.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ControlZone.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ControlZone", order = 14)]
    public sealed class ControlZoneSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Identity")]
        [Tooltip("Display name for this zone shown in HUD labels.")]
        [SerializeField] private string _zoneId = "Zone";

        [Header("Capture Settings")]
        [Tooltip("Seconds of continuous occupation required to capture the zone.")]
        [SerializeField, Min(0.1f)] private float _captureTime = 3f;

        [Tooltip("Score points awarded per score-tick while the zone is captured.")]
        [SerializeField, Min(0f)] private float _scorePerSecond = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when elapsed occupation time first reaches CaptureTime.")]
        [SerializeField] private VoidGameEvent _onCaptured;

        [Tooltip("Raised when a captured zone loses its occupant.")]
        [SerializeField] private VoidGameEvent _onLost;

        [Tooltip("Raised each time the controller fires a score tick while captured.")]
        [SerializeField] private VoidGameEvent _onScoreTick;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool  _isCaptured;
        private bool  _isCapturing;
        private float _elapsed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Display name of this zone.</summary>
        public string ZoneId => _zoneId;

        /// <summary>Seconds of continuous occupation required to capture.</summary>
        public float CaptureTime => _captureTime;

        /// <summary>Score awarded per score-tick while captured.</summary>
        public float ScorePerSecond => _scorePerSecond;

        /// <summary>True once the zone has been fully captured.</summary>
        public bool IsCaptured => _isCaptured;

        /// <summary>True while an occupant is present but the zone is not yet captured.</summary>
        public bool IsCapturing => _isCapturing;

        /// <summary>Normalised capture progress in [0, 1]. Suitable for Slider.value.</summary>
        public float CaptureRatio =>
            _captureTime > 0f ? Mathf.Clamp01(_elapsed / _captureTime) : 0f;

        /// <summary>Raw elapsed occupation time in seconds.</summary>
        public float ElapsedCapture => _elapsed;

        /// <summary>Event raised when the zone is captured. May be null.</summary>
        public VoidGameEvent OnCaptured => _onCaptured;

        /// <summary>Event raised when a captured zone is lost. May be null.</summary>
        public VoidGameEvent OnLost => _onLost;

        /// <summary>Event raised each score-tick while captured. May be null.</summary>
        public VoidGameEvent OnScoreTick => _onScoreTick;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the capture timer by <paramref name="dt"/> seconds.
        /// When total elapsed time reaches <see cref="CaptureTime"/> the zone becomes
        /// captured and fires <see cref="_onCaptured"/>.
        /// No-op once the zone is already captured.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void CaptureProgress(float dt)
        {
            if (_isCaptured) return;

            _isCapturing = true;
            _elapsed    += dt;

            if (_elapsed >= _captureTime)
            {
                _elapsed     = _captureTime;
                _isCaptured  = true;
                _onCaptured?.Raise();
            }
        }

        /// <summary>
        /// Cancels capture progress if the zone is being captured, or releases the
        /// zone and fires <see cref="_onLost"/> if the zone was already captured.
        /// Zero allocation.
        /// </summary>
        public void Lose()
        {
            bool wasCaptured = _isCaptured;

            _isCaptured  = false;
            _isCapturing = false;
            _elapsed     = 0f;

            if (wasCaptured)
                _onLost?.Raise();
        }

        /// <summary>
        /// Fires <see cref="_onScoreTick"/> if the zone is currently captured.
        /// Called by the controller at <c>_tickInterval</c> cadence.
        /// Zero allocation.
        /// </summary>
        public void ScoreTick()
        {
            if (!_isCaptured) return;
            _onScoreTick?.Raise();
        }

        /// <summary>
        /// Silently resets all runtime state to initial values.
        /// Does NOT fire any events — safe to call at match start.
        /// </summary>
        public void Reset()
        {
            _isCaptured  = false;
            _isCapturing = false;
            _elapsed     = 0f;
        }
    }
}

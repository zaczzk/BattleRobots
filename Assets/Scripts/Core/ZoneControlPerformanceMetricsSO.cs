using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks player capture accuracy and capture rate for a
    /// match and derives a letter performance grade.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="RecordCapture"/> counts a successful zone capture (also
    ///   increments attempts).
    ///   <see cref="RecordAttempt"/> counts a missed capture (attempt only).
    ///   <see cref="FinalizeMetrics"/> locks the match duration and fires
    ///   <c>_onMetricsFinalized</c>.
    ///   <see cref="GetPerformanceGrade"/> returns "S/A/B/C/D" based on
    ///   <see cref="CaptureAccuracy"/> and <see cref="CaptureRate"/>.
    ///   <see cref="Reset"/> clears all runtime state silently; called from
    ///   <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlPerformanceMetrics.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlPerformanceMetrics", order = 76)]
    public sealed class ZoneControlPerformanceMetricsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Grade Thresholds")]
        [Tooltip("Minimum capture accuracy (0–1) for S grade.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sGradeAccuracy = 0.9f;

        [Tooltip("Minimum captures-per-minute for S grade.")]
        [Min(0f)]
        [SerializeField] private float _sGradeRate = 3f;

        [Tooltip("Minimum capture accuracy (0–1) for A grade.")]
        [Range(0f, 1f)]
        [SerializeField] private float _aGradeAccuracy = 0.7f;

        [Tooltip("Minimum captures-per-minute for A grade.")]
        [Min(0f)]
        [SerializeField] private float _aGradeRate = 2f;

        [Tooltip("Minimum capture accuracy (0–1) for B grade.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bGradeAccuracy = 0.5f;

        [Tooltip("Minimum captures-per-minute for B grade.")]
        [Min(0f)]
        [SerializeField] private float _bGradeRate = 1f;

        [Tooltip("Minimum capture accuracy (0–1) for C grade.")]
        [Range(0f, 1f)]
        [SerializeField] private float _cGradeAccuracy = 0.3f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMetricsFinalized;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _captureCount;
        private int   _attemptCount;
        private float _matchDurationSeconds;
        private bool  _isFinalized;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        public int   CaptureCount          => _captureCount;
        public int   AttemptCount          => _attemptCount;
        public float MatchDurationSeconds  => _matchDurationSeconds;
        public bool  IsFinalized           => _isFinalized;

        /// <summary>Successful captures / total attempts; 0 when no attempts recorded.</summary>
        public float CaptureAccuracy =>
            _attemptCount > 0 ? (float)_captureCount / _attemptCount : 0f;

        /// <summary>Captures per minute; 0 when match duration is not yet recorded.</summary>
        public float CaptureRate =>
            _matchDurationSeconds > 0f ? _captureCount / (_matchDurationSeconds / 60f) : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Records a successful zone capture (also counts as an attempt).</summary>
        public void RecordCapture()
        {
            _captureCount++;
            _attemptCount++;
        }

        /// <summary>Records a missed capture attempt (no successful capture).</summary>
        public void RecordAttempt()
        {
            _attemptCount++;
        }

        /// <summary>
        /// Locks the match duration and fires <c>_onMetricsFinalized</c>.
        /// Subsequent calls are ignored.
        /// </summary>
        public void FinalizeMetrics(float durationSeconds)
        {
            if (_isFinalized) return;
            _matchDurationSeconds = Mathf.Max(0f, durationSeconds);
            _isFinalized          = true;
            _onMetricsFinalized?.Raise();
        }

        /// <summary>
        /// Returns a letter grade (S/A/B/C/D) based on capture accuracy and rate.
        /// Requires both metrics to meet the threshold for S and A; accuracy alone
        /// drives B, C and D.
        /// </summary>
        public string GetPerformanceGrade()
        {
            float acc  = CaptureAccuracy;
            float rate = CaptureRate;

            if (acc >= _sGradeAccuracy && rate >= _sGradeRate) return "S";
            if (acc >= _aGradeAccuracy && rate >= _aGradeRate) return "A";
            if (acc >= _bGradeAccuracy && rate >= _bGradeRate) return "B";
            if (acc >= _cGradeAccuracy)                        return "C";
            return "D";
        }

        /// <summary>Clears all runtime state silently.  Called from <c>OnEnable</c>.</summary>
        public void Reset()
        {
            _captureCount         = 0;
            _attemptCount         = 0;
            _matchDurationSeconds = 0f;
            _isFinalized          = false;
        }
    }
}

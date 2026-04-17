using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Bot capture behaviour patterns inferred from match data.</summary>
    public enum ZoneControlBotPattern
    {
        Random     = 0,
        Systematic = 1
    }

    /// <summary>
    /// Runtime SO that stores pre-match intel derived from the previous match:
    /// the bot's effective capture rate (caps/min) and inferred behaviour pattern.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="UpdateReport"/> computes the capture rate from raw counts and
    ///   match duration, classifies the pattern against
    ///   <c>_systematicThreshold</c>, marks the report as generated, and fires
    ///   <c>_onReportGenerated</c>.
    ///   <see cref="GetSummary"/> returns a human-readable string or "No data"
    ///   when the report has not yet been generated.
    ///   <see cref="Reset"/> clears state silently; called from <c>OnEnable</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on play-mode entry.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlScoutReport.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlScoutReport", order = 72)]
    public sealed class ZoneControlScoutReportSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scout Settings")]
        [Tooltip("Captures-per-minute rate at or above which the bot is classified as Systematic.")]
        [Min(0.1f)]
        [SerializeField] private float _systematicThreshold = 1.0f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time UpdateReport completes successfully.")]
        [SerializeField] private VoidGameEvent _onReportGenerated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float                _botCaptureRate;
        private ZoneControlBotPattern _botPattern;
        private bool                 _isGenerated;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Bot capture rate from the last match in captures per minute.</summary>
        public float                 BotCaptureRate      => _botCaptureRate;

        /// <summary>Inferred behaviour pattern from the last match.</summary>
        public ZoneControlBotPattern BotPattern          => _botPattern;

        /// <summary>True once <see cref="UpdateReport"/> has been called successfully.</summary>
        public bool                  IsGenerated         => _isGenerated;

        /// <summary>Threshold (caps/min) above which the pattern is Systematic.</summary>
        public float                 SystematicThreshold => _systematicThreshold;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Computes and stores the bot capture rate and pattern.
        /// No-ops when <paramref name="matchDuration"/> is zero or negative.
        /// Fires <c>_onReportGenerated</c> on success.
        /// </summary>
        /// <param name="botCaptures">Total bot zone captures in the match (clamped to ≥ 0).</param>
        /// <param name="matchDuration">Match duration in seconds (must be &gt; 0).</param>
        public void UpdateReport(int botCaptures, float matchDuration)
        {
            if (matchDuration <= 0f) return;

            _botCaptureRate = Mathf.Max(0, botCaptures) / (matchDuration / 60f);
            _botPattern     = _botCaptureRate >= _systematicThreshold
                              ? ZoneControlBotPattern.Systematic
                              : ZoneControlBotPattern.Random;
            _isGenerated    = true;
            _onReportGenerated?.Raise();
        }

        /// <summary>
        /// Returns a human-readable scout summary, or "No data" if
        /// <see cref="IsGenerated"/> is false.
        /// </summary>
        public string GetSummary() =>
            _isGenerated
                ? $"Bot Rate: {_botCaptureRate:F1}/min - {_botPattern}"
                : "No data";

        /// <summary>
        /// Clears all runtime state silently.  Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _botCaptureRate = 0f;
            _botPattern     = ZoneControlBotPattern.Random;
            _isGenerated    = false;
        }
    }
}

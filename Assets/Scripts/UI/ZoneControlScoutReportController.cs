using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that generates a bot scout report at the end of each
    /// match and displays it for the player's next tactical decision.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchStarted</c>: resets the bot capture counter and start time,
    ///   then calls <see cref="ZoneControlScoutReportSO.Reset"/> and refreshes.
    ///   On <c>_onMatchEnded</c>: calls
    ///   <see cref="ZoneControlScoutReportSO.UpdateReport"/> with the accumulated
    ///   bot captures and elapsed duration, then refreshes.
    ///   On <c>_onReportGenerated</c>: refreshes the display.
    ///   <see cref="RecordBotCapture"/> increments the internal bot capture counter
    ///   (call from an IntGameEvent listener or another controller).
    ///   <see cref="Refresh"/> shows <c>GetSummary()</c>; hides panel when SO null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one scout report controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlScoutReportController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlScoutReportSO _reportSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onReportGenerated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _reportLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Runtime tracking ──────────────────────────────────────────────────

        private int   _botCaptureCount;
        private float _matchStartTime;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onReportGenerated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onReportGenerated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Resets the bot capture counter and records the match start time.
        /// </summary>
        public void HandleMatchStarted()
        {
            _botCaptureCount = 0;
            _matchStartTime  = Time.time;
            _reportSO?.Reset();
            Refresh();
        }

        /// <summary>
        /// Generates the scout report from accumulated data and refreshes.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_reportSO == null) { Refresh(); return; }
            float duration = Time.time - _matchStartTime;
            _reportSO.UpdateReport(_botCaptureCount, duration);
            Refresh();
        }

        /// <summary>
        /// Increments the bot capture counter.  Call this whenever the bot
        /// captures a zone during the match (e.g., from an IntGameEvent handler).
        /// </summary>
        public void RecordBotCapture() => _botCaptureCount++;

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the report label from <see cref="ZoneControlScoutReportSO.GetSummary"/>.
        /// Hides the panel when <c>_reportSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_reportSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_reportLabel != null)
                _reportLabel.text = _reportSO.GetSummary();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound scout report SO (may be null).</summary>
        public ZoneControlScoutReportSO ReportSO       => _reportSO;

        /// <summary>Bot captures recorded since the last <see cref="HandleMatchStarted"/>.</summary>
        public int                      BotCaptureCount => _botCaptureCount;
    }
}

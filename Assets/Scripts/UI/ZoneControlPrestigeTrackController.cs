using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that accumulates zone captures from
    /// <see cref="ZoneControlSessionSummarySO"/> into the
    /// <see cref="ZoneControlPrestigeTrackSO"/> at match end and
    /// displays the current prestige title and capture total.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: reads <c>_summarySO.TotalZonesCaptured</c>
    ///   and calls <see cref="ZoneControlPrestigeTrackSO.AddCaptures"/>, then
    ///   refreshes the display.
    ///   On <c>_onPrestigeGranted</c>: refreshes the display.
    ///   <see cref="Refresh"/> shows title + cumulative capture count.
    ///   Panel is hidden when <c>_trackSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one prestige track controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlPrestigeTrackController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlPrestigeTrackSO  _trackSO;
        [SerializeField] private ZoneControlSessionSummarySO _summarySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onPrestigeGranted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _titleLabel;
        [SerializeField] private Text       _captureLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onPrestigeGranted?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onPrestigeGranted?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Feeds the session's capture total into the prestige track and refreshes.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_trackSO == null) { Refresh(); return; }
            _trackSO.AddCaptures(_summarySO != null ? _summarySO.TotalZonesCaptured : 0);
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the prestige title and cumulative capture labels.
        /// Hides the panel when <c>_trackSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_trackSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_titleLabel   != null)
                _titleLabel.text   = _trackSO.GetPrestigeTitle();

            if (_captureLabel != null)
                _captureLabel.text = $"Captures: {_trackSO.TotalCaptures}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound prestige track SO (may be null).</summary>
        public ZoneControlPrestigeTrackSO TrackSO => _trackSO;
    }
}

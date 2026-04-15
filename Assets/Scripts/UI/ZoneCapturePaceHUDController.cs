using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the current zone-capture pace from a
    /// <see cref="ZoneCapturePaceTrackerSO"/> and shows coloured fast/slow
    /// pace indicators.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _rateLabel      → "{rate:F1}/min"  (live captures-per-minute reading)
    ///   _fastIndicator  → shown when rate ≥ FastPaceThreshold
    ///   _slowIndicator  → shown when rate ≤ SlowPaceThreshold
    ///   _panel          → root panel; hidden when <c>_trackerSO</c> is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onCaptured</c> to call RecordCapture(Time.time) and
    ///     to <c>_onPaceUpdated</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one pace HUD per panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _trackerSO      → ZoneCapturePaceTrackerSO asset.
    ///   2. Assign _onCaptured     → ControlZoneSO._onCaptured or a shared channel.
    ///   3. Assign _onPaceUpdated  → ZoneCapturePaceTrackerSO._onPaceUpdated channel.
    ///   4. Assign _rateLabel, _fastIndicator, _slowIndicator, and _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCapturePaceHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneCapturePaceTrackerSO _trackerSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to a zone-captured event; triggers RecordCapture(Time.time).")]
        [SerializeField] private VoidGameEvent _onCaptured;

        [Tooltip("Wire to ZoneCapturePaceTrackerSO._onPaceUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onPaceUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _rateLabel;
        [SerializeField] private GameObject _fastIndicator;
        [SerializeField] private GameObject _slowIndicator;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _recordDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _recordDelegate  = HandleCapture;
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onCaptured?.RegisterCallback(_recordDelegate);
            _onPaceUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onCaptured?.UnregisterCallback(_recordDelegate);
            _onPaceUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a zone capture at the current game time and refreshes the HUD.
        /// Null-safe.
        /// </summary>
        public void HandleCapture()
        {
            _trackerSO?.RecordCapture(Time.time);
            Refresh();
        }

        /// <summary>
        /// Rebuilds the pace label and indicator states from the current tracker rate.
        /// Hides the panel when <c>_trackerSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_trackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            float rate = _trackerSO.GetCapturesPerMinute(Time.time);

            if (_rateLabel != null)
                _rateLabel.text = $"{rate:F1}/min";

            _fastIndicator?.SetActive(rate >= _trackerSO.FastPaceThreshold);
            _slowIndicator?.SetActive(rate <= _trackerSO.SlowPaceThreshold);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound tracker SO (may be null).</summary>
        public ZoneCapturePaceTrackerSO TrackerSO => _trackerSO;
    }
}

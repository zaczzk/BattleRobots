using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records per-match average capture-pace readings into a
    /// <see cref="ZoneCapturePaceHistorySO"/> and visualises the history as a
    /// colour-coded bar chart using Image fill amounts.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _paceBarImages → Array of up to 5 Image components (oldest left to right).
    ///                    Each bar:
    ///                      enabled      = true  when a reading exists at that index
    ///                      fillAmount   = Clamp01(reading / _maxDisplayRate)
    ///                    Bars beyond the available reading count are disabled.
    ///   _panel         → Root panel; hidden when _historySO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onMatchEnded</c> to snapshot pace from
    ///     <c>_trackerSO.GetCapturesPerMinute</c> and append it to history.
    ///   - Subscribes to <c>_onPaceHistoryUpdated</c> for reactive bar refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one history panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _historySO            → ZoneCapturePaceHistorySO asset.
    ///   2. Assign _trackerSO            → ZoneCapturePaceTrackerSO asset.
    ///   3. Assign _onMatchEnded         → shared MatchEnded VoidGameEvent.
    ///   4. Assign _onPaceHistoryUpdated → ZoneCapturePaceHistorySO._onPaceHistoryUpdated.
    ///   5. Populate _paceBarImages and tune _maxDisplayRate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCapturePaceHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneCapturePaceHistorySO  _historySO;
        [SerializeField] private ZoneCapturePaceTrackerSO  _trackerSO;

        [Header("Display Settings")]
        [Tooltip("Rate (captures/min) that maps to a full bar (fillAmount = 1).")]
        [SerializeField, Min(0.1f)] private float _maxDisplayRate = 5f;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded channel; triggers pace snapshot.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneCapturePaceHistorySO._onPaceHistoryUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onPaceHistoryUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("Up to 5 Image refs representing pace bars (oldest → newest, left → right).")]
        [SerializeField] private Image[]    _paceBarImages;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _matchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchEndedDelegate = HandleMatchEnded;
            _refreshDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_matchEndedDelegate);
            _onPaceHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_matchEndedDelegate);
            _onPaceHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Snapshots the current captures-per-minute from <c>_trackerSO</c> and
        /// appends it to <c>_historySO</c>.
        /// No-op when either SO is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_historySO == null || _trackerSO == null) return;

            float rate = _trackerSO.GetCapturesPerMinute(Time.time);
            _historySO.AddPaceReading(rate);
        }

        /// <summary>
        /// Rebuilds the bar chart from the current pace history.
        /// Hides the panel when <c>_historySO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_historySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_paceBarImages == null) return;

            var readings = _historySO.GetReadings();

            for (int i = 0; i < _paceBarImages.Length; i++)
            {
                var bar = _paceBarImages[i];
                if (bar == null) continue;

                if (i < readings.Count)
                {
                    bar.enabled    = true;
                    bar.fillAmount = Mathf.Clamp01(readings[i] / _maxDisplayRate);
                }
                else
                {
                    bar.enabled = false;
                }
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound pace history SO (may be null).</summary>
        public ZoneCapturePaceHistorySO HistorySO => _historySO;

        /// <summary>The bound pace tracker SO (may be null).</summary>
        public ZoneCapturePaceTrackerSO TrackerSO => _trackerSO;
    }
}

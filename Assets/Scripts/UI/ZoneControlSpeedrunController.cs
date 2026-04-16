using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control speedrun HUD.
    /// At match end, records an attempt in <see cref="ZoneControlSpeedrunSO"/> using
    /// the session summary's zone count and elapsed match time, then refreshes the
    /// personal-best display.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _bestTimeLabel → "Best: F1s (N zones)" for the entry with the most zones,
    ///                    or "No records yet" when empty.
    ///   _panel         → Root panel; hidden when <c>_speedrunSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one speedrun panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _speedrunSO          → ZoneControlSpeedrunSO asset.
    ///   2. Assign _summarySO           → ZoneControlSessionSummarySO asset.
    ///   3. Assign _onMatchEnded        → a shared VoidGameEvent raised at match end.
    ///   4. Assign _onSpeedrunUpdated   → speedrunSO._onSpeedrunUpdated channel.
    ///   5. Assign label / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSpeedrunController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSpeedrunSO       _speedrunSO;
        [SerializeField] private ZoneControlSessionSummarySO _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers attempt recording.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlSpeedrunSO._onSpeedrunUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onSpeedrunUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _bestTimeLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float _matchStartTime;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
            _matchStartTime           = 0f;
        }

        private void OnEnable()
        {
            _matchStartTime = Time.time;
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onSpeedrunUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onSpeedrunUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a speedrun attempt using the session summary's total zones and
        /// elapsed match time since <c>OnEnable</c>.
        /// No-op when either SO is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_speedrunSO == null || _summarySO == null)
            {
                Refresh();
                return;
            }

            float elapsed  = Time.time - _matchStartTime;
            int   zones    = _summarySO.TotalZonesCaptured;
            _speedrunSO.RecordAttempt(elapsed, zones);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the personal-best label from the current snapshot.
        /// Shows the entry with the highest zone count.
        /// Hides the panel when <c>_speedrunSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_speedrunSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bestTimeLabel != null)
            {
                IReadOnlyList<ZoneControlSpeedrunEntry> snapshot = _speedrunSO.TakeSnapshot();
                if (snapshot.Count == 0)
                {
                    _bestTimeLabel.text = "No records yet";
                }
                else
                {
                    // TakeSnapshot is sorted ascending — last entry has the most zones.
                    ZoneControlSpeedrunEntry best = snapshot[snapshot.Count - 1];
                    _bestTimeLabel.text = $"Best: {best.BestTime:F1}s ({best.ZoneCount} zones)";
                }
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound speedrun SO (may be null).</summary>
        public ZoneControlSpeedrunSO SpeedrunSO => _speedrunSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}

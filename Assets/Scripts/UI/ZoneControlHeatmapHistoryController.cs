using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records heatmap snapshots at match end and drives a
    /// side-by-side session vs. lifetime heat bar HUD.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • On <c>_onMatchEnded</c>: reads per-zone capture counts from
    ///     <see cref="ZoneControlHeatmapSO"/>, appends to
    ///     <see cref="ZoneControlHeatmapHistorySO"/>, then calls Refresh.
    ///   • On <c>_onHistoryUpdated</c>: calls Refresh.
    ///   • Refresh populates two parallel bar arrays:
    ///       _sessionBars  — current-session heat levels (from <c>_heatmapSO</c>).
    ///       _lifetimeBars — lifetime heat levels (from <c>_historySO</c>).
    ///   • Panel hidden when <c>_historySO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one heatmap-history panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlHeatmapHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlHeatmapSO        _heatmapSO;
        [SerializeField] private ZoneControlHeatmapHistorySO _historySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers snapshot + refresh.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised by ZoneControlHeatmapHistorySO after every AddSnapshot; triggers refresh.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("One Image per zone showing the session heat level (fillAmount).")]
        [SerializeField] private Image[] _sessionBars;

        [Tooltip("One Image per zone showing the lifetime heat level (fillAmount).")]
        [SerializeField] private Image[] _lifetimeBars;

        [Header("UI Refs — Panel (optional)")]
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
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a per-zone capture count array from <c>_heatmapSO</c>, appends
        /// it to <c>_historySO</c>, then refreshes the HUD.
        /// No-op when <c>_historySO</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_historySO == null) return;

            if (_heatmapSO != null)
            {
                int zoneCount = _heatmapSO.ZoneCount;
                var counts    = new int[zoneCount];
                for (int i = 0; i < zoneCount; i++)
                    counts[i] = _heatmapSO.GetCaptureCount(i);

                _historySO.AddSnapshot(counts);
            }

            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Updates session and lifetime bar fills.
        /// Hides the panel when <c>_historySO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_historySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sessionBars != null && _heatmapSO != null)
            {
                for (int i = 0; i < _sessionBars.Length; i++)
                {
                    if (_sessionBars[i] == null) continue;
                    _sessionBars[i].fillAmount = _heatmapSO.GetHeatLevel(i);
                }
            }

            if (_lifetimeBars != null)
            {
                for (int i = 0; i < _lifetimeBars.Length; i++)
                {
                    if (_lifetimeBars[i] == null) continue;
                    _lifetimeBars[i].fillAmount = _historySO.GetLifetimeHeatLevel(i);
                }
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound heatmap SO (may be null).</summary>
        public ZoneControlHeatmapSO HeatmapSO => _heatmapSO;

        /// <summary>The bound heatmap history SO (may be null).</summary>
        public ZoneControlHeatmapHistorySO HistorySO => _historySO;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that takes a <see cref="ZoneControlHeatmapSO"/> snapshot at
    /// match end, stores it in <see cref="ZoneControlHeatmapHistorySO"/>, and drives
    /// parallel session-vs-lifetime heat bar arrays.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _sessionHeatBars[i]  → fillAmount = sessionSO.GetHeatLevel(i).
    ///   _lifetimeHeatBars[i] → fillAmount = historySO.GetLifetimeHeatLevel(i).
    ///   _panel               → hidden when historySO is null.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlHeatmapHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlHeatmapHistorySO _historySO;
        [SerializeField] private ZoneControlHeatmapSO         _sessionSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("UI Refs — Session heat bars (optional, one per zone)")]
        [SerializeField] private Image[] _sessionHeatBars;

        [Header("UI Refs — Lifetime heat bars (optional, one per zone)")]
        [SerializeField] private Image[] _lifetimeHeatBars;

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
        /// Takes a capture-count snapshot from the session SO and appends it to
        /// the history SO at match end.  No-op when either SO is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_historySO == null || _sessionSO == null) return;

            int zoneCount = _sessionSO.ZoneCount;
            var snapshot  = new int[zoneCount];
            for (int i = 0; i < zoneCount; i++)
                snapshot[i] = _sessionSO.GetCaptureCount(i);

            _historySO.AddSnapshot(snapshot);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds session and lifetime heat bars from their respective SOs.
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

            if (_sessionHeatBars != null && _sessionSO != null)
            {
                for (int i = 0; i < _sessionHeatBars.Length; i++)
                {
                    if (_sessionHeatBars[i] == null) continue;
                    _sessionHeatBars[i].fillAmount = _sessionSO.GetHeatLevel(i);
                }
            }

            if (_lifetimeHeatBars != null)
            {
                for (int i = 0; i < _lifetimeHeatBars.Length; i++)
                {
                    if (_lifetimeHeatBars[i] == null) continue;
                    _lifetimeHeatBars[i].fillAmount = _historySO.GetLifetimeHeatLevel(i);
                }
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound history SO (may be null).</summary>
        public ZoneControlHeatmapHistorySO HistorySO => _historySO;

        /// <summary>The bound session heatmap SO (may be null).</summary>
        public ZoneControlHeatmapSO SessionSO => _sessionSO;
    }
}

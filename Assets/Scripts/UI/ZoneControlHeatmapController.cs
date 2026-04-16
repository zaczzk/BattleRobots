using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives a per-zone heat-map HUD for zone-control mode.
    ///
    /// Subscribes to zone-capture and match-boundary events; delegates per-zone
    /// heat tracking to <see cref="ZoneControlHeatmapSO"/>; and refreshes a row of
    /// Image fill bars whose colour lerps from <see cref="_coldColor"/> to
    /// <see cref="_hotColor"/> as capture frequency rises.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _heatBars[i]  → Image (fillAmount = heat 0–1; color lerped cold→hot).
    ///   _panel        → Root panel; hidden when <c>_heatmapSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one heatmap panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _heatmapSO          → ZoneControlHeatmapSO asset.
    ///   2. Assign _onZoneCaptured     → IntGameEvent raised per zone capture (value = zone index).
    ///   3. Assign _onMatchStarted     → VoidGameEvent raised at match start.
    ///   4. Assign _onHeatmapUpdated   → _heatmapSO._onHeatmapUpdated channel.
    ///   5. Populate _heatBars         → one Image per zone (order matches zone index).
    ///   6. Assign _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlHeatmapController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlHeatmapSO _heatmapSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("IntGameEvent raised per zone capture; the int value is the zone index.")]
        [SerializeField] private IntGameEvent _onZoneCaptured;

        [Tooltip("Raised at match start; resets the heatmap SO state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised by ZoneControlHeatmapSO after each RecordCapture for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onHeatmapUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("One Image fill bar per zone (index matches zone index).")]
        [SerializeField] private Image[] _heatBars;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        [Header("Heat Colours")]
        [Tooltip("Bar colour when a zone has zero or low relative captures.")]
        [SerializeField] private Color _coldColor = Color.blue;

        [Tooltip("Bar colour when a zone has the maximum relative captures.")]
        [SerializeField] private Color _hotColor = Color.red;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action<int> _handleZoneCapturedDelegate;
        private Action      _handleMatchStartedDelegate;
        private Action      _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHeatmapUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHeatmapUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a zone capture for <paramref name="zoneIndex"/> on the heatmap SO.
        /// The SO fires <c>_onHeatmapUpdated</c> which triggers <see cref="Refresh"/>.
        /// </summary>
        public void HandleZoneCaptured(int zoneIndex)
        {
            _heatmapSO?.RecordCapture(zoneIndex);
        }

        /// <summary>
        /// Resets the heatmap SO at match start and refreshes the HUD.
        /// </summary>
        public void HandleMatchStarted()
        {
            _heatmapSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all heat-bar fill amounts and colours from the current heatmap state.
        /// Hides the panel when <c>_heatmapSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_heatmapSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_heatBars == null) return;

            for (int i = 0; i < _heatBars.Length; i++)
            {
                Image bar = _heatBars[i];
                if (bar == null) continue;

                float heat      = _heatmapSO.GetHeatLevel(i);
                bar.fillAmount  = heat;
                bar.color       = Color.Lerp(_coldColor, _hotColor, heat);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound heatmap SO (may be null).</summary>
        public ZoneControlHeatmapSO HeatmapSO => _heatmapSO;

        /// <summary>Number of heat-bar Images wired in the inspector.</summary>
        public int HeatBarCount => _heatBars?.Length ?? 0;
    }
}

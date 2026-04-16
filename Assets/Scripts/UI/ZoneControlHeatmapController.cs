using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives a per-zone heatmap HUD for zone-control matches.
    ///
    /// Each element in <see cref="_zoneCaptureChannels"/> corresponds to a specific
    /// zone.  When element[i] fires, <see cref="ZoneControlHeatmapSO.RecordCapture"/>
    /// is called for zone index i and the heat-bar array is refreshed.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _heatBars[i] → Image.fillAmount = GetHeatLevel(i)  (0 = cold, 1 = hottest)
    ///   _panel        → hidden when _heatmapSO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Per-zone Action delegates pre-allocated in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one heatmap-HUD panel per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _heatmapSO             → ZoneControlHeatmapSO asset.
    ///   2. Assign _zoneCaptureChannels[] → one VoidGameEvent per zone (index-matched).
    ///   3. Assign _onMatchStarted        → VoidGameEvent raised at match start.
    ///   4. Assign _heatBars[]            → one Image per zone (index-matched).
    ///   5. Assign _panel                 → root panel GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlHeatmapController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlHeatmapSO _heatmapSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("One entry per zone; element[i] fires when zone i is captured.")]
        [SerializeField] private VoidGameEvent[] _zoneCaptureChannels;

        [Tooltip("Raised at match start; resets the heatmap.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [Tooltip("One Image per zone; fillAmount driven by GetHeatLevel(i).")]
        [SerializeField] private Image[] _heatBars;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action[] _captureHandlers;
        private Action   _matchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchStartedDelegate = HandleMatchStarted;

            if (_zoneCaptureChannels != null && _zoneCaptureChannels.Length > 0)
            {
                _captureHandlers = new Action[_zoneCaptureChannels.Length];
                for (int i = 0; i < _zoneCaptureChannels.Length; i++)
                {
                    int zoneIndex = i; // captured by value for correct closure
                    _captureHandlers[i] = () => HandleZoneCaptured(zoneIndex);
                }
            }
            else
            {
                _captureHandlers = Array.Empty<Action>();
            }
        }

        private void OnEnable()
        {
            if (_zoneCaptureChannels != null)
            {
                for (int i = 0; i < _zoneCaptureChannels.Length && i < _captureHandlers.Length; i++)
                    _zoneCaptureChannels[i]?.RegisterCallback(_captureHandlers[i]);
            }

            _onMatchStarted?.RegisterCallback(_matchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            if (_zoneCaptureChannels != null)
            {
                for (int i = 0; i < _zoneCaptureChannels.Length && i < _captureHandlers.Length; i++)
                    _zoneCaptureChannels[i]?.UnregisterCallback(_captureHandlers[i]);
            }

            _onMatchStarted?.UnregisterCallback(_matchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Records a capture for <paramref name="zoneIndex"/> on the heatmap SO
        /// and refreshes the HUD.
        /// </summary>
        public void HandleZoneCaptured(int zoneIndex)
        {
            _heatmapSO?.RecordCapture(zoneIndex);
            Refresh();
        }

        /// <summary>Resets the heatmap SO and refreshes the HUD at match start.</summary>
        public void HandleMatchStarted()
        {
            _heatmapSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all heat-bar fill amounts from the current heatmap state.
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
                if (_heatBars[i] == null) continue;
                _heatBars[i].fillAmount = _heatmapSO.GetHeatLevel(i);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound heatmap SO (may be null).</summary>
        public ZoneControlHeatmapSO HeatmapSO => _heatmapSO;

        /// <summary>Number of per-zone capture handler delegates allocated in Awake.</summary>
        public int ChannelCount => _captureHandlers?.Length ?? 0;
    }
}

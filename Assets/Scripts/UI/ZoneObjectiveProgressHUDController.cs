using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays real-time zone-objective progress from a
    /// <see cref="ZoneObjectiveProgressTrackerSO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _progressLabel      → "N / M zones"  (held / required)
    ///   _progressBar        → Slider.value = ProgressRatio  [0, 1]
    ///   _objectiveMetPanel  → activated when IsObjectiveMet is true
    ///   _panel              → root panel; hidden when _trackerSO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onDominanceChanged for reactive refresh; the controller
    ///     does not poll — it refreshes only on event receipt and OnEnable.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _trackerSO           → ZoneObjectiveProgressTrackerSO asset.
    ///   2. Assign _onDominanceChanged  → ZoneDominanceSO._onDominanceChanged channel.
    ///   3. Assign optional UI Text, Slider, and panel references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneObjectiveProgressHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneObjectiveProgressTrackerSO _trackerSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneDominanceSO._onDominanceChanged to refresh on zone state change.")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _progressLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _objectiveMetPanel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onDominanceChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD from the current objective progress state.
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

            int held     = _trackerSO.HeldZones;
            int required = _trackerSO.RequiredZones;

            if (_progressLabel != null)
                _progressLabel.text = $"{held} / {required} zones";

            if (_progressBar != null)
                _progressBar.value = _trackerSO.ProgressRatio;

            _objectiveMetPanel?.SetActive(_trackerSO.IsObjectiveMet);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound tracker SO (may be null).</summary>
        public ZoneObjectiveProgressTrackerSO TrackerSO => _trackerSO;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control replay step navigator HUD.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _stepLabel   → "Step N / M"  (1-based current step over total count)
    ///   _prevButton  → calls StepBackward() + Refresh
    ///   _nextButton  → calls StepForward()  + Refresh
    ///   _panel       → root panel; hidden when _replaySO is null or empty
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onReplayUpdated for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; button actions wired in Awake.
    ///   - DisallowMultipleComponent — one HUD per replay panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _replaySO        → ZoneControlReplaySO asset.
    ///   2. Assign _onReplayUpdated → ZoneControlReplaySO._onReplayUpdated channel.
    ///   3. Assign _stepLabel, _prevButton, _nextButton, _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlReplayHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlReplaySO _replaySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onReplayUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _stepLabel;
        [SerializeField] private Button     _prevButton;
        [SerializeField] private Button     _nextButton;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action         _refreshDelegate;
        private UnityEngine.Events.UnityAction _prevAction;
        private UnityEngine.Events.UnityAction _nextAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
            _prevAction      = StepBackward;
            _nextAction      = StepForward;

            _prevButton?.onClick.AddListener(_prevAction);
            _nextButton?.onClick.AddListener(_nextAction);
        }

        private void OnEnable()
        {
            _onReplayUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onReplayUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the replay cursor by one step and refreshes the HUD.
        /// Null-safe.
        /// </summary>
        public void StepForward()
        {
            _replaySO?.StepForward();
            Refresh();
        }

        /// <summary>
        /// Moves the replay cursor back by one step and refreshes the HUD.
        /// Null-safe.
        /// </summary>
        public void StepBackward()
        {
            _replaySO?.StepBackward();
            Refresh();
        }

        /// <summary>
        /// Rebuilds the HUD label from the current replay state.
        /// Hides the panel when <c>_replaySO</c> is null or has no frames.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_replaySO == null || _replaySO.Count == 0)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stepLabel != null)
                _stepLabel.text = $"Step {_replaySO.CurrentStep + 1} / {_replaySO.Count}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound replay SO (may be null).</summary>
        public ZoneControlReplaySO ReplaySO => _replaySO;
    }
}

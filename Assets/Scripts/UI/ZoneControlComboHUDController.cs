using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the live combo-streak HUD for zone-control mode.
    /// Subscribes to zone-capture and match-boundary events; ticks the combo expiry
    /// timer each frame; and refreshes labels when the combo state changes.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _comboCountLabel  → "Combo: N".
    ///   _multiplierLabel  → "xF2" (e.g. "x2.50").
    ///   _comboPanel       → Root panel; hidden when <c>_trackerSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one combo panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _trackerSO        → ZoneControlComboTrackerSO asset.
    ///   2. Assign _onZoneCaptured   → shared ZoneCaptured VoidGameEvent.
    ///   3. Assign _onComboUpdated   → trackerSO._onComboUpdated channel.
    ///   4. Assign _onMatchStarted   → shared MatchStarted VoidGameEvent.
    ///   5. Assign _onMatchEnded     → shared MatchEnded VoidGameEvent.
    ///   6. Assign label / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlComboHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlComboTrackerSO _trackerSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to a zone-captured event to record each capture.")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;

        [Tooltip("Wire to ZoneControlComboTrackerSO._onComboUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onComboUpdated;

        [Tooltip("Wire to the shared MatchStarted VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _comboCountLabel;
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private GameObject _comboPanel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleCaptureDelegate;
        private Action _refreshDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleCaptureDelegate      = HandleCapture;
            _refreshDelegate            = Refresh;
            _handleMatchStartedDelegate = HandleMatchBoundary;
            _handleMatchEndedDelegate   = HandleMatchBoundary;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onComboUpdated?.RegisterCallback(_refreshDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onComboUpdated?.UnregisterCallback(_refreshDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        private void Update()
        {
            _trackerSO?.Tick(Time.deltaTime);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Records a zone capture on the tracker SO and refreshes the HUD.</summary>
        public void HandleCapture()
        {
            _trackerSO?.RecordCapture();
            Refresh();
        }

        /// <summary>Resets the tracker on match start or end and refreshes the HUD.</summary>
        public void HandleMatchBoundary()
        {
            _trackerSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all HUD elements from the current tracker state.
        /// Hides the panel when <c>_trackerSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_trackerSO == null)
            {
                _comboPanel?.SetActive(false);
                return;
            }

            _comboPanel?.SetActive(true);

            if (_comboCountLabel != null)
                _comboCountLabel.text = $"Combo: {_trackerSO.ComboCount}";

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"x{_trackerSO.CurrentMultiplier:F2}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound combo-tracker SO (may be null).</summary>
        public ZoneControlComboTrackerSO TrackerSO => _trackerSO;
    }
}

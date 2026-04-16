using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates the player's capture pace against a target
    /// and displays advisory text ("On target", "Speed up", "Ease off") in a HUD label.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _adviceLabel → "On target" / "Speed up" / "Ease off"
    ///   _panel       → Root panel; hidden when <c>_paceSO</c> or <c>_trackerSO</c>
    ///                  is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one advisor panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchPaceAdvisorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchPaceSO    _paceSO;
        [SerializeField] private ZoneCapturePaceTrackerSO  _trackerSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when the tracker's pace changes; triggers EvaluatePace + Refresh.")]
        [SerializeField] private VoidGameEvent _onPaceUpdated;

        [Tooltip("Raised at match start; resets the pace SO and refreshes.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _adviceLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handlePaceUpdatedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handlePaceUpdatedDelegate  = HandlePaceUpdated;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onPaceUpdated?.RegisterCallback(_handlePaceUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPaceUpdated?.UnregisterCallback(_handlePaceUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current captures-per-minute from the tracker, evaluates pace,
        /// and refreshes the advisor HUD.
        /// </summary>
        public void HandlePaceUpdated()
        {
            if (_paceSO == null || _trackerSO == null) { Refresh(); return; }

            float rate = _trackerSO.GetCapturesPerMinute(UnityEngine.Time.time);
            _paceSO.EvaluatePace(rate);
            Refresh();
        }

        /// <summary>
        /// Resets the pace SO and refreshes the HUD at match start.
        /// </summary>
        public void HandleMatchStarted()
        {
            _paceSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD label from the current pace advice.
        /// Hides the panel when <c>_paceSO</c> or <c>_trackerSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_paceSO == null || _trackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_adviceLabel != null)
            {
                _adviceLabel.text = _paceSO.LastAdvice switch
                {
                    ZoneControlPaceAdvice.AheadOfPace => "Ease off",
                    ZoneControlPaceAdvice.BehindPace  => "Speed up",
                    _                                 => "On target"
                };
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound pace SO (may be null).</summary>
        public ZoneControlMatchPaceSO PaceSO => _paceSO;
    }
}

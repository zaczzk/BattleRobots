using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a one-page end-of-session full report panel
    /// combining career statistics, the current match rating, and difficulty advice.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _totalZonesLabel → "Zones: N"              (from ZoneControlSessionSummarySO)
    ///   _matchesLabel    → "Matches: N"            (from ZoneControlSessionSummarySO)
    ///   _ratingLabel     → "Rating: N / 5"         (from ZoneControlMatchRatingController)
    ///   _adviceLabel     → difficulty advice string (from ZoneControlDifficultyAdvisorController)
    ///   _panel           → Root panel; hidden when _summarySO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onMatchEnded</c> to trigger Refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one session report panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _summarySO         → ZoneControlSessionSummarySO asset.
    ///   2. Assign _ratingController  → ZoneControlMatchRatingController in the scene.
    ///   3. Assign _advisorController → ZoneControlDifficultyAdvisorController in the scene.
    ///   4. Assign _onMatchEnded      → shared MatchEnded VoidGameEvent.
    ///   5. Assign labels and _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSessionReportController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSessionSummarySO          _summarySO;

        [Header("Scene Refs (optional)")]
        [SerializeField] private ZoneControlMatchRatingController      _ratingController;
        [SerializeField] private ZoneControlDifficultyAdvisorController _advisorController;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _totalZonesLabel;
        [SerializeField] private Text       _matchesLabel;
        [SerializeField] private Text       _ratingLabel;
        [SerializeField] private Text       _adviceLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called when a match ends. Triggers a <see cref="Refresh"/>.
        /// Null-safe.
        /// </summary>
        public void HandleMatchEnded()
        {
            Refresh();
        }

        /// <summary>
        /// Rebuilds all report labels from the current SOs and controller state.
        /// Hides the panel when <c>_summarySO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_summarySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_totalZonesLabel != null)
                _totalZonesLabel.text = $"Zones: {_summarySO.TotalZonesCaptured}";

            if (_matchesLabel != null)
                _matchesLabel.text = $"Matches: {_summarySO.MatchesPlayed}";

            if (_ratingLabel != null)
            {
                int rating = _ratingController != null ? _ratingController.CurrentRating : 0;
                _ratingLabel.text = $"Rating: {rating} / 5";
            }

            if (_adviceLabel != null)
                _adviceLabel.text = _advisorController != null
                    ? _advisorController.ComputeAdvice()
                    : string.Empty;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;

        /// <summary>The bound rating controller (may be null).</summary>
        public ZoneControlMatchRatingController RatingController => _ratingController;

        /// <summary>The bound difficulty advisor controller (may be null).</summary>
        public ZoneControlDifficultyAdvisorController AdvisorController => _advisorController;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that reads a <see cref="ZoneControlSessionSummarySO"/> and
    /// <see cref="ZoneControlMatchRatingConfig"/> to produce a one-line difficulty
    /// advice string, recommending whether the player should increase or decrease
    /// zone count or capture pace targets based on recent performance.
    ///
    /// ── Advice tiers (based on computed 1–5 star rating) ──────────────────────
    ///   ★★★★★  →  "Excellent! Try increasing zone count."
    ///   ★★★★   →  "Good. Consider raising the capture pace target."
    ///   ★★★    →  "Solid. Maintain current settings."
    ///   ★★     →  "Try reducing zone count for consistency."
    ///   ★      →  "Start with fewer zones and slower capture targets."
    ///   No matches yet → configurable <c>_noMatchesMessage</c>
    ///   Null SO/config  → configurable <c>_noDataMessage</c>
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onSummaryUpdated</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one advisor panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _summarySO         → ZoneControlSessionSummarySO asset.
    ///   2. Assign _ratingConfig      → ZoneControlMatchRatingConfig asset.
    ///   3. Assign _onSummaryUpdated  → ZoneControlSessionSummarySO._onSummaryUpdated.
    ///   4. Assign _adviceLabel and _panel.
    ///   5. Optionally customise advice strings to match your game's tone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlDifficultyAdvisorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;
        [SerializeField] private ZoneControlMatchRatingConfig _ratingConfig;

        [Header("Advice Messages")]
        [Tooltip("Shown when _summarySO or _ratingConfig is null.")]
        [SerializeField] private string _noDataMessage      = "No data available.";

        [Tooltip("Shown when no matches have been recorded yet.")]
        [SerializeField] private string _noMatchesMessage   = "Play a match to receive advice.";

        [Tooltip("Advice for 5-star performance.")]
        [SerializeField] private string _highAdvice         = "Excellent! Try increasing zone count.";

        [Tooltip("Advice for 4-star performance.")]
        [SerializeField] private string _goodAdvice         = "Good. Consider raising the capture pace target.";

        [Tooltip("Advice for 3-star performance.")]
        [SerializeField] private string _averageAdvice      = "Solid. Maintain current settings.";

        [Tooltip("Advice for 2-star performance.")]
        [SerializeField] private string _belowAverageAdvice = "Try reducing zone count for consistency.";

        [Tooltip("Advice for 1-star performance.")]
        [SerializeField] private string _lowAdvice          = "Start with fewer zones and slower capture targets.";

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlSessionSummarySO._onSummaryUpdated.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _adviceLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleSummaryDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleSummaryDelegate = HandleSummaryUpdated;
        }

        private void OnEnable()
        {
            _onSummaryUpdated?.RegisterCallback(_handleSummaryDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onSummaryUpdated?.UnregisterCallback(_handleSummaryDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the session summary SO raises its update event.
        /// Triggers a <see cref="Refresh"/>.
        /// </summary>
        public void HandleSummaryUpdated()
        {
            Refresh();
        }

        /// <summary>
        /// Computes and returns the advice string based on current session statistics.
        /// Returns <c>_noDataMessage</c> when either SO or config is null.
        /// Returns <c>_noMatchesMessage</c> when no matches have been played yet.
        /// Zero allocation.
        /// </summary>
        public string ComputeAdvice()
        {
            if (_summarySO == null || _ratingConfig == null) return _noDataMessage;
            if (_summarySO.MatchesPlayed == 0)               return _noMatchesMessage;

            int rating = _ratingConfig.ComputeRating(
                _summarySO.TotalZonesCaptured,
                _summarySO.BestCaptureStreak,
                _summarySO.MatchesWithDominance);

            if (rating >= 5) return _highAdvice;
            if (rating >= 4) return _goodAdvice;
            if (rating >= 3) return _averageAdvice;
            if (rating >= 2) return _belowAverageAdvice;
            return _lowAdvice;
        }

        /// <summary>
        /// Refreshes the advice label with the latest computed advice.
        /// Hides the panel when <c>_summarySO</c> is null.
        /// Zero allocation.
        /// </summary>
        public void Refresh()
        {
            if (_summarySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_adviceLabel != null)
                _adviceLabel.text = ComputeAdvice();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;

        /// <summary>The bound match rating config (may be null).</summary>
        public ZoneControlMatchRatingConfig RatingConfig => _ratingConfig;
    }
}

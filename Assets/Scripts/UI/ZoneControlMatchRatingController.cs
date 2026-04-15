using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that computes and displays a 1–5 star match rating at the
    /// end of each match using <see cref="ZoneControlMatchRatingConfig"/> thresholds
    /// and career statistics from <see cref="ZoneControlSessionSummarySO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _ratingLabel → "N / 5"  (current numeric rating)
    ///   _starImages  → Array of up to 5 Image components; each is enabled when
    ///                  its index is less than the current rating (1-based stars).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onMatchEnded</c> for reactive computation.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one rating panel per match summary.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _summarySO     → ZoneControlSessionSummarySO asset.
    ///   2. Assign _ratingConfig  → ZoneControlMatchRatingConfig asset.
    ///   3. Assign _onMatchEnded  → the shared MatchEnded VoidGameEvent.
    ///   4. Assign _onRatingSet   → an output VoidGameEvent for downstream consumers.
    ///   5. Assign _ratingLabel and populate _starImages (up to 5 Image refs).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchRatingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;
        [SerializeField] private ZoneControlMatchRatingConfig _ratingConfig;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Raised after CurrentRating is set. Wire to downstream consumers.")]
        [SerializeField] private VoidGameEvent _onRatingSet;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text    _ratingLabel;
        [Tooltip("Up to 5 Image refs; each is enabled when its index < current rating.")]
        [SerializeField] private Image[] _starImages;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _currentRating;

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
        /// Computes the rating from the current session summary, updates the HUD,
        /// and raises <c>_onRatingSet</c>.
        /// No-op when <c>_summarySO</c> or <c>_ratingConfig</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_summarySO == null || _ratingConfig == null) return;

            _currentRating = _ratingConfig.ComputeRating(
                _summarySO.TotalZonesCaptured,
                _summarySO.BestCaptureStreak,
                _summarySO.MatchesWithDominance);

            Refresh();
            _onRatingSet?.Raise();
        }

        /// <summary>
        /// Rebuilds the rating label and star image states from <see cref="_currentRating"/>.
        /// Zero allocation.
        /// </summary>
        public void Refresh()
        {
            if (_ratingLabel != null)
                _ratingLabel.text = $"{_currentRating} / 5";

            if (_starImages == null) return;

            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] != null)
                    _starImages[i].enabled = i < _currentRating;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Most recently computed rating (0 until the first match ends).</summary>
        public int CurrentRating => _currentRating;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;

        /// <summary>The bound rating config (may be null).</summary>
        public ZoneControlMatchRatingConfig RatingConfig => _ratingConfig;
    }
}

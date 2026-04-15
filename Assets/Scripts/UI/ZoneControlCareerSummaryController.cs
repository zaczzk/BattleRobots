using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that aggregates zone-control career statistics from a
    /// <see cref="ZoneControlSessionSummarySO"/> and a
    /// <see cref="ZoneControlMatchRatingHistorySO"/> into a single post-session
    /// career card panel.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _totalZonesLabel  → "Total Zones: N"
    ///   _avgZonesLabel    → "Avg/Match: F"  (one decimal place)
    ///   _bestStreakLabel  → "Best Streak: N"
    ///   _ratingBadges    → Sparkline of up to 5 Image refs (oldest → newest);
    ///                       each badge:
    ///                         enabled = true   when a rating entry exists at that index
    ///                         color   = green  (rating ≥ 4)
    ///                                   yellow (rating == 3)
    ///                                   red    (rating ≤ 2)
    ///                       Badges beyond the history count are disabled.
    ///   _panel           → Root panel; hidden when _summarySO is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onSummaryUpdated</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one career summary panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _summarySO        → ZoneControlSessionSummarySO asset.
    ///   2. Assign _historySO        → ZoneControlMatchRatingHistorySO asset (optional).
    ///   3. Assign _onSummaryUpdated → ZoneControlSessionSummarySO._onSummaryUpdated.
    ///   4. Assign labels, _ratingBadges (up to 5 Images), and _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCareerSummaryController : MonoBehaviour
    {
        // ── Colour constants ──────────────────────────────────────────────────

        private static readonly Color ColourGood    = new Color(0.2f, 0.9f, 0.3f);
        private static readonly Color ColourAverage = new Color(0.9f, 0.8f, 0.2f);
        private static readonly Color ColourPoor    = new Color(0.9f, 0.2f, 0.2f);

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSessionSummarySO     _summarySO;
        [SerializeField] private ZoneControlMatchRatingHistorySO _historySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlSessionSummarySO._onSummaryUpdated.")]
        [SerializeField] private VoidGameEvent _onSummaryUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text    _totalZonesLabel;
        [SerializeField] private Text    _avgZonesLabel;
        [SerializeField] private Text    _bestStreakLabel;

        [Tooltip("Up to 5 Image refs; each shows the colour-coded rating for one match (oldest left).")]
        [SerializeField] private Image[] _ratingBadges;

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
            _onSummaryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onSummaryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all career stat labels and rating badge sparkline.
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
                _totalZonesLabel.text = $"Total Zones: {_summarySO.TotalZonesCaptured}";

            if (_avgZonesLabel != null)
                _avgZonesLabel.text = $"Avg/Match: {_summarySO.AverageZonesPerMatch:F1}";

            if (_bestStreakLabel != null)
                _bestStreakLabel.text = $"Best Streak: {_summarySO.BestCaptureStreak}";

            RefreshBadges();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void RefreshBadges()
        {
            if (_ratingBadges == null) return;

            var ratings = _historySO != null ? _historySO.GetRatings() : null;
            int count   = ratings != null ? ratings.Count : 0;

            for (int i = 0; i < _ratingBadges.Length; i++)
            {
                var badge = _ratingBadges[i];
                if (badge == null) continue;

                if (i < count)
                {
                    badge.enabled = true;
                    badge.color   = RatingToColor(ratings[i]);
                }
                else
                {
                    badge.enabled = false;
                }
            }
        }

        private static Color RatingToColor(int rating)
        {
            if (rating >= 4) return ColourGood;
            if (rating >= 3) return ColourAverage;
            return ColourPoor;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;

        /// <summary>The bound rating history SO (may be null).</summary>
        public ZoneControlMatchRatingHistorySO HistorySO => _historySO;
    }
}

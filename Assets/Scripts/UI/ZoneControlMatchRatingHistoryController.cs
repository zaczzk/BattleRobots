using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records each match rating into a
    /// <see cref="ZoneControlMatchRatingHistorySO"/> ring buffer and renders the
    /// history as a row of coloured star-badge Images (sparkline style).
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _ratingBadges → Array of up to 5 Image refs (one slot per history entry).
    ///                   Each badge is enabled and coloured by its star count:
    ///                     1–2 stars → red   (0.9, 0.2, 0.2)
    ///                     3 stars   → yellow(0.9, 0.8, 0.2)
    ///                     4–5 stars → green (0.2, 0.9, 0.3)
    ///                   Unused slots are disabled (alpha 0 / not enabled).
    ///   _panel        → Root container; hidden when <c>_historySO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onRatingSet</c> to record the latest rating from
    ///     <c>_ratingController.CurrentRating</c> into the history SO.
    ///   - Subscribes to <c>_onRatingHistoryUpdated</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one history panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _historySO          → ZoneControlMatchRatingHistorySO asset.
    ///   2. Assign _ratingController   → ZoneControlMatchRatingController in scene.
    ///   3. Assign _onRatingSet        → ZoneControlMatchRatingController._onRatingSet.
    ///   4. Assign _onRatingHistoryUpdated → historySO._onRatingHistoryUpdated channel.
    ///   5. Populate _ratingBadges (up to 5 Image refs) and assign _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchRatingHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchRatingHistorySO  _historySO;
        [SerializeField] private ZoneControlMatchRatingController _ratingController;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlMatchRatingController._onRatingSet.")]
        [SerializeField] private VoidGameEvent _onRatingSet;

        [Tooltip("Wire to ZoneControlMatchRatingHistorySO._onRatingHistoryUpdated.")]
        [SerializeField] private VoidGameEvent _onRatingHistoryUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("Up to 5 Image refs. Colour reflects 1-2=red, 3=yellow, 4-5=green.")]
        [SerializeField] private Image[]    _ratingBadges;
        [SerializeField] private GameObject _panel;

        // ── Badge colours ─────────────────────────────────────────────────────

        private static readonly Color ColorLow  = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color ColorMid  = new Color(0.9f, 0.8f, 0.2f);
        private static readonly Color ColorHigh = new Color(0.2f, 0.9f, 0.3f);

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleRatingSetDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleRatingSetDelegate = HandleRatingSet;
            _refreshDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onRatingSet?.RegisterCallback(_handleRatingSetDelegate);
            _onRatingHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onRatingSet?.UnregisterCallback(_handleRatingSetDelegate);
            _onRatingHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current rating from <c>_ratingController</c> and records it
        /// in <c>_historySO</c>.
        /// No-op when either reference is null.
        /// </summary>
        public void HandleRatingSet()
        {
            if (_historySO == null || _ratingController == null) return;
            _historySO.AddRating(_ratingController.CurrentRating);
        }

        /// <summary>
        /// Rebuilds the badge sparkline from the current history SO state.
        /// Hides the panel when <c>_historySO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_historySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ratingBadges == null) return;

            var ratings = _historySO.GetRatings();

            for (int i = 0; i < _ratingBadges.Length; i++)
            {
                if (_ratingBadges[i] == null) continue;

                if (i < ratings.Count)
                {
                    _ratingBadges[i].enabled = true;
                    _ratingBadges[i].color   = RatingToColor(ratings[i]);
                }
                else
                {
                    _ratingBadges[i].enabled = false;
                }
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static Color RatingToColor(int rating)
        {
            if (rating >= 4) return ColorHigh;
            if (rating >= 3) return ColorMid;
            return ColorLow;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound history SO (may be null).</summary>
        public ZoneControlMatchRatingHistorySO HistorySO => _historySO;

        /// <summary>The bound rating controller (may be null).</summary>
        public ZoneControlMatchRatingController RatingController => _ratingController;
    }
}

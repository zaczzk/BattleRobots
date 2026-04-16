using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control league HUD.
    /// Listens for a post-match rating event, submits points to the
    /// <see cref="ZoneControlLeagueSO"/>, and refreshes the division display.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _divisionLabel    → CurrentDivision.ToString() (e.g. "Gold").
    ///   _pointsLabel      → "Points: N".
    ///   _promotionLabel   → "Next: N pts"  /  "Max Division!".
    ///   _panel            → Root panel; hidden when <c>_leagueSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one league panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _leagueSO              → ZoneControlLeagueSO asset.
    ///   2. Assign _ratingController      → ZoneControlMatchRatingController in scene.
    ///   3. Assign _onRatingSet           → ZoneControlMatchRatingController._onRatingSet.
    ///   4. Assign _onLeagueUpdated       → leagueSO._onLeagueUpdated channel.
    ///   5. Assign _onPromotion/_onRelegation → leagueSO channels (optional).
    ///   6. Assign label / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlLeagueController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlLeagueSO _leagueSO;

        [Header("Scene Refs (optional)")]
        [Tooltip("Used to read CurrentRating when a rating-set event fires.")]
        [SerializeField] private ZoneControlMatchRatingController _ratingController;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlMatchRatingController._onRatingSet.")]
        [SerializeField] private VoidGameEvent _onRatingSet;

        [Tooltip("Wire to ZoneControlLeagueSO._onLeagueUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onLeagueUpdated;

        [Tooltip("Wire to ZoneControlLeagueSO._onPromotion for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onPromotion;

        [Tooltip("Wire to ZoneControlLeagueSO._onRelegation for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onRelegation;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _divisionLabel;
        [SerializeField] private Text _pointsLabel;
        [SerializeField] private Text _promotionLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

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
            _onLeagueUpdated?.RegisterCallback(_refreshDelegate);
            _onPromotion?.RegisterCallback(_refreshDelegate);
            _onRelegation?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onRatingSet?.UnregisterCallback(_handleRatingSetDelegate);
            _onLeagueUpdated?.UnregisterCallback(_refreshDelegate);
            _onPromotion?.UnregisterCallback(_refreshDelegate);
            _onRelegation?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current match rating and submits rating points to the league SO.
        /// No-op when either <c>_leagueSO</c> or <c>_ratingController</c> is null.
        /// </summary>
        public void HandleRatingSet()
        {
            if (_leagueSO == null || _ratingController == null)
            {
                Refresh();
                return;
            }

            _leagueSO.AddRatingPoints(_ratingController.CurrentRating);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all HUD elements from the current league SO state.
        /// Hides the panel when <c>_leagueSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_leagueSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_divisionLabel != null)
                _divisionLabel.text = _leagueSO.CurrentDivision.ToString();

            if (_pointsLabel != null)
                _pointsLabel.text = $"Points: {_leagueSO.CurrentPoints}";

            if (_promotionLabel != null)
            {
                bool atMax = _leagueSO.CurrentDivision == ZoneControlLeagueDivision.Platinum;
                _promotionLabel.text = atMax
                    ? "Max Division!"
                    : $"Next: {_leagueSO.PromotionThreshold - _leagueSO.CurrentPoints} pts";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound league SO (may be null).</summary>
        public ZoneControlLeagueSO LeagueSO => _leagueSO;

        /// <summary>The bound match-rating controller (may be null).</summary>
        public ZoneControlMatchRatingController RatingController => _ratingController;
    }
}

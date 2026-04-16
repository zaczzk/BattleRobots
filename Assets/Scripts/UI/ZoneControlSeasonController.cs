using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control season HUD.
    /// Listens for an external "end season" trigger, records the current league
    /// division into <see cref="ZoneControlSeasonSO"/>, and refreshes the display.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _seasonCountLabel     → "Season: N".
    ///   _highestDivisionLabel → "Best: Division" (e.g. "Best: Gold").
    ///   _rewardTierLabel      → "Reward Tier N" (based on most recent season).
    ///   _panel                → Root panel; hidden when <c>_seasonSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one season panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _seasonSO              → ZoneControlSeasonSO asset.
    ///   2. Assign _leagueSO              → ZoneControlLeagueSO asset (read division).
    ///   3. Assign _onEndSeasonTriggered  → a VoidGameEvent raised to conclude a season.
    ///   4. Assign _onSeasonUpdated       → seasonSO._onSeasonUpdated channel.
    ///   5. Assign label / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSeasonController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSeasonSO _seasonSO;

        [Header("Scene Refs (optional)")]
        [Tooltip("Read CurrentDivision when a season ends.")]
        [SerializeField] private ZoneControlLeagueSO _leagueSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised externally to trigger end-of-season processing.")]
        [SerializeField] private VoidGameEvent _onEndSeasonTriggered;

        [Tooltip("Wire to ZoneControlSeasonSO._onSeasonUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onSeasonUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text _seasonCountLabel;
        [SerializeField] private Text _highestDivisionLabel;
        [SerializeField] private Text _rewardTierLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleEndSeasonDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleEndSeasonDelegate = HandleEndSeason;
            _refreshDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onEndSeasonTriggered?.RegisterCallback(_handleEndSeasonDelegate);
            _onSeasonUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onEndSeasonTriggered?.UnregisterCallback(_handleEndSeasonDelegate);
            _onSeasonUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Concludes the current season using <see cref="ZoneControlLeagueSO.CurrentDivision"/>.
        /// No-op when <c>_seasonSO</c> is null.
        /// </summary>
        public void HandleEndSeason()
        {
            if (_seasonSO == null)
                return;

            ZoneControlLeagueDivision div = _leagueSO != null
                ? _leagueSO.CurrentDivision
                : ZoneControlLeagueDivision.Bronze;

            _seasonSO.EndSeason(div);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all HUD elements from the current season SO state.
        /// Hides the panel when <c>_seasonSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_seasonSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_seasonCountLabel != null)
                _seasonCountLabel.text = $"Season: {_seasonSO.SeasonCount}";

            if (_highestDivisionLabel != null)
                _highestDivisionLabel.text = $"Best: {_seasonSO.HighestDivision}";

            if (_rewardTierLabel != null)
                _rewardTierLabel.text = $"Reward Tier {_seasonSO.LatestRewardTier}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound season SO (may be null).</summary>
        public ZoneControlSeasonSO SeasonSO => _seasonSO;

        /// <summary>The bound league SO (may be null).</summary>
        public ZoneControlLeagueSO LeagueSO => _leagueSO;
    }
}

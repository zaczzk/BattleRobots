using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's all-time single-match career highlights on a UI panel.
    ///
    /// ── Data source ───────────────────────────────────────────────────────────
    ///   Reads from an optional <see cref="CareerHighlightsSO"/> and refreshes when
    ///   <c>_onHighlightsUpdated</c> fires (raised by CareerHighlightsSO after each
    ///   match where a new category best is set).
    ///
    /// ── Displayed categories ──────────────────────────────────────────────────
    ///   • Best single-match damage dealt   → _bestDamageText   e.g. "Best Damage: 347"
    ///   • Fastest winning match duration   → _fastestWinText   e.g. "Fastest Win: 1m 23s"
    ///   • Highest currency in one match    → _bestCurrencyText e.g. "Best Payout: 350"
    ///   • Longest match duration played    → _longestMatchText e.g. "Longest Match: 2m 00s"
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to the career-highlights panel (Main Menu or Career screen).
    ///   2. Assign _highlights → the CareerHighlightsSO asset.
    ///   3. Assign _onHighlightsUpdated → the VoidGameEvent SO wired on CareerHighlightsSO.
    ///   4. Assign any subset of the four optional Text labels.
    ///   5. Optionally assign _newHighlightBanner — shown when any IsNew* flag is true
    ///      immediately after a match where a record was broken.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core only. No Physics refs.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegate cached in Awake — zero alloc on Subscribe/Unsubscribe.
    ///   - String allocations in Refresh() only on the cold event-response path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CareerHighlightsController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("SO tracking all-time single-match bests. " +
                 "Leave null to show '—' on all labels.")]
        [SerializeField] private CareerHighlightsSO _highlights;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by CareerHighlightsSO after any record is broken. " +
                 "Triggers Refresh(). Leave null — OnEnable still calls Refresh() once.")]
        [SerializeField] private VoidGameEvent _onHighlightsUpdated;

        [Header("Labels (all optional)")]
        [Tooltip("e.g. 'Best Damage: 347'. Populated from BestSingleMatchDamage.")]
        [SerializeField] private Text _bestDamageText;

        [Tooltip("e.g. 'Fastest Win: 1m 23s'. Shows '—' if the player has never won.")]
        [SerializeField] private Text _fastestWinText;

        [Tooltip("e.g. 'Best Payout: 350'. Populated from BestSingleMatchCurrency.")]
        [SerializeField] private Text _bestCurrencyText;

        [Tooltip("e.g. 'Longest Match: 2m 00s'. Shows '—' if no match played yet.")]
        [SerializeField] private Text _longestMatchText;

        [Header("New Highlight Banner (optional)")]
        [Tooltip("Activated when any IsNew* flag is true on CareerHighlightsSO " +
                 "(i.e. a record was broken in the most recent match). " +
                 "Leave null to skip.")]
        [SerializeField] private GameObject _newHighlightBanner;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onHighlightsUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHighlightsUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="CareerHighlightsSO"/> state and updates all
        /// label texts.  Called on OnEnable and each time <c>_onHighlightsUpdated</c> fires.
        /// Safe to call with null _highlights — every label shows '—'.
        /// </summary>
        public void Refresh()
        {
            if (_highlights == null)
            {
                if (_bestDamageText   != null) _bestDamageText.text   = "Best Damage: \u2014";
                if (_fastestWinText   != null) _fastestWinText.text   = "Fastest Win: \u2014";
                if (_bestCurrencyText != null) _bestCurrencyText.text = "Best Payout: \u2014";
                if (_longestMatchText != null) _longestMatchText.text = "Longest Match: \u2014";
                if (_newHighlightBanner != null) _newHighlightBanner.SetActive(false);
                return;
            }

            if (_bestDamageText != null)
                _bestDamageText.text = string.Format("Best Damage: {0:F0}",
                    _highlights.BestSingleMatchDamage);

            if (_fastestWinText != null)
            {
                _fastestWinText.text = _highlights.FastestWinSeconds > 0f
                    ? string.Format("Fastest Win: {0}", FormatDuration(_highlights.FastestWinSeconds))
                    : "Fastest Win: \u2014";
            }

            if (_bestCurrencyText != null)
                _bestCurrencyText.text = string.Format("Best Payout: {0}",
                    _highlights.BestSingleMatchCurrency);

            if (_longestMatchText != null)
            {
                _longestMatchText.text = _highlights.LongestMatchSeconds > 0f
                    ? string.Format("Longest Match: {0}", FormatDuration(_highlights.LongestMatchSeconds))
                    : "Longest Match: \u2014";
            }

            if (_newHighlightBanner != null)
            {
                bool anyNew = _highlights.IsNewBestDamage
                           || _highlights.IsNewFastestWin
                           || _highlights.IsNewBestCurrency
                           || _highlights.IsNewLongestMatch;
                _newHighlightBanner.SetActive(anyNew);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Formats a duration as "Xm YYs" (when ≥ 60 s) or "Ys" (when &lt; 60 s).
        /// </summary>
        private static string FormatDuration(float seconds)
        {
            int totalSecs = Mathf.FloorToInt(seconds);
            int mins      = totalSecs / 60;
            int secs      = totalSecs % 60;
            return mins > 0
                ? string.Format("{0}m {1:00}s", mins, secs)
                : string.Format("{0}s", secs);
        }
    }
}

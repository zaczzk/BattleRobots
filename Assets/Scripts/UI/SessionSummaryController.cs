using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the current session's aggregate match statistics on a UI panel.
    ///
    /// ── Data source ───────────────────────────────────────────────────────────
    ///   Reads from an optional <see cref="SessionSummarySO"/> and refreshes when
    ///   <c>_onSessionUpdated</c> fires (raised by SessionSummarySO after each match
    ///   via <see cref="SessionSummarySO.RecordMatch"/>).
    ///
    /// ── Displayed statistics ──────────────────────────────────────────────────
    ///   • _matchesPlayedText  — total matches played this session  (e.g. "3")
    ///   • _winsText           — total wins this session            (e.g. "2")
    ///   • _winRateText        — session win rate as percentage      (e.g. "67%")
    ///   • _currencyEarnedText — total currency earned this session (e.g. "750")
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to the session-summary panel (Main Menu or career screen).
    ///   2. Assign _sessionSummary → the SessionSummarySO asset.
    ///   3. Assign _onSessionUpdated → the VoidGameEvent SO wired on SessionSummarySO.
    ///   4. Assign any subset of the four optional Text labels.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core only. No Physics refs.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegate cached in Awake — zero alloc on Subscribe/Unsubscribe.
    ///   - String allocations in Refresh() only on the cold event-response path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionSummaryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("SO tracking session-scoped match statistics. " +
                 "Leave null to show '—' on all labels.")]
        [SerializeField] private SessionSummarySO _sessionSummary;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by SessionSummarySO after each RecordMatch() call. " +
                 "Triggers Refresh(). Leave null — OnEnable still calls Refresh() once.")]
        [SerializeField] private VoidGameEvent _onSessionUpdated;

        [Header("Labels (all optional)")]
        [Tooltip("Displays total matches played this session (e.g. \"3\").")]
        [SerializeField] private Text _matchesPlayedText;

        [Tooltip("Displays total wins this session (e.g. \"2\").")]
        [SerializeField] private Text _winsText;

        [Tooltip("Displays the session win rate as a percentage (e.g. \"67%\").")]
        [SerializeField] private Text _winRateText;

        [Tooltip("Displays total currency earned this session (e.g. \"750\").")]
        [SerializeField] private Text _currencyEarnedText;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onSessionUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onSessionUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="SessionSummarySO"/> state and updates all label
        /// texts.  Called on OnEnable and each time <c>_onSessionUpdated</c> fires.
        /// Safe to call with null <c>_sessionSummary</c> — every label shows '—'.
        /// </summary>
        public void Refresh()
        {
            if (_sessionSummary == null)
            {
                if (_matchesPlayedText  != null) _matchesPlayedText.text  = "\u2014";
                if (_winsText           != null) _winsText.text           = "\u2014";
                if (_winRateText        != null) _winRateText.text        = "\u2014";
                if (_currencyEarnedText != null) _currencyEarnedText.text = "\u2014";
                return;
            }

            if (_matchesPlayedText  != null)
                _matchesPlayedText.text  = _sessionSummary.MatchesPlayed.ToString();

            if (_winsText != null)
                _winsText.text = _sessionSummary.Wins.ToString();

            if (_winRateText != null)
                _winRateText.text = _sessionSummary.WinRatePercent;

            if (_currencyEarnedText != null)
                _currencyEarnedText.text = _sessionSummary.TotalCurrencyEarned.ToString();
        }
    }
}

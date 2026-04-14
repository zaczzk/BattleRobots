using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the time remaining until the daily challenge resets at midnight UTC,
    /// or "Challenge complete!" when the daily challenge has already been completed today.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="DailyChallengeManager"/> marks the challenge complete and raises
    ///      <c>_onMatchEnded</c> (or the controller subscribes <c>_onChallengeCompleted</c>
    ///      via <c>_onMatchEnded</c>).
    ///   2. This controller refreshes on that event to reflect the updated state.
    ///   3. <see cref="Refresh"/> also runs on <c>OnEnable</c> for immediate display.
    ///
    /// ── Display rules ─────────────────────────────────────────────────────────
    ///   • <c>_challenge</c> is null → panel hidden.
    ///   • <c>IsCompleted</c> is true → label shows "Challenge complete!".
    ///   • Otherwise → label shows "Resets in Xh Ym" (hours/minutes until midnight UTC).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one countdown label per canvas.
    ///   - All inspector fields optional — safe with no refs assigned.
    ///   - No Update/FixedUpdate — purely event-driven (refreshes on match end).
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _challenge       → shared DailyChallengeSO asset.
    ///   _onMatchEnded    → VoidGameEvent raised by MatchManager at match end.
    ///   _countdownLabel  → Text component showing the time-remaining string.
    ///   _countdownPanel  → optional container shown when challenge is assigned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DailyChallengeCountdownController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("DailyChallengeSO providing the IsCompleted flag. " +
                 "Leave null to hide the countdown panel entirely.")]
        [SerializeField] private DailyChallengeSO _challenge;

        // ── Inspector — Event ─────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager at match end. " +
                 "Triggers Refresh() to update the countdown display. " +
                 "Leave null — OnEnable still calls Refresh() once.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Text label receiving 'Resets in Xh Ym' or 'Challenge complete!'.")]
        [SerializeField] private Text _countdownLabel;

        [Tooltip("Optional container panel. Shown when _challenge is assigned; " +
                 "hidden when _challenge is null.")]
        [SerializeField] private GameObject _countdownPanel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _countdownPanel?.SetActive(false);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates <c>_countdownLabel</c> and <c>_countdownPanel</c> to reflect the
        /// current daily challenge state.
        ///
        /// Called on <c>OnEnable</c> and each time <c>_onMatchEnded</c> fires.
        /// Safe to call with null <c>_challenge</c>.
        /// </summary>
        public void Refresh()
        {
            if (_challenge == null)
            {
                _countdownPanel?.SetActive(false);
                return;
            }

            _countdownPanel?.SetActive(true);

            if (_countdownLabel == null) return;

            if (_challenge.IsCompleted)
            {
                _countdownLabel.text = "Challenge complete!";
                return;
            }

            // Compute hours and minutes until the next midnight UTC reset.
            float secondsLeft = SecondsUntilMidnightUtc();
            int totalSeconds  = Mathf.Max(0, Mathf.RoundToInt(secondsLeft));
            int hours         = totalSeconds / 3600;
            int minutes       = (totalSeconds % 3600) / 60;

            _countdownLabel.text = string.Format("Resets in {0}h {1}m", hours, minutes);
        }

        /// <summary>
        /// Returns the number of seconds remaining until the next midnight UTC.
        /// Exposed as <c>public static</c> so tests can verify the format without
        /// depending on the exact wall-clock time.
        /// </summary>
        public static float SecondsUntilMidnightUtc()
        {
            DateTime now      = DateTime.UtcNow;
            DateTime midnight = now.Date.AddDays(1);
            return (float)(midnight - now).TotalSeconds;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="DailyChallengeSO"/>. May be null.</summary>
        public DailyChallengeSO Challenge => _challenge;
    }
}

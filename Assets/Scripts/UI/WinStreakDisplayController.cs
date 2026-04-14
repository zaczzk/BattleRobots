using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's current and all-time best win streak on a UI panel.
    /// Also shows an optional "notable streak" badge when the current streak reaches
    /// or exceeds a configurable threshold.
    ///
    /// ── Data source ───────────────────────────────────────────────────────────
    ///   Reads from an optional <see cref="WinStreakSO"/> and refreshes when
    ///   <c>_onStreakChanged</c> fires (raised after every win or loss).
    ///
    /// ── Displayed fields ──────────────────────────────────────────────────────
    ///   • _currentStreakText  — e.g. "Streak: 3"
    ///   • _bestStreakText     — e.g. "Best: 5"
    ///   • _streakBadge       — GameObject activated when CurrentStreak ≥ threshold
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to the arena HUD or post-match panel.
    ///   2. Assign _winStreak → the shared WinStreakSO asset.
    ///   3. Assign _onStreakChanged → the VoidGameEvent wired to WinStreakSO.
    ///   4. Assign any subset of the optional Text labels and the optional badge GO.
    ///   5. Set _notableStreakThreshold to the streak count that should show the badge
    ///      (default 3).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core only. No Physics refs.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegate cached in Awake — zero alloc on Subscribe/Unsubscribe.
    ///   - String allocations in Refresh() only on the cold event-response path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WinStreakDisplayController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("WinStreakSO tracking the current and best win streaks. " +
                 "Leave null to show '—' on all labels.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by WinStreakSO after every RecordWin() / RecordLoss() call. " +
                 "Triggers Refresh(). Leave null — OnEnable still calls Refresh() once.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        [Header("Labels (all optional)")]
        [Tooltip("Displays the current consecutive win streak, e.g. 'Streak: 3'.")]
        [SerializeField] private Text _currentStreakText;

        [Tooltip("Displays the all-time best win streak, e.g. 'Best: 5'.")]
        [SerializeField] private Text _bestStreakText;

        [Header("Streak Badge (optional)")]
        [Tooltip("GameObject shown when CurrentStreak ≥ _notableStreakThreshold. " +
                 "Hidden when the streak is below the threshold or _winStreak is null.")]
        [SerializeField] private GameObject _streakBadge;

        [Tooltip("Minimum consecutive wins required to show the streak badge.")]
        [SerializeField, Min(1)] private int _notableStreakThreshold = 3;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onStreakChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onStreakChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="WinStreakSO"/> state and updates all labels
        /// and the optional badge.  Called on OnEnable and each time
        /// <c>_onStreakChanged</c> fires.
        /// Safe to call with null <c>_winStreak</c> — every label shows '—'.
        /// </summary>
        public void Refresh()
        {
            if (_winStreak == null)
            {
                if (_currentStreakText != null) _currentStreakText.text = "Streak: \u2014";
                if (_bestStreakText    != null) _bestStreakText.text    = "Best: \u2014";
                if (_streakBadge      != null) _streakBadge.SetActive(false);
                return;
            }

            if (_currentStreakText != null)
                _currentStreakText.text = string.Format("Streak: {0}", _winStreak.CurrentStreak);

            if (_bestStreakText != null)
                _bestStreakText.text = string.Format("Best: {0}", _winStreak.BestStreak);

            if (_streakBadge != null)
                _streakBadge.SetActive(_winStreak.CurrentStreak >= _notableStreakThreshold);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="WinStreakSO"/>. May be null.</summary>
        public WinStreakSO WinStreak => _winStreak;

        /// <summary>Minimum streak count required to show the streak badge (≥ 1).</summary>
        public int NotableStreakThreshold => _notableStreakThreshold;
    }
}

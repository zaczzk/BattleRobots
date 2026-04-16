using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that evaluates capture-streak milestones as the streak
    /// changes and displays the current streak and next milestone in a HUD label.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _streakLabel       → "Streak: N"
    ///   _nextMilestoneLabel → "Next: N" / "Max!"
    ///   _panel             → Root panel; hidden when <c>_rewardSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one streak-reward panel per HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStreakRewardController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStreakRewardSO _rewardSO;
        [SerializeField] private ZoneCaptureStreakSO              _streakSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when the streak changes; triggers EvaluateStreak + Refresh.")]
        [SerializeField] private VoidGameEvent _onStreakUpdated;

        [Tooltip("Raised at match start; resets the reward SO and refreshes.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised by ZoneControlCaptureStreakRewardSO when a milestone is crossed.")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _nextMilestoneLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleStreakUpdatedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleStreakUpdatedDelegate = HandleStreakUpdated;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onStreakUpdated?.RegisterCallback(_handleStreakUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onStreakUpdated?.UnregisterCallback(_handleStreakUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current streak from <c>_streakSO</c>, evaluates milestones,
        /// credits any rewards to the wallet, and refreshes the HUD.
        /// </summary>
        public void HandleStreakUpdated()
        {
            if (_rewardSO == null) { Refresh(); return; }

            int streak = _streakSO != null ? _streakSO.CurrentStreak : 0;
            int before = _rewardSO.TotalRewardAwarded;
            _rewardSO.EvaluateStreak(streak);
            int earned = _rewardSO.TotalRewardAwarded - before;
            if (earned > 0)
                _wallet?.AddFunds(earned);

            Refresh();
        }

        /// <summary>
        /// Resets the reward SO and refreshes the HUD at match start.
        /// </summary>
        public void HandleMatchStarted()
        {
            _rewardSO?.Reset();
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD labels from the current streak and next milestone.
        /// Hides the panel when <c>_rewardSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_rewardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int streak = _streakSO != null ? _streakSO.CurrentStreak : 0;
            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {streak}";

            if (_nextMilestoneLabel != null)
            {
                int next = _rewardSO.NextMilestone;
                _nextMilestoneLabel.text = next < 0 ? "Max!" : $"Next: {next}";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound capture-streak reward SO (may be null).</summary>
        public ZoneControlCaptureStreakRewardSO RewardSO => _rewardSO;
    }
}

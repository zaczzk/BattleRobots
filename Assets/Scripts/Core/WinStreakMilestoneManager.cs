using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that grants credit rewards when the player's consecutive win
    /// streak reaches configured milestone targets.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. OnEnable  : subscribes <see cref="HandleStreakChanged"/> to
    ///      <c>_onStreakChanged</c> (the same event fired by <see cref="WinStreakSO"/>).
    ///   2. HandleStreakChanged : reads <see cref="WinStreakSO.CurrentStreak"/>;
    ///      skips silently when streak ≤ 0 (fired on a loss / reset);
    ///      looks up matching <see cref="WinStreakMilestoneEntry"/> items from the config;
    ///      calls <see cref="PlayerWallet.AddFunds"/> for each; persists via the
    ///      load → mutate → save pattern; raises <c>_onMilestoneReached</c>; and
    ///      optionally enqueues toast notifications.
    ///   3. OnDisable : unsubscribes to prevent stale callbacks.
    ///
    /// ── Repeatability ─────────────────────────────────────────────────────────
    ///   Because a player's streak resets to 0 on a loss, the same milestone
    ///   (e.g. "3-win streak") can be reached multiple times across a session.
    ///   The manager rewards the player each time — making streaks an ongoing
    ///   engagement incentive, not a one-time achievement.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI namespace references.
    ///   - HandleStreakChanged fires during <see cref="MatchManager.EndMatch"/>
    ///     (via WinStreakSO.RecordWin → _onStreakChanged.Raise()).
    ///   - All optional fields are null-safe; leave unassigned to skip that behaviour.
    ///   - No Update / FixedUpdate — purely event-driven (cold path only).
    ///   - The <see cref="System.Action"/> delegate is cached in Awake — zero alloc.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to the Bootstrap or Arena scene.
    ///   2. Assign <c>_milestoneConfig</c> (WinStreakMilestoneSO) and
    ///      <c>_winStreak</c> (WinStreakSO).
    ///   3. Assign <c>_onStreakChanged</c> → the same VoidGameEvent wired to
    ///      <see cref="WinStreakSO._onStreakChanged"/>.
    ///   4. Assign <c>_playerWallet</c> and optionally <c>_notificationQueue</c>.
    ///   5. Optionally assign <c>_onMilestoneReached</c> → a VoidGameEvent for
    ///      audio / UI fanfare.
    /// </summary>
    public sealed class WinStreakMilestoneManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Milestone Catalog")]
        [Tooltip("SO defining which streak counts grant credit rewards and how much.")]
        [SerializeField] private WinStreakMilestoneSO _milestoneConfig;

        [Header("Win Streak")]
        [Tooltip("Runtime SO tracking the player's current and best win streaks. " +
                 "CurrentStreak is read when _onStreakChanged fires.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Header("Economy")]
        [Tooltip("Wallet credited when a milestone reward is applied.")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Notifications (optional)")]
        [Tooltip("When assigned, a toast is enqueued for each reward entry that has " +
                 "a non-empty DisplayName. Leave null to skip toast notifications.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by WinStreakSO on RecordWin() and RecordLoss(). " +
                 "Wire to the same SO as WinStreakSO._onStreakChanged.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after all rewards for the current streak milestone have been applied. " +
                 "Leave null to skip. Wire to audio, analytics, or UI systems.")]
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        // ── Cached delegate ───────────────────────────────────────────────────

        private System.Action _handleStreakChangedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleStreakChangedDelegate = HandleStreakChanged;
        }

        private void OnEnable()
        {
            _onStreakChanged?.RegisterCallback(_handleStreakChangedDelegate);
        }

        private void OnDisable()
        {
            _onStreakChanged?.UnregisterCallback(_handleStreakChangedDelegate);
        }

        // ── Internal handler ──────────────────────────────────────────────────

        private void HandleStreakChanged()
        {
            if (_milestoneConfig == null || _winStreak == null) return;

            int streak = _winStreak.CurrentStreak;

            // Streak resets to 0 on a loss — skip the reward pass silently.
            if (streak <= 0) return;

            IReadOnlyList<WinStreakMilestoneEntry> rewards =
                _milestoneConfig.GetRewardsForStreak(streak);

            if (rewards.Count == 0) return;

            int totalGranted = 0;

            for (int i = 0; i < rewards.Count; i++)
            {
                WinStreakMilestoneEntry entry = rewards[i];

                if (entry.rewardCredits > 0)
                {
                    _playerWallet?.AddFunds(entry.rewardCredits);
                    totalGranted += entry.rewardCredits;
                }

                // Enqueue optional toast when a display name is configured.
                if (_notificationQueue != null
                    && !string.IsNullOrWhiteSpace(entry.displayName))
                {
                    _notificationQueue.Enqueue(
                        entry.displayName,
                        entry.rewardCredits > 0
                            ? $"+ {entry.rewardCredits} credits"
                            : string.Empty);
                }
            }

            // Persist the updated wallet balance so the reward survives the session.
            if (totalGranted > 0)
                PersistWallet();

            _onMilestoneReached?.Raise();

            Debug.Log($"[WinStreakMilestoneManager] Streak {streak} reached — " +
                      $"granted {rewards.Count} reward(s) totalling {totalGranted} credits.");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void PersistWallet()
        {
            if (_playerWallet == null) return;
            SaveData save = SaveSystem.Load();
            save.walletBalance = _playerWallet.Balance;
            SaveSystem.Save(save);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_milestoneConfig == null)
                Debug.LogWarning("[WinStreakMilestoneManager] _milestoneConfig not assigned — " +
                                 "no streak milestone rewards will be granted.", this);
            if (_winStreak == null)
                Debug.LogWarning("[WinStreakMilestoneManager] _winStreak not assigned — " +
                                 "current streak cannot be read when the event fires.", this);
            if (_onStreakChanged == null)
                Debug.LogWarning("[WinStreakMilestoneManager] _onStreakChanged not assigned — " +
                                 "HandleStreakChanged will never fire.", this);
        }
#endif
    }
}

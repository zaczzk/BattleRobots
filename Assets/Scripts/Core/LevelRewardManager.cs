using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that grants one-time credit rewards when the player reaches
    /// configured level milestones.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. OnEnable  : subscribes <see cref="HandleLevelUp"/> to <c>_onLevelUp</c>.
    ///   2. HandleLevelUp : reads <see cref="PlayerProgressionSO.CurrentLevel"/>,
    ///      looks up matching <see cref="LevelRewardEntry"/> items from the catalog,
    ///      calls <see cref="PlayerWallet.AddFunds"/> for each, persists via the
    ///      load → mutate → save pattern, raises <c>_onRewardGranted</c>, and
    ///      optionally enqueues toast notifications.
    ///   3. OnDisable : unsubscribes to prevent stale callbacks.
    ///
    /// ── Re-entrancy / double-claim safety ─────────────────────────────────────
    ///   <see cref="PlayerProgressionSO.AddXP"/> fires <c>_onLevelUp</c> exactly once
    ///   per level gained during an XP award call.  Because levels are monotonically
    ///   increasing and persist across sessions (GameBootstrapper restores the level
    ///   without re-firing events), each milestone is naturally rewarded once.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI namespace references.
    ///   - HandleLevelUp fires during <see cref="PlayerProgressionSO.AddXP"/> (which
    ///     is called from <see cref="MatchManager.EndMatch"/>).  The wallet AddFunds
    ///     call happens in-memory; the Load → mutate → Save persists the updated
    ///     balance.  MatchManager's own final Save (after AddXP returns) re-reads the
    ///     save file and overwrites only <c>walletBalance</c> — so the level reward
    ///     balance is correctly reflected in the match record as well.
    ///   - All optional fields are null-safe; leave unassigned to skip that behaviour.
    ///   - No Update / FixedUpdate — purely event-driven (cold path only).
    ///   - The <see cref="System.Action"/> delegate is cached in Awake — zero alloc
    ///     in the event callback.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to the Bootstrap or Arena scene.
    ///   2. Assign <c>_rewardConfig</c> (LevelRewardConfigSO) and
    ///      <c>_playerProgression</c> (PlayerProgressionSO).
    ///   3. Assign <c>_onLevelUp</c> → the same VoidGameEvent as
    ///      <see cref="PlayerProgressionSO._onLevelUp"/>.
    ///   4. Assign <c>_playerWallet</c> and optionally <c>_notificationQueue</c>.
    ///   5. Optionally assign <c>_onRewardGranted</c> → a VoidGameEvent for audio/UI.
    /// </summary>
    public sealed class LevelRewardManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Reward Catalog")]
        [Tooltip("SO defining which levels grant credit rewards and how much.")]
        [SerializeField] private LevelRewardConfigSO _rewardConfig;

        [Header("Progression")]
        [Tooltip("Read after _onLevelUp fires to determine which level was just reached.")]
        [SerializeField] private PlayerProgressionSO _playerProgression;

        [Header("Economy")]
        [Tooltip("Wallet credited when a milestone reward is applied.")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Notifications (optional)")]
        [Tooltip("When assigned, a toast is enqueued for each reward entry that has " +
                 "a non-empty DisplayName. Leave null to skip toast notifications.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by PlayerProgressionSO when the player levels up. " +
                 "Wire to the same SO as PlayerProgressionSO._onLevelUp.")]
        [SerializeField] private VoidGameEvent _onLevelUp;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after all rewards for the current level-up have been applied. " +
                 "Payload: none. Wire to audio, analytics, or UI systems. Leave null to skip.")]
        [SerializeField] private VoidGameEvent _onRewardGranted;

        // ── Cached delegate ───────────────────────────────────────────────────

        private System.Action _handleLevelUpDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleLevelUpDelegate = HandleLevelUp;
        }

        private void OnEnable()
        {
            _onLevelUp?.RegisterCallback(_handleLevelUpDelegate);
        }

        private void OnDisable()
        {
            _onLevelUp?.UnregisterCallback(_handleLevelUpDelegate);
        }

        // ── Internal handler ──────────────────────────────────────────────────

        private void HandleLevelUp()
        {
            if (_rewardConfig == null || _playerProgression == null) return;

            int level = _playerProgression.CurrentLevel;
            IReadOnlyList<LevelRewardEntry> rewards = _rewardConfig.GetRewardsForLevel(level);

            if (rewards.Count == 0) return;

            int totalGranted = 0;

            for (int i = 0; i < rewards.Count; i++)
            {
                LevelRewardEntry entry = rewards[i];

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
            // Uses the load → mutate → save pattern so no other SaveData field is disturbed.
            if (totalGranted > 0)
                PersistWallet();

            _onRewardGranted?.Raise();

            Debug.Log($"[LevelRewardManager] Level {level} reached — " +
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
            if (_rewardConfig == null)
                Debug.LogWarning("[LevelRewardManager] _rewardConfig not assigned — " +
                                 "no level rewards will be granted.", this);
            if (_playerProgression == null)
                Debug.LogWarning("[LevelRewardManager] _playerProgression not assigned — " +
                                 "current level cannot be read on level-up.", this);
            if (_onLevelUp == null)
                Debug.LogWarning("[LevelRewardManager] _onLevelUp not assigned — " +
                                 "HandleLevelUp will never fire.", this);
        }
#endif
    }
}

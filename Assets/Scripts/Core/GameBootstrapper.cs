using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Single entry point executed before any other script (Script Execution Order: -100).
    /// Loads save data, rehydrates the PlayerWallet, and fires the startup event channel.
    /// Place one instance in the Bootstrap scene.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Economy")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Tooltip("Inventory SO rehydrated from SaveData.unlockedPartIds on startup.")]
        [SerializeField] private PlayerInventory _playerInventory;

        [Tooltip("Parts given to a brand-new player (empty inventory after save load). " +
                 "Leave null to skip starter distribution.")]
        [SerializeField] private StarterInventoryConfig _starterConfig;

        [Tooltip("Persisted equipped-part configuration. LoadSnapshot called on startup. " +
                 "Leave null to skip loadout rehydration.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Tracks per-part upgrade tiers. LoadSnapshot called on startup. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private PlayerPartUpgrades _playerPartUpgrades;

        [Tooltip("Tracks the player's consecutive win streak. LoadSnapshot called on startup. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private WinStreakSO _winStreak;

        [Header("Progression")]
        [Tooltip("Tracks the player's accumulated XP and level. LoadSnapshot called on startup. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private PlayerProgressionSO _playerProgression;

        [Header("Achievements (optional)")]
        [Tooltip("Runtime SO for achievement unlock state and match counters. " +
                 "LoadSnapshot called on startup. Leave null to skip (backwards-compatible).")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Header("Career Statistics (optional)")]
        [Tooltip("Accumulates career-wide damage, currency, and playtime totals. " +
                 "LoadSnapshot called on startup. Leave null to skip (backwards-compatible).")]
        [SerializeField] private PlayerCareerStatsSO _careerStats;

        [Header("Daily Challenge (optional)")]
        [Tooltip("Runtime SO storing the current daily challenge and completion state. " +
                 "LoadSnapshot called on startup so DailyChallengeManager.Awake() can " +
                 "restore the correct challenge via RefreshIfNeeded(). " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private DailyChallengeSO _dailyChallenge;

        [Header("Settings")]
        [Tooltip("Audio/gameplay settings SO. Loaded from disk on startup. " +
                 "Leave null to skip (settings will use inspector defaults).")]
        [SerializeField] private GameSettingsSO _gameSettings;

        [Header("Events")]
        [SerializeField] private VoidGameEvent _onGameBootstrapped;

        private void Awake()
        {
            // Ensure only one bootstrapper exists across scene loads.
            DontDestroyOnLoad(gameObject);

            LoadAndApplySaveData();

            _onGameBootstrapped?.Raise();
        }

        private void LoadAndApplySaveData()
        {
            SaveData save = SaveSystem.Load();

            if (_playerWallet != null)
            {
                // Distinguish a true first launch (all-default SaveData) from a returning
                // player who legitimately spent all their money (balance == 0 but has history).
                // Reset() applies the inspector _startingBalance; LoadSnapshot restores a
                // persisted value including a valid 0.
                bool isFirstLaunch = save.matchHistory.Count == 0 && save.walletBalance == 0
                                  && save.unlockedPartIds.Count == 0;
                if (isFirstLaunch)
                    _playerWallet.Reset();
                else
                    _playerWallet.LoadSnapshot(save.walletBalance);
            }

            // Rehydrate owned-part list from persisted snapshot.
            // Safe when save.unlockedPartIds is null (old saves) or empty (new game).
            _playerInventory?.LoadSnapshot(save.unlockedPartIds);

            // Restore audio volume preferences.
            // LoadSnapshot is a no-op when settingsSnapshot is null (old saves get defaults).
            _gameSettings?.LoadSnapshot(save.settingsSnapshot);

            // Restore the player's saved part loadout.
            // LoadSnapshot is a no-op on null (old saves get an empty loadout).
            _playerLoadout?.LoadSnapshot(save.loadoutPartIds);

            // Restore per-part upgrade tiers.
            // LoadSnapshot is safe with empty/null lists (old saves get no upgrades).
            _playerPartUpgrades?.LoadSnapshot(save.upgradePartIds, save.upgradePartTierValues);

            // Restore win-streak counters.
            // LoadSnapshot is bootstrapper-safe (no event fire); old saves default to 0.
            _winStreak?.LoadSnapshot(save.currentWinStreak, save.bestWinStreak);

            // Restore XP and level.
            // LoadSnapshot clamps level 0 (old-save default) to level 1 automatically.
            _playerProgression?.LoadSnapshot(save.playerTotalXP, save.playerLevel);

            // Restore achievement unlock state and match-play counters.
            // LoadSnapshot is bootstrapper-safe (no events); old saves default to 0/empty.
            _playerAchievements?.LoadSnapshot(
                save.totalMatchesPlayed, save.totalMatchesWon, save.unlockedAchievementIds);

            // Restore career-wide stat totals.
            // LoadSnapshot is bootstrapper-safe (no events); old saves default to 0.
            _careerStats?.LoadSnapshot(
                save.careerDamageDealt, save.careerDamageTaken,
                save.careerCurrencyEarned, save.careerPlaytimeSeconds);

            // Restore daily challenge date, pool index, and completion flag.
            // LoadSnapshot is bootstrapper-safe (no events); old saves default to
            // empty date / -1 index / false — DailyChallengeManager.Awake then calls
            // RefreshIfNeeded() which treats the empty date as a new day.
            _dailyChallenge?.LoadSnapshot(
                save.dailyChallengeDate, save.dailyChallengeIndex, save.dailyChallengeCompleted);

            // On a brand-new save (inventory empty after load), unlock configured starter parts
            // and immediately persist them so they survive the next session.
            if (_playerInventory != null
                && _starterConfig  != null
                && _playerInventory.UnlockedPartIds.Count == 0
                && _starterConfig.StarterPartIds.Count    >  0)
            {
                ApplyStarterInventory(save);
            }
        }

        /// <summary>
        /// Unlocks all starter parts in <c>_starterConfig</c> into <c>_playerInventory</c>
        /// and persists the result.  Called only when the inventory is empty (new game).
        /// </summary>
        private void ApplyStarterInventory(SaveData save)
        {
            IReadOnlyList<string> starters = _starterConfig.StarterPartIds;
            for (int i = 0; i < starters.Count; i++)
                _playerInventory.UnlockPart(starters[i]);

            // Persist starters so they survive the next session.
            save.unlockedPartIds.Clear();
            IReadOnlyList<string> owned = _playerInventory.UnlockedPartIds;
            for (int i = 0; i < owned.Count; i++)
                save.unlockedPartIds.Add(owned[i]);
            SaveSystem.Save(save);
        }

        /// <summary>
        /// Call at the end of a match to persist the match record and current wallet.
        /// </summary>
        public void RecordMatchAndSave(MatchRecord record)
        {
            if (record == null) return;

            SaveData save = SaveSystem.Load();
            save.walletBalance = _playerWallet != null ? _playerWallet.Balance : 0;
            record.walletSnapshot = save.walletBalance;
            save.matchHistory.Add(record);
            SaveSystem.Save(save);
        }
    }
}

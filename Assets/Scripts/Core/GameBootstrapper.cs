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
                _playerWallet.LoadSnapshot(save.walletBalance > 0
                    ? save.walletBalance
                    : _playerWallet.Balance); // keeps starting balance on first launch

            // Rehydrate owned-part list from persisted snapshot.
            // Safe when save.unlockedPartIds is null (old saves) or empty (new game).
            _playerInventory?.LoadSnapshot(save.unlockedPartIds);

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

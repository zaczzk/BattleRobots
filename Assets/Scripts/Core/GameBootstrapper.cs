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

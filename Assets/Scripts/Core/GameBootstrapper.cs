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

        [Header("Settings")]
        [Tooltip("SettingsSO — loaded from save file on startup, applied immediately.")]
        [SerializeField] private SettingsSO _settings;

        [Header("Robot Loadout")]
        [Tooltip("RobotLoadoutSO — restored from save file on startup; snapshotted on match save.")]
        [SerializeField] private RobotLoadoutSO _robotLoadout;

        [Header("Player Profile")]
        [Tooltip("PlayerProfileSO — restored from save file on startup; career stats updated on match save.")]
        [SerializeField] private PlayerProfileSO _playerProfile;

        [Header("Social")]
        [Tooltip("FriendListSO — restored from save file on startup; mutations auto-persist.")]
        [SerializeField] private FriendListSO _friendList;

        [Header("Achievements")]
        [Tooltip("AchievementProgressSO — loaded from save file on startup; checked after each match.")]
        [SerializeField] private AchievementProgressSO _achievementProgress;

        [Header("Daily Challenge")]
        [Tooltip("DailyChallengeProgressSO — refreshed on startup to select today's challenge; " +
                 "updated after each match via RecordMatch.")]
        [SerializeField] private DailyChallengeProgressSO _dailyChallenge;

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

            if (_settings != null)
            {
                _settings.LoadFromData(save.settings);
                _settings.LoadKeyBindings(save.keyBindings);
            }

            if (_robotLoadout != null)
                _robotLoadout.LoadFromData(save.robotLoadout);

            if (_playerProfile != null)
                _playerProfile.LoadFromData(save.playerProfile);

            if (_friendList != null)
                _friendList.LoadFromData(save.friendList);

            if (_achievementProgress != null)
                _achievementProgress.LoadFromData(save.achievements);

            if (_dailyChallenge != null)
                _dailyChallenge.RefreshForToday(save.dailyChallenge);
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

            if (_robotLoadout != null)
                save.robotLoadout = _robotLoadout.BuildData();

            if (_playerProfile != null)
            {
                _playerProfile.UpdateFromMatchRecord(record);
                save.playerProfile = _playerProfile.BuildData();
            }

            // Achievement check must come after profile update so career stats
            // already reflect the completed match when conditions are evaluated.
            if (_achievementProgress != null)
            {
                _achievementProgress.CheckAndUnlock(record, _playerProfile);
                save.achievements = _achievementProgress.BuildData();
            }

            // Daily challenge: record match progress and persist updated state.
            if (_dailyChallenge != null)
            {
                _dailyChallenge.RecordMatch(record);
                save.dailyChallenge = _dailyChallenge.BuildData();
            }

            SaveSystem.Save(save);
        }
    }
}

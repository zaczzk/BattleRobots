using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LevelRewardManager"/>.
    ///
    /// Uses the inactive-GameObject pattern: the GO is created inactive so fields can
    /// be injected via reflection before Awake / OnEnable run.  Activating the GO
    /// triggers Awake + OnEnable (event subscription).
    ///
    /// SaveSystem.Delete() is called in SetUp and TearDown to keep tests isolated.
    /// Wallet is initialised to 0 credits via SetField("_startingBalance", 0) + Reset().
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null fields — DoesNotThrow.
    ///   • Null _onLevelUp channel — OnEnable DoesNotThrow.
    ///   • HandleLevelUp null _rewardConfig — no wallet change.
    ///   • HandleLevelUp null _playerProgression — no wallet change.
    ///   • HandleLevelUp no matching level — no wallet change, no event.
    ///   • HandleLevelUp matching level credits wallet correctly.
    ///   • HandleLevelUp null _playerWallet — DoesNotThrow.
    ///   • HandleLevelUp fires _onRewardGranted after applying rewards.
    ///   • HandleLevelUp null _onRewardGranted — DoesNotThrow.
    ///   • HandleLevelUp null _notificationQueue — DoesNotThrow.
    ///   • HandleLevelUp enqueues toast when DisplayName is set.
    ///   • OnDisable unregisters — raising _onLevelUp after disable has no effect.
    ///   • Multiple reward entries at same level all applied.
    /// </summary>
    public class LevelRewardManagerTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject          _go;
        private LevelRewardManager  _manager;
        private LevelRewardConfigSO _config;
        private PlayerProgressionSO _progression;
        private PlayerWallet        _wallet;
        private NotificationQueueSO _notificationQueue;
        private VoidGameEvent       _onLevelUp;
        private VoidGameEvent       _onRewardGranted;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static LevelRewardEntry MakeEntry(int level, int credits, string name = "")
            => new LevelRewardEntry { level = level, rewardCredits = credits, displayName = name };

        private static void SetRewards(LevelRewardConfigSO config,
                                       List<LevelRewardEntry> rewards)
        {
            FieldInfo fi = config.GetType()
                .GetField("_rewards", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_rewards' not found on LevelRewardConfigSO.");
            fi.SetValue(config, rewards);
        }

        /// <summary>Creates a wallet with a starting balance of 0 credits.</summary>
        private static PlayerWallet MakeEmptyWallet()
        {
            var wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(wallet, "_startingBalance", 0);
            wallet.Reset();
            return wallet;
        }

        /// <summary>Wires all standard fields and activates the GO.</summary>
        private void ActivateWithDefaults()
        {
            SetField(_manager, "_rewardConfig",      _config);
            SetField(_manager, "_playerProgression", _progression);
            SetField(_manager, "_playerWallet",      _wallet);
            SetField(_manager, "_notificationQueue", _notificationQueue);
            SetField(_manager, "_onLevelUp",         _onLevelUp);
            SetField(_manager, "_onRewardGranted",   _onRewardGranted);
            _go.SetActive(true);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            _config            = ScriptableObject.CreateInstance<LevelRewardConfigSO>();
            _progression       = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            _wallet            = MakeEmptyWallet();  // balance starts at 0
            _notificationQueue = ScriptableObject.CreateInstance<NotificationQueueSO>();
            _onLevelUp         = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onRewardGranted   = ScriptableObject.CreateInstance<VoidGameEvent>();

            _go      = new GameObject("LevelRewardManager");
            _go.SetActive(false);   // Inject fields before Awake fires.
            _manager = _go.AddComponent<LevelRewardManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_progression);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_notificationQueue);
            Object.DestroyImmediate(_onLevelUp);
            Object.DestroyImmediate(_onRewardGranted);
            SaveSystem.Delete();
        }

        // ── Null-guard: all-null fields ───────────────────────────────────────

        [Test]
        public void OnEnable_AllNullFields_DoesNotThrow()
        {
            // No fields assigned — activate triggers Awake + OnEnable.
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void OnDisable_AllNullFields_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── Null _onLevelUp channel ───────────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            SetField(_manager, "_rewardConfig",      _config);
            SetField(_manager, "_playerProgression", _progression);
            SetField(_manager, "_playerWallet",      _wallet);
            // _onLevelUp intentionally left null.
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        // ── HandleLevelUp: null _rewardConfig ────────────────────────────────

        [Test]
        public void HandleLevelUp_NullConfig_NoWalletChange()
        {
            SetField(_manager, "_rewardConfig",      null);
            SetField(_manager, "_playerProgression", _progression);
            SetField(_manager, "_playerWallet",      _wallet);
            SetField(_manager, "_onLevelUp",         _onLevelUp);
            _go.SetActive(true);

            int before = _wallet.Balance;
            _onLevelUp.Raise();

            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── HandleLevelUp: null _playerProgression ────────────────────────────

        [Test]
        public void HandleLevelUp_NullProgression_NoWalletChange()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(2, 100) });
            SetField(_manager, "_rewardConfig",      _config);
            SetField(_manager, "_playerProgression", null);
            SetField(_manager, "_playerWallet",      _wallet);
            SetField(_manager, "_onLevelUp",         _onLevelUp);
            _go.SetActive(true);

            int before = _wallet.Balance;
            _onLevelUp.Raise();

            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── HandleLevelUp: no matching level ─────────────────────────────────

        [Test]
        public void HandleLevelUp_NoMatchingLevel_NoWalletChange()
        {
            // Config has a reward at level 5; progression is at level 2.
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(5, 200) });
            _progression.LoadSnapshot(totalXP: 50, level: 2);
            ActivateWithDefaults();

            int before = _wallet.Balance;
            _onLevelUp.Raise();

            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── HandleLevelUp: matching level credits wallet ──────────────────────

        [Test]
        public void HandleLevelUp_MatchingLevel_CreditsWalletCorrectly()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(3, 150) });
            _progression.LoadSnapshot(totalXP: 300, level: 3);
            ActivateWithDefaults();

            _onLevelUp.Raise();

            Assert.AreEqual(150, _wallet.Balance);
        }

        // ── HandleLevelUp: null _playerWallet ────────────────────────────────

        [Test]
        public void HandleLevelUp_NullWallet_DoesNotThrow()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(2, 100) });
            _progression.LoadSnapshot(totalXP: 100, level: 2);
            SetField(_manager, "_rewardConfig",      _config);
            SetField(_manager, "_playerProgression", _progression);
            SetField(_manager, "_playerWallet",      null);
            SetField(_manager, "_onLevelUp",         _onLevelUp);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onLevelUp.Raise());
        }

        // ── HandleLevelUp: fires _onRewardGranted ────────────────────────────

        [Test]
        public void HandleLevelUp_MatchingLevel_FiresOnRewardGranted()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(2, 50) });
            _progression.LoadSnapshot(totalXP: 100, level: 2);
            ActivateWithDefaults();

            bool fired = false;
            _onRewardGranted.RegisterCallback(() => fired = true);
            _onLevelUp.Raise();

            Assert.IsTrue(fired, "_onRewardGranted should have been raised.");
        }

        // ── HandleLevelUp: null _onRewardGranted ─────────────────────────────

        [Test]
        public void HandleLevelUp_NullOnRewardGranted_DoesNotThrow()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(2, 50) });
            _progression.LoadSnapshot(totalXP: 100, level: 2);
            SetField(_manager, "_rewardConfig",      _config);
            SetField(_manager, "_playerProgression", _progression);
            SetField(_manager, "_playerWallet",      _wallet);
            SetField(_manager, "_onLevelUp",         _onLevelUp);
            SetField(_manager, "_onRewardGranted",   null);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onLevelUp.Raise());
        }

        // ── HandleLevelUp: null _notificationQueue ────────────────────────────

        [Test]
        public void HandleLevelUp_NullNotificationQueue_DoesNotThrow()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(2, 50, "Level 2!")
            });
            _progression.LoadSnapshot(totalXP: 100, level: 2);
            SetField(_manager, "_rewardConfig",      _config);
            SetField(_manager, "_playerProgression", _progression);
            SetField(_manager, "_playerWallet",      _wallet);
            SetField(_manager, "_notificationQueue", null);
            SetField(_manager, "_onLevelUp",         _onLevelUp);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onLevelUp.Raise());
        }

        // ── HandleLevelUp: enqueues toast for named entries ───────────────────

        [Test]
        public void HandleLevelUp_NamedEntry_EnqueuesToast()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(2, 50, "Level 2 Bonus!")
            });
            _progression.LoadSnapshot(totalXP: 100, level: 2);
            ActivateWithDefaults();

            _onLevelUp.Raise();

            Assert.Greater(_notificationQueue.Count, 0,
                "A toast should be enqueued for an entry with a non-empty DisplayName.");
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_Unregisters_RaisingEventHasNoEffect()
        {
            SetRewards(_config, new List<LevelRewardEntry> { MakeEntry(2, 100) });
            _progression.LoadSnapshot(totalXP: 100, level: 2);
            ActivateWithDefaults();

            // Disable removes the subscription.
            _go.SetActive(false);

            int before = _wallet.Balance;
            _onLevelUp.Raise();

            // Wallet must not change because the handler was unregistered.
            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── Multiple entries at same level all applied ────────────────────────

        [Test]
        public void HandleLevelUp_MultipleEntriesSameLevel_AllApplied()
        {
            SetRewards(_config, new List<LevelRewardEntry>
            {
                MakeEntry(5, 100),
                MakeEntry(5, 250),
            });
            _progression.LoadSnapshot(totalXP: 1000, level: 5);
            ActivateWithDefaults();

            _onLevelUp.Raise();

            // 100 + 250 = 350 total
            Assert.AreEqual(350, _wallet.Balance);
        }
    }
}

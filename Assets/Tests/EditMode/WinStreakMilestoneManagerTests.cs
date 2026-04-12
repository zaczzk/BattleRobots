using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WinStreakMilestoneManager"/>.
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
    ///   • OnEnable with null _onStreakChanged channel — DoesNotThrow.
    ///   • HandleStreakChanged null _milestoneConfig — no wallet change.
    ///   • HandleStreakChanged null _winStreak — no wallet change.
    ///   • HandleStreakChanged streak = 0 (loss) — no wallet change.
    ///   • HandleStreakChanged no matching streak — no wallet change.
    ///   • HandleStreakChanged matching streak — credits wallet correctly.
    ///   • HandleStreakChanged null _playerWallet — DoesNotThrow.
    ///   • HandleStreakChanged fires _onMilestoneReached after rewards applied.
    ///   • HandleStreakChanged null _onMilestoneReached — DoesNotThrow.
    ///   • HandleStreakChanged null _notificationQueue — DoesNotThrow.
    ///   • HandleStreakChanged named entry — enqueues toast.
    ///   • OnDisable unregisters — raising _onStreakChanged after disable has no effect.
    ///   • Multiple entries at same streakTarget — all applied.
    /// </summary>
    public class WinStreakMilestoneManagerTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject               _go;
        private WinStreakMilestoneManager _manager;
        private WinStreakMilestoneSO      _config;
        private WinStreakSO              _winStreak;
        private PlayerWallet             _wallet;
        private NotificationQueueSO      _notificationQueue;
        private VoidGameEvent            _onStreakChanged;
        private VoidGameEvent            _onMilestoneReached;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static WinStreakMilestoneEntry MakeEntry(int target, int credits,
                                                          string name = "")
            => new WinStreakMilestoneEntry
            {
                streakTarget  = target,
                rewardCredits = credits,
                displayName   = name,
            };

        private static void SetMilestones(WinStreakMilestoneSO config,
                                           List<WinStreakMilestoneEntry> milestones)
        {
            FieldInfo fi = config.GetType()
                .GetField("_milestones", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_milestones' not found on WinStreakMilestoneSO.");
            fi.SetValue(config, milestones);
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
            SetField(_manager, "_milestoneConfig",    _config);
            SetField(_manager, "_winStreak",           _winStreak);
            SetField(_manager, "_playerWallet",        _wallet);
            SetField(_manager, "_notificationQueue",   _notificationQueue);
            SetField(_manager, "_onStreakChanged",     _onStreakChanged);
            SetField(_manager, "_onMilestoneReached",  _onMilestoneReached);
            _go.SetActive(true);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            _config             = ScriptableObject.CreateInstance<WinStreakMilestoneSO>();
            _winStreak          = ScriptableObject.CreateInstance<WinStreakSO>();
            _wallet             = MakeEmptyWallet();
            _notificationQueue  = ScriptableObject.CreateInstance<NotificationQueueSO>();
            _onStreakChanged    = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onMilestoneReached = ScriptableObject.CreateInstance<VoidGameEvent>();

            _go      = new GameObject("WinStreakMilestoneManager");
            _go.SetActive(false);   // Inject fields before Awake fires.
            _manager = _go.AddComponent<WinStreakMilestoneManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_winStreak);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_notificationQueue);
            Object.DestroyImmediate(_onStreakChanged);
            Object.DestroyImmediate(_onMilestoneReached);
            SaveSystem.Delete();
        }

        // ── Null-guard: all-null fields ───────────────────────────────────────

        [Test]
        public void OnEnable_AllNullFields_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void OnDisable_AllNullFields_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── Null _onStreakChanged channel ─────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            SetField(_manager, "_milestoneConfig", _config);
            SetField(_manager, "_winStreak",       _winStreak);
            SetField(_manager, "_playerWallet",    _wallet);
            // _onStreakChanged intentionally left null.
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        // ── HandleStreakChanged: null _milestoneConfig ────────────────────────

        [Test]
        public void HandleStreakChanged_NullConfig_NoWalletChange()
        {
            _winStreak.RecordWin(); // streak = 1
            SetField(_manager, "_milestoneConfig", null);
            SetField(_manager, "_winStreak",       _winStreak);
            SetField(_manager, "_playerWallet",    _wallet);
            SetField(_manager, "_onStreakChanged", _onStreakChanged);
            _go.SetActive(true);

            int before = _wallet.Balance;
            _onStreakChanged.Raise();

            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── HandleStreakChanged: null _winStreak ──────────────────────────────

        [Test]
        public void HandleStreakChanged_NullWinStreak_NoWalletChange()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(1, 100) });
            SetField(_manager, "_milestoneConfig", _config);
            SetField(_manager, "_winStreak",       null);
            SetField(_manager, "_playerWallet",    _wallet);
            SetField(_manager, "_onStreakChanged", _onStreakChanged);
            _go.SetActive(true);

            int before = _wallet.Balance;
            _onStreakChanged.Raise();

            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── HandleStreakChanged: streak = 0 (after a loss) ────────────────────

        [Test]
        public void HandleStreakChanged_StreakIsZero_NoWalletChange()
        {
            // Configure a milestone at streak 0 (pathological case) — should not fire.
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(0, 100) });
            // _winStreak.CurrentStreak defaults to 0 (fresh instance).
            ActivateWithDefaults();

            int before = _wallet.Balance;
            _onStreakChanged.Raise();

            // streak <= 0 guard must suppress the reward.
            Assert.AreEqual(before, _wallet.Balance,
                "No reward should be granted when CurrentStreak is 0 (fired on loss).");
        }

        // ── HandleStreakChanged: no matching milestone ─────────────────────────

        [Test]
        public void HandleStreakChanged_NoMatchingStreak_NoWalletChange()
        {
            // Config has milestone at streak 5; winStreak is at streak 2.
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(5, 200) });
            _winStreak.RecordWin(); // streak = 1
            _winStreak.RecordWin(); // streak = 2
            ActivateWithDefaults();

            int before = _wallet.Balance;
            _onStreakChanged.Raise();

            Assert.AreEqual(before, _wallet.Balance);
        }

        // ── HandleStreakChanged: matching milestone credits wallet ─────────────

        [Test]
        public void HandleStreakChanged_MatchingStreak_CreditsWalletCorrectly()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(3, 150) });
            _winStreak.RecordWin(); // 1
            _winStreak.RecordWin(); // 2
            _winStreak.RecordWin(); // 3
            ActivateWithDefaults();

            _onStreakChanged.Raise();

            Assert.AreEqual(150, _wallet.Balance);
        }

        // ── HandleStreakChanged: null _playerWallet ───────────────────────────

        [Test]
        public void HandleStreakChanged_NullWallet_DoesNotThrow()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(1, 100) });
            _winStreak.RecordWin(); // streak = 1
            SetField(_manager, "_milestoneConfig", _config);
            SetField(_manager, "_winStreak",       _winStreak);
            SetField(_manager, "_playerWallet",    null);
            SetField(_manager, "_onStreakChanged", _onStreakChanged);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onStreakChanged.Raise());
        }

        // ── HandleStreakChanged: fires _onMilestoneReached ─────────────────────

        [Test]
        public void HandleStreakChanged_MatchingStreak_FiresOnMilestoneReached()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(2, 50) });
            _winStreak.RecordWin(); // 1
            _winStreak.RecordWin(); // 2
            ActivateWithDefaults();

            bool fired = false;
            _onMilestoneReached.RegisterCallback(() => fired = true);
            _onStreakChanged.Raise();

            Assert.IsTrue(fired, "_onMilestoneReached should have been raised.");
        }

        // ── HandleStreakChanged: null _onMilestoneReached ─────────────────────

        [Test]
        public void HandleStreakChanged_NullOnMilestoneReached_DoesNotThrow()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(1, 50) });
            _winStreak.RecordWin(); // streak = 1
            SetField(_manager, "_milestoneConfig",   _config);
            SetField(_manager, "_winStreak",          _winStreak);
            SetField(_manager, "_playerWallet",       _wallet);
            SetField(_manager, "_onStreakChanged",    _onStreakChanged);
            SetField(_manager, "_onMilestoneReached", null);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onStreakChanged.Raise());
        }

        // ── HandleStreakChanged: null _notificationQueue ──────────────────────

        [Test]
        public void HandleStreakChanged_NullNotificationQueue_DoesNotThrow()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(1, 50, "1-Win Streak!")
            });
            _winStreak.RecordWin(); // streak = 1
            SetField(_manager, "_milestoneConfig",  _config);
            SetField(_manager, "_winStreak",        _winStreak);
            SetField(_manager, "_playerWallet",     _wallet);
            SetField(_manager, "_notificationQueue", null);
            SetField(_manager, "_onStreakChanged",  _onStreakChanged);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onStreakChanged.Raise());
        }

        // ── HandleStreakChanged: named entry enqueues toast ───────────────────

        [Test]
        public void HandleStreakChanged_NamedEntry_EnqueuesToast()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(3, 75, "Hat Trick!")
            });
            _winStreak.RecordWin(); // 1
            _winStreak.RecordWin(); // 2
            _winStreak.RecordWin(); // 3
            ActivateWithDefaults();

            _onStreakChanged.Raise();

            Assert.Greater(_notificationQueue.Count, 0,
                "A toast should be enqueued for an entry with a non-empty DisplayName.");
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_Unregisters_RaisingEventHasNoEffect()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry> { MakeEntry(1, 100) });
            _winStreak.RecordWin(); // streak = 1
            ActivateWithDefaults();

            _go.SetActive(false);   // triggers OnDisable → unregisters

            int before = _wallet.Balance;
            _onStreakChanged.Raise();

            Assert.AreEqual(before, _wallet.Balance,
                "Wallet must not change after OnDisable unregistered the handler.");
        }

        // ── Multiple entries at same streakTarget all applied ─────────────────

        [Test]
        public void HandleStreakChanged_MultipleEntriesSameTarget_AllApplied()
        {
            SetMilestones(_config, new List<WinStreakMilestoneEntry>
            {
                MakeEntry(5, 100),
                MakeEntry(5, 250),
            });
            for (int i = 0; i < 5; i++) _winStreak.RecordWin(); // streak = 5
            ActivateWithDefaults();

            _onStreakChanged.Raise();

            // 100 + 250 = 350 total
            Assert.AreEqual(350, _wallet.Balance,
                "Both entries at streak 5 should be applied (total 350 credits).");
        }
    }
}

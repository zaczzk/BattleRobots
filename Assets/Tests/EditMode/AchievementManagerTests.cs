using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AchievementManager"/>.
    ///
    /// Covers:
    ///   • Null safety: null catalog / null achievements → EvaluateAll() no-throw.
    ///   • Trigger evaluation — each <see cref="AchievementTrigger"/> type:
    ///       MatchWon, WinStreak, ReachLevel, TotalMatches, PartUpgraded.
    ///   • Idempotency: already-unlocked achievements are not unlocked again.
    ///   • Credit reward: wallet AddFunds called on unlock when RewardCredits > 0;
    ///     null wallet is safe.
    ///   • HandleMatchEnded (private, invoked via event channel):
    ///       records win/loss in PlayerAchievementsSO; evaluates achievements;
    ///       persists to disk (SaveData fields updated).
    ///   • OnEnable / OnDisable registration: unregistered AchievementManager
    ///     does NOT react to MatchEnded after disable.
    ///   • PersistAchievements: writes correct counters + IDs to SaveData.
    ///
    /// HandleMatchEnded is private; it is invoked by raising the _onMatchEnded
    /// VoidGameEvent after wiring it to the AchievementManager via reflection
    /// (inactive-GO pattern: fields injected while disabled, then activated so
    /// OnEnable subscribes).
    ///
    /// SaveSystem.Delete() is called in SetUp and TearDown to prevent cross-test
    /// save-file pollution.
    /// </summary>
    public class AchievementManagerTests
    {
        // ── Scene state ───────────────────────────────────────────────────────

        private GameObject          _go;
        private AchievementManager  _manager;

        private readonly List<GameObject>       _extraGOs = new List<GameObject>();
        private readonly List<ScriptableObject> _extraSOs = new List<ScriptableObject>();

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            // Create GO inactive so we can wire fields before OnEnable runs.
            _go      = new GameObject("TestAchievementManager");
            _go.SetActive(false);
            _manager = _go.AddComponent<AchievementManager>();

            _extraGOs.Clear();
            _extraSOs.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();

            Object.DestroyImmediate(_go);
            foreach (var g in _extraGOs) if (g) Object.DestroyImmediate(g);
            foreach (var s in _extraSOs) if (s) Object.DestroyImmediate(s);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private AchievementDefinitionSO MakeDef(
            string id,
            AchievementTrigger trigger,
            int targetCount = 1,
            int reward      = 0)
        {
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            _extraSOs.Add(def);
            SetField(def, "_id",           id);
            SetField(def, "_displayName",  id);
            SetField(def, "_triggerType",  trigger);
            SetField(def, "_targetCount",  targetCount);
            SetField(def, "_rewardCredits", reward);
            return def;
        }

        private AchievementCatalogSO MakeCatalog(params AchievementDefinitionSO[] defs)
        {
            var cat = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            _extraSOs.Add(cat);
            var list = new List<AchievementDefinitionSO>(defs);
            SetField(cat, "_achievements", list);
            return cat;
        }

        private PlayerAchievementsSO MakeAchievements()
        {
            var s = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            _extraSOs.Add(s);
            return s;
        }

        private PlayerWallet MakeWallet(int balance)
        {
            var w = ScriptableObject.CreateInstance<PlayerWallet>();
            _extraSOs.Add(w);
            SetField(w, "_startingBalance", balance);
            w.Reset();
            return w;
        }

        private WinStreakSO MakeStreak(int current, int best)
        {
            var s = ScriptableObject.CreateInstance<WinStreakSO>();
            _extraSOs.Add(s);
            s.LoadSnapshot(current, best);
            return s;
        }

        private PlayerProgressionSO MakeProgression(int level)
        {
            var p = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            _extraSOs.Add(p);
            p.LoadSnapshot(0, level);
            return p;
        }

        private PlayerPartUpgrades MakeUpgrades(Dictionary<string, int> tiers)
        {
            var u = ScriptableObject.CreateInstance<PlayerPartUpgrades>();
            _extraSOs.Add(u);
            var keys   = new List<string>(tiers.Keys);
            var values = new List<int>(tiers.Values);
            u.LoadSnapshot(keys, values);
            return u;
        }

        private MatchResultSO MakeMatchResult(bool playerWon)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            _extraSOs.Add(r);
            r.Write(playerWon, 60f, 100, 500);
            return r;
        }

        /// <summary>
        /// Wires the manager's fields, activates the GO (triggering OnEnable), and
        /// returns the manager ready for use.
        /// </summary>
        private void Activate(
            AchievementCatalogSO    catalog       = null,
            PlayerAchievementsSO    achievements  = null,
            PlayerWallet            wallet        = null,
            WinStreakSO             winStreak     = null,
            PlayerProgressionSO     progression   = null,
            PlayerPartUpgrades      upgrades      = null,
            MatchResultSO           matchResult   = null,
            VoidGameEvent           onMatchEnded  = null,
            VoidGameEvent           onLevelUp     = null,
            VoidGameEvent           onUpgrades    = null,
            VoidGameEvent           onStreak      = null)
        {
            SetField(_manager, "_catalog",           catalog);
            SetField(_manager, "_playerAchievements", achievements);
            SetField(_manager, "_playerWallet",       wallet);
            SetField(_manager, "_winStreak",          winStreak);
            SetField(_manager, "_playerProgression",  progression);
            SetField(_manager, "_playerPartUpgrades", upgrades);
            SetField(_manager, "_matchResult",        matchResult);
            SetField(_manager, "_onMatchEnded",       onMatchEnded);
            SetField(_manager, "_onLevelUp",          onLevelUp);
            SetField(_manager, "_onUpgradesChanged",  onUpgrades);
            SetField(_manager, "_onStreakChanged",     onStreak);

            _go.SetActive(true); // triggers Awake + OnEnable
        }

        // ── Null safety ───────────────────────────────────────────────────────

        [Test]
        public void EvaluateAll_NullCatalog_DoesNotThrow()
        {
            Activate(achievements: MakeAchievements());
            Assert.DoesNotThrow(() => _manager.EvaluateAll());
        }

        [Test]
        public void EvaluateAll_NullAchievements_DoesNotThrow()
        {
            Activate(catalog: MakeCatalog());
            Assert.DoesNotThrow(() => _manager.EvaluateAll());
        }

        [Test]
        public void EvaluateAll_NullEntryInCatalog_DoesNotThrow()
        {
            var cat = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            _extraSOs.Add(cat);
            // Leave the list with a null entry by reflection.
            SetField(cat, "_achievements",
                     new List<AchievementDefinitionSO> { null });

            var ach = MakeAchievements();
            Activate(catalog: cat, achievements: ach);
            Assert.DoesNotThrow(() => _manager.EvaluateAll());
        }

        // ── MatchWon trigger ──────────────────────────────────────────────────

        [Test]
        public void MatchWon_TriggerUnlocks_WhenCountMet()
        {
            var def = MakeDef("win_1", AchievementTrigger.MatchWon, targetCount: 1);
            var ach = MakeAchievements();
            ach.RecordMatchResult(playerWon: true);  // TotalMatchesWon == 1

            Activate(catalog: MakeCatalog(def), achievements: ach);
            _manager.EvaluateAll();

            Assert.IsTrue(ach.HasUnlocked("win_1"));
        }

        [Test]
        public void MatchWon_TriggerDoesNotUnlock_WhenCountNotMet()
        {
            var def = MakeDef("win_5", AchievementTrigger.MatchWon, targetCount: 5);
            var ach = MakeAchievements();
            ach.RecordMatchResult(playerWon: true);  // only 1 win

            Activate(catalog: MakeCatalog(def), achievements: ach);
            _manager.EvaluateAll();

            Assert.IsFalse(ach.HasUnlocked("win_5"));
        }

        // ── WinStreak trigger ─────────────────────────────────────────────────

        [Test]
        public void WinStreak_TriggerUnlocks_WhenBestStreakMet()
        {
            var def    = MakeDef("streak_3", AchievementTrigger.WinStreak, targetCount: 3);
            var ach    = MakeAchievements();
            var streak = MakeStreak(current: 3, best: 3);

            Activate(catalog: MakeCatalog(def), achievements: ach, winStreak: streak);
            _manager.EvaluateAll();

            Assert.IsTrue(ach.HasUnlocked("streak_3"));
        }

        [Test]
        public void WinStreak_NullWinStreakSO_DoesNotUnlock()
        {
            var def = MakeDef("streak_1", AchievementTrigger.WinStreak, targetCount: 1);
            var ach = MakeAchievements();

            Activate(catalog: MakeCatalog(def), achievements: ach);  // no winStreak
            _manager.EvaluateAll();

            Assert.IsFalse(ach.HasUnlocked("streak_1"));
        }

        // ── ReachLevel trigger ────────────────────────────────────────────────

        [Test]
        public void ReachLevel_TriggerUnlocks_WhenLevelMet()
        {
            var def  = MakeDef("level_5", AchievementTrigger.ReachLevel, targetCount: 5);
            var ach  = MakeAchievements();
            var prog = MakeProgression(level: 5);

            Activate(catalog: MakeCatalog(def), achievements: ach, progression: prog);
            _manager.EvaluateAll();

            Assert.IsTrue(ach.HasUnlocked("level_5"));
        }

        [Test]
        public void ReachLevel_NullProgressionSO_DoesNotUnlock()
        {
            var def = MakeDef("level_2", AchievementTrigger.ReachLevel, targetCount: 2);
            var ach = MakeAchievements();

            Activate(catalog: MakeCatalog(def), achievements: ach);  // no progression
            _manager.EvaluateAll();

            Assert.IsFalse(ach.HasUnlocked("level_2"));
        }

        // ── TotalMatches trigger ──────────────────────────────────────────────

        [Test]
        public void TotalMatches_TriggerUnlocks_WhenCountMet()
        {
            var def = MakeDef("played_3", AchievementTrigger.TotalMatches, targetCount: 3);
            var ach = MakeAchievements();
            ach.RecordMatchResult(false);
            ach.RecordMatchResult(false);
            ach.RecordMatchResult(false);  // 3 matches played

            Activate(catalog: MakeCatalog(def), achievements: ach);
            _manager.EvaluateAll();

            Assert.IsTrue(ach.HasUnlocked("played_3"));
        }

        // ── PartUpgraded trigger ──────────────────────────────────────────────

        [Test]
        public void PartUpgraded_TriggerUnlocks_WhenTierSumMet()
        {
            var def      = MakeDef("upgraded_3", AchievementTrigger.PartUpgraded, targetCount: 3);
            var ach      = MakeAchievements();
            var upgrades = MakeUpgrades(new Dictionary<string, int>
            {
                { "part_A", 2 },
                { "part_B", 1 },   // total tiers = 3
            });

            Activate(catalog: MakeCatalog(def), achievements: ach, upgrades: upgrades);
            _manager.EvaluateAll();

            Assert.IsTrue(ach.HasUnlocked("upgraded_3"));
        }

        [Test]
        public void PartUpgraded_NullUpgrades_DoesNotUnlock()
        {
            var def = MakeDef("upgraded_1", AchievementTrigger.PartUpgraded, targetCount: 1);
            var ach = MakeAchievements();

            Activate(catalog: MakeCatalog(def), achievements: ach);  // no upgrades
            _manager.EvaluateAll();

            Assert.IsFalse(ach.HasUnlocked("upgraded_1"));
        }

        // ── Idempotency ───────────────────────────────────────────────────────

        [Test]
        public void EvaluateAll_AlreadyUnlocked_NotReunlocked()
        {
            var def = MakeDef("win_1", AchievementTrigger.MatchWon, targetCount: 1);
            var ach = MakeAchievements();
            ach.RecordMatchResult(playerWon: true);
            ach.Unlock("win_1");  // manually unlock first

            int fireCount = 0;
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(evt);
            SetField(ach, "_onAchievementUnlocked", evt);
            evt.RegisterCallback(() => fireCount++);

            Activate(catalog: MakeCatalog(def), achievements: ach);
            _manager.EvaluateAll();

            Assert.AreEqual(0, fireCount, "Event should not re-fire for an already-unlocked achievement.");
        }

        // ── Credit reward on unlock ───────────────────────────────────────────

        [Test]
        public void Unlock_CreditReward_AddsFundsToWallet()
        {
            var def    = MakeDef("win_1", AchievementTrigger.MatchWon, targetCount: 1, reward: 250);
            var ach    = MakeAchievements();
            var wallet = MakeWallet(100);
            ach.RecordMatchResult(playerWon: true);

            Activate(catalog: MakeCatalog(def), achievements: ach, wallet: wallet);
            _manager.EvaluateAll();

            Assert.AreEqual(350, wallet.Balance, "Wallet should receive the 250-credit achievement reward.");
        }

        [Test]
        public void Unlock_NullWallet_DoesNotThrow()
        {
            var def = MakeDef("win_1", AchievementTrigger.MatchWon, targetCount: 1, reward: 100);
            var ach = MakeAchievements();
            ach.RecordMatchResult(playerWon: true);

            Activate(catalog: MakeCatalog(def), achievements: ach);  // no wallet
            Assert.DoesNotThrow(() => _manager.EvaluateAll());
        }

        // ── HandleMatchEnded via event channel ───────────────────────────────

        [Test]
        public void HandleMatchEnded_Win_RecordsMatchWon()
        {
            var onMatchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(onMatchEnded);

            var ach         = MakeAchievements();
            var matchResult = MakeMatchResult(playerWon: true);

            Activate(
                catalog:      MakeCatalog(),
                achievements: ach,
                matchResult:  matchResult,
                onMatchEnded: onMatchEnded);

            onMatchEnded.Raise();

            Assert.AreEqual(1, ach.TotalMatchesPlayed);
            Assert.AreEqual(1, ach.TotalMatchesWon);
        }

        [Test]
        public void HandleMatchEnded_Loss_DoesNotIncrementWon()
        {
            var onMatchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(onMatchEnded);

            var ach         = MakeAchievements();
            var matchResult = MakeMatchResult(playerWon: false);

            Activate(
                catalog:      MakeCatalog(),
                achievements: ach,
                matchResult:  matchResult,
                onMatchEnded: onMatchEnded);

            onMatchEnded.Raise();

            Assert.AreEqual(1, ach.TotalMatchesPlayed);
            Assert.AreEqual(0, ach.TotalMatchesWon);
        }

        [Test]
        public void HandleMatchEnded_NullMatchResult_TreatsAsLoss()
        {
            var onMatchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(onMatchEnded);
            var ach = MakeAchievements();

            Activate(
                catalog:      MakeCatalog(),
                achievements: ach,
                matchResult:  null,          // no matchResult → assumed loss
                onMatchEnded: onMatchEnded);

            onMatchEnded.Raise();

            Assert.AreEqual(1, ach.TotalMatchesPlayed);
            Assert.AreEqual(0, ach.TotalMatchesWon);
        }

        [Test]
        public void HandleMatchEnded_UnlocksAchievement_ViaEvent()
        {
            var onMatchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(onMatchEnded);

            var def         = MakeDef("first_win", AchievementTrigger.MatchWon, targetCount: 1);
            var ach         = MakeAchievements();
            var matchResult = MakeMatchResult(playerWon: true);

            Activate(
                catalog:      MakeCatalog(def),
                achievements: ach,
                matchResult:  matchResult,
                onMatchEnded: onMatchEnded);

            onMatchEnded.Raise();

            Assert.IsTrue(ach.HasUnlocked("first_win"));
        }

        // ── PersistAchievements ───────────────────────────────────────────────

        [Test]
        public void PersistAchievements_WritesCountersToSaveData()
        {
            var ach = MakeAchievements();
            ach.RecordMatchResult(playerWon: true);
            ach.RecordMatchResult(playerWon: false);

            Activate(catalog: MakeCatalog(), achievements: ach);
            _manager.PersistAchievements();

            SaveData save = SaveSystem.Load();
            Assert.AreEqual(2, save.totalMatchesPlayed);
            Assert.AreEqual(1, save.totalMatchesWon);
        }

        [Test]
        public void PersistAchievements_WritesUnlockedIds()
        {
            var ach = MakeAchievements();
            ach.Unlock("ach_001");
            ach.Unlock("ach_002");

            Activate(catalog: MakeCatalog(), achievements: ach);
            _manager.PersistAchievements();

            SaveData save = SaveSystem.Load();
            Assert.AreEqual(2, save.unlockedAchievementIds.Count);
            Assert.IsTrue(save.unlockedAchievementIds.Contains("ach_001"));
            Assert.IsTrue(save.unlockedAchievementIds.Contains("ach_002"));
        }

        [Test]
        public void PersistAchievements_NullAchievements_DoesNotThrow()
        {
            Activate(catalog: MakeCatalog());  // no _playerAchievements
            Assert.DoesNotThrow(() => _manager.PersistAchievements());
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersMatchEndedCallback()
        {
            var onMatchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            _extraSOs.Add(onMatchEnded);
            var ach = MakeAchievements();

            Activate(
                catalog:      MakeCatalog(),
                achievements: ach,
                onMatchEnded: onMatchEnded);

            _go.SetActive(false);   // triggers OnDisable
            onMatchEnded.Raise();   // should not call HandleMatchEnded

            // If HandleMatchEnded ran, TotalMatchesPlayed would be 1.
            Assert.AreEqual(0, ach.TotalMatchesPlayed,
                "AchievementManager should not respond to MatchEnded after being disabled.");
        }
    }
}

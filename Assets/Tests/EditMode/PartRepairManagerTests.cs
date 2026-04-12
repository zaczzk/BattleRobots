using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartRepairManager"/>.
    ///
    /// Uses the inactive-GameObject pattern so fields are injected before Awake/OnEnable.
    /// SaveSystem.Delete() is called in SetUp and TearDown to keep persistence tests isolated.
    ///
    /// Covers:
    ///   • RepairPart: null registry → false; null wallet → false; null config → false;
    ///     unknown partId → false; already full HP → false; insufficient funds → false;
    ///     success deducts correct amount; success heals condition to full;
    ///     success fires _onRepairApplied; null _onRepairApplied → no throw.
    ///   • RepairAll: null registry → 0; skips unaffordable parts, returns affordable spend.
    ///   • GetRepairCost: null registry → 0; known part → expected cost.
    /// </summary>
    public class PartRepairManagerTests
    {
        private GameObject          _go;
        private PartRepairManager   _manager;
        private PartConditionRegistry _registry;
        private PlayerWallet        _wallet;
        private PartRepairConfig    _config;
        private PartConditionSO     _cond;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PlayerWallet MakeWallet(int balance)
        {
            var w = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(w, "_startingBalance", balance);
            w.Reset();
            return w;
        }

        private static PartConditionSO MakeCondition(float maxHP = 50f)
        {
            var so = ScriptableObject.CreateInstance<PartConditionSO>();
            SetField(so, "_maxHP", maxHP);
            so.LoadSnapshot(1f);
            return so;
        }

        private static PartRepairConfig MakeConfig(float rate = 2f)
        {
            var cfg = ScriptableObject.CreateInstance<PartRepairConfig>();
            SetField(cfg, "_creditsPerHPPoint", rate);
            return cfg;
        }

        private static PartConditionRegistry MakeRegistry(
            List<PartConditionRegistry.PartConditionEntry> entries)
        {
            var reg = ScriptableObject.CreateInstance<PartConditionRegistry>();
            SetField(reg, "_entries", entries);
            return reg;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            _go = new GameObject();
            _go.SetActive(false);
            _manager = _go.AddComponent<PartRepairManager>();

            _cond     = MakeCondition(50f);       // MaxHP = 50, full health
            _wallet   = MakeWallet(500);           // 500 credits
            _config   = MakeConfig(2f);            // 2 credits/HP
            _registry = MakeRegistry(new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry
                    { partId = "arm_01", condition = _cond },
            });

            SetField(_manager, "_registry",     _registry);
            SetField(_manager, "_wallet",        _wallet);
            SetField(_manager, "_repairConfig",  _config);

            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_registry);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_cond);
            _go       = null;
            _manager  = null;
            _registry = null;
            _wallet   = null;
            _config   = null;
            _cond     = null;
        }

        // ── RepairPart — guard conditions ─────────────────────────────────────

        [Test]
        public void RepairPart_NullRegistry_ReturnsFalse()
        {
            SetField(_manager, "_registry", null);
            Assert.IsFalse(_manager.RepairPart("arm_01"));
        }

        [Test]
        public void RepairPart_NullWallet_ReturnsFalse()
        {
            _cond.TakeDamage(10f);
            SetField(_manager, "_wallet", null);
            Assert.IsFalse(_manager.RepairPart("arm_01"));
        }

        [Test]
        public void RepairPart_NullConfig_ReturnsFalse()
        {
            _cond.TakeDamage(10f);
            SetField(_manager, "_repairConfig", null);
            Assert.IsFalse(_manager.RepairPart("arm_01"));
        }

        [Test]
        public void RepairPart_UnknownPartId_ReturnsFalse()
        {
            Assert.IsFalse(_manager.RepairPart("no_such_part"));
        }

        [Test]
        public void RepairPart_AlreadyFullHP_ReturnsFalse()
        {
            // _cond is at full HP — nothing to repair.
            Assert.IsFalse(_manager.RepairPart("arm_01"));
        }

        [Test]
        public void RepairPart_InsufficientFunds_ReturnsFalse()
        {
            // Missing 50 HP × 2 credits = 100 credits cost; wallet has only 5.
            _cond.TakeDamage(50f);
            var poorWallet = MakeWallet(5);
            SetField(_manager, "_wallet", poorWallet);

            bool result = _manager.RepairPart("arm_01");

            Assert.IsFalse(result);
            Object.DestroyImmediate(poorWallet);
        }

        // ── RepairPart — success path ─────────────────────────────────────────

        [Test]
        public void RepairPart_Success_DeductsCorrectAmountFromWallet()
        {
            // Missing 20 HP × 2 credits = 40 credits cost; wallet has 500.
            _cond.TakeDamage(20f);
            int balanceBefore = _wallet.Balance;

            _manager.RepairPart("arm_01");

            Assert.AreEqual(balanceBefore - 40, _wallet.Balance);
        }

        [Test]
        public void RepairPart_Success_HealsConditionToFullHP()
        {
            _cond.TakeDamage(25f);

            bool result = _manager.RepairPart("arm_01");

            Assert.IsTrue(result);
            Assert.AreEqual(_cond.MaxHP, _cond.CurrentHP, 0.001f);
            Assert.IsFalse(_cond.IsDestroyed);
        }

        [Test]
        public void RepairPart_Success_FiresOnRepairApplied()
        {
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            evt.RegisterCallback(() => count++);
            SetField(_manager, "_onRepairApplied", evt);

            _cond.TakeDamage(10f);
            _manager.RepairPart("arm_01");

            Assert.AreEqual(1, count, "_onRepairApplied should fire once on success.");
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void RepairPart_NullOnRepairApplied_DoesNotThrow()
        {
            SetField(_manager, "_onRepairApplied", null);
            _cond.TakeDamage(10f);

            Assert.DoesNotThrow(() => _manager.RepairPart("arm_01"));
        }

        // ── RepairAll ─────────────────────────────────────────────────────────

        [Test]
        public void RepairAll_NullRegistry_ReturnsZero()
        {
            SetField(_manager, "_registry", null);
            Assert.AreEqual(0, _manager.RepairAll());
        }

        [Test]
        public void RepairAll_SkipsUnaffordableParts_ReturnsAffordableSpend()
        {
            // Part A: MaxHP 50, damaged 10 HP → cost = 20 credits (affordable at 25)
            // Part B: MaxHP 50, damaged 50 HP → cost = 100 credits (unaffordable at 25)
            var condA = MakeCondition(50f);
            condA.TakeDamage(10f);
            var condB = MakeCondition(50f);
            condB.TakeDamage(50f);

            var registry = MakeRegistry(new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "a", condition = condA },
                new PartConditionRegistry.PartConditionEntry { partId = "b", condition = condB },
            });
            SetField(_manager, "_registry", registry);

            var poorWallet = MakeWallet(25); // can afford A (20) but not B (100)
            SetField(_manager, "_wallet", poorWallet);

            int spent = _manager.RepairAll();

            Assert.AreEqual(20, spent, "Only part A should be repaired.");
            Assert.AreEqual(_cond.MaxHP, condA.CurrentHP, 0.001f,
                "Part A should be at full HP.");
            Assert.IsTrue(condB.IsDestroyed,
                "Part B should remain destroyed (unaffordable).");

            Object.DestroyImmediate(condA);
            Object.DestroyImmediate(condB);
            Object.DestroyImmediate(registry);
            Object.DestroyImmediate(poorWallet);
        }

        // ── GetRepairCost ─────────────────────────────────────────────────────

        [Test]
        public void GetRepairCost_NullRegistry_ReturnsZero()
        {
            SetField(_manager, "_registry", null);
            Assert.AreEqual(0, _manager.GetRepairCost("arm_01"));
        }

        [Test]
        public void GetRepairCost_KnownDamagedPart_ReturnsExpectedCost()
        {
            // Missing 20 HP × 2 credits = 40
            _cond.TakeDamage(20f);
            Assert.AreEqual(40, _manager.GetRepairCost("arm_01"));
        }
    }
}

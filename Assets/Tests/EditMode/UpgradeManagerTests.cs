using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="UpgradeManager.UpgradePart"/>,
    /// <see cref="UpgradeManager.GetCurrentTier"/>, and
    /// <see cref="UpgradeManager.GetNextUpgradeCost"/>.
    ///
    /// Covers every guard path in UpgradePart():
    ///   • null PartDefinition argument
    ///   • missing PlayerWallet / PlayerPartUpgrades / PartUpgradeConfig
    ///   • part already at max tier
    ///   • insufficient funds
    ///   • successful upgrade — wallet deduction, tier increment, event fire, disk persist
    ///
    /// Covers GetCurrentTier() and GetNextUpgradeCost():
    ///   • null part → expected defaults
    ///   • null upgrades → expected defaults
    ///   • valid part at various tiers
    ///
    /// UpgradeManager is a MonoBehaviour; a headless GameObject is created in
    /// SetUp and destroyed in TearDown.  Private fields are injected via reflection.
    /// </summary>
    public class UpgradeManagerTests
    {
        // ── Scene / MB objects ────────────────────────────────────────────────
        private GameObject     _go;
        private UpgradeManager _manager;

        // ── ScriptableObjects ─────────────────────────────────────────────────
        private PlayerWallet       _wallet;
        private PlayerPartUpgrades _upgrades;
        private PartUpgradeConfig  _config;
        private PartDefinition     _part;
        private VoidGameEvent      _upgradeEvent;
        private VoidGameEvent      _balanceEvent;
        private VoidGameEvent      _upgradesChangedEvent;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetConfigField(PartUpgradeConfig cfg, string name, object value)
        {
            FieldInfo fi = typeof(PartUpgradeConfig)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on PartUpgradeConfig.");
            fi.SetValue(cfg, value);
        }

        private static void SetPartField(PartDefinition part, string name, object value)
        {
            FieldInfo fi = typeof(PartDefinition)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on PartDefinition.");
            fi.SetValue(part, value);
        }

        [SetUp]
        public void SetUp()
        {
            // MonoBehaviour host
            _go      = new GameObject("UpgradeManagerHost");
            _manager = _go.AddComponent<UpgradeManager>();

            // Wallet
            _balanceEvent = ScriptableObject.CreateInstance<VoidGameEvent>();
            _wallet       = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(_wallet, "_startingBalance", 1000);
            _wallet.Reset(); // initialise Balance

            // PartUpgradeConfig: maxTier=3, costs=[100,250,500], mults=[1.0,1.1,1.25,1.5]
            _config = ScriptableObject.CreateInstance<PartUpgradeConfig>();
            SetConfigField(_config, "_maxTier",             3);
            SetConfigField(_config, "_tierCosts",           new int[]   { 100, 250, 500 });
            SetConfigField(_config, "_tierStatMultipliers", new float[] { 1.0f, 1.1f, 1.25f, 1.5f });

            // PlayerPartUpgrades
            _upgradesChangedEvent = ScriptableObject.CreateInstance<VoidGameEvent>();
            _upgrades             = ScriptableObject.CreateInstance<PlayerPartUpgrades>();
            SetField(_upgrades, "_onUpgradesChanged", _upgradesChangedEvent);

            // PartDefinition
            _part = ScriptableObject.CreateInstance<PartDefinition>();
            SetPartField(_part, "_partId",      "arm_heavy");
            SetPartField(_part, "_displayName", "Heavy Arm");
            SetPartField(_part, "_cost",        200);

            // Completion event
            _upgradeEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            // Wire UpgradeManager
            SetField(_manager, "_wallet",             _wallet);
            SetField(_manager, "_upgrades",           _upgrades);
            SetField(_manager, "_upgradeConfig",      _config);
            SetField(_manager, "_onUpgradeCompleted", _upgradeEvent);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_upgrades);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_upgradeEvent);
            Object.DestroyImmediate(_balanceEvent);
            Object.DestroyImmediate(_upgradesChangedEvent);
        }

        // ── UpgradePart — guard conditions ────────────────────────────────────

        [Test]
        public void UpgradePart_NullPart_ReturnsFalse()
        {
            Assert.IsFalse(_manager.UpgradePart(null));
        }

        [Test]
        public void UpgradePart_NullWallet_ReturnsFalse()
        {
            SetField(_manager, "_wallet", null);
            Assert.IsFalse(_manager.UpgradePart(_part));
        }

        [Test]
        public void UpgradePart_NullUpgrades_ReturnsFalse()
        {
            SetField(_manager, "_upgrades", null);
            Assert.IsFalse(_manager.UpgradePart(_part));
        }

        [Test]
        public void UpgradePart_NullConfig_ReturnsFalse()
        {
            SetField(_manager, "_upgradeConfig", null);
            Assert.IsFalse(_manager.UpgradePart(_part));
        }

        [Test]
        public void UpgradePart_AlreadyAtMaxTier_ReturnsFalse()
        {
            _upgrades.SetTier("arm_heavy", 3); // maxTier = 3
            Assert.IsFalse(_manager.UpgradePart(_part));
        }

        [Test]
        public void UpgradePart_AlreadyAtMaxTier_WalletUnchanged()
        {
            _upgrades.SetTier("arm_heavy", 3);
            int before = _wallet.Balance;
            _manager.UpgradePart(_part);
            Assert.AreEqual(before, _wallet.Balance);
        }

        [Test]
        public void UpgradePart_InsufficientFunds_ReturnsFalse()
        {
            // Drain wallet so balance < 100 (cost to tier 1)
            _wallet.Deduct(950); // balance = 50
            Assert.IsFalse(_manager.UpgradePart(_part));
        }

        [Test]
        public void UpgradePart_InsufficientFunds_TierUnchanged()
        {
            _wallet.Deduct(950); // balance = 50
            _manager.UpgradePart(_part);
            Assert.AreEqual(0, _upgrades.GetTier("arm_heavy"));
        }

        // ── UpgradePart — success path ────────────────────────────────────────

        [Test]
        public void UpgradePart_Success_ReturnsTrue()
        {
            Assert.IsTrue(_manager.UpgradePart(_part));
        }

        [Test]
        public void UpgradePart_Success_DeductsCorrectCost()
        {
            int before = _wallet.Balance;
            _manager.UpgradePart(_part); // tier 0 → 1, cost = 100
            Assert.AreEqual(before - 100, _wallet.Balance);
        }

        [Test]
        public void UpgradePart_Success_IncreasesTier()
        {
            _manager.UpgradePart(_part);
            Assert.AreEqual(1, _upgrades.GetTier("arm_heavy"));
        }

        [Test]
        public void UpgradePart_Success_FiresCompletionEvent()
        {
            bool fired = false;
            _upgradeEvent.RegisterCallback(() => fired = true);
            _manager.UpgradePart(_part);
            Assert.IsTrue(fired);
        }

        [Test]
        public void UpgradePart_Success_PersistsToDisk()
        {
            _manager.UpgradePart(_part);
            SaveData save = SaveSystem.Load();
            bool found = false;
            for (int i = 0; i < save.upgradePartIds.Count; i++)
            {
                if (save.upgradePartIds[i] == "arm_heavy" && save.upgradePartTierValues[i] == 1)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Expected arm_heavy tier 1 in persisted save data.");
        }

        [Test]
        public void UpgradePart_CalledTwice_TierAdvancesTwice()
        {
            _manager.UpgradePart(_part); // tier 0 → 1, cost 100
            _manager.UpgradePart(_part); // tier 1 → 2, cost 250
            Assert.AreEqual(2, _upgrades.GetTier("arm_heavy"));
            Assert.AreEqual(1000 - 100 - 250, _wallet.Balance);
        }

        // ── GetCurrentTier ────────────────────────────────────────────────────

        [Test]
        public void GetCurrentTier_NullPart_ReturnsZero()
        {
            Assert.AreEqual(0, _manager.GetCurrentTier(null));
        }

        [Test]
        public void GetCurrentTier_NullUpgrades_ReturnsZero()
        {
            SetField(_manager, "_upgrades", null);
            Assert.AreEqual(0, _manager.GetCurrentTier(_part));
        }

        [Test]
        public void GetCurrentTier_AfterUpgrade_ReturnsNewTier()
        {
            _manager.UpgradePart(_part);
            Assert.AreEqual(1, _manager.GetCurrentTier(_part));
        }

        // ── GetNextUpgradeCost ────────────────────────────────────────────────

        [Test]
        public void GetNextUpgradeCost_NullPart_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, _manager.GetNextUpgradeCost(null));
        }

        [Test]
        public void GetNextUpgradeCost_AtTier0_ReturnsFirstCost()
        {
            Assert.AreEqual(100, _manager.GetNextUpgradeCost(_part));
        }

        [Test]
        public void GetNextUpgradeCost_AtMaxTier_ReturnsNegativeOne()
        {
            _upgrades.SetTier("arm_heavy", 3);
            Assert.AreEqual(-1, _manager.GetNextUpgradeCost(_part));
        }
    }
}

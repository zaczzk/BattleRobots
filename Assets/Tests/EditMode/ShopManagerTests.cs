using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShopManager.BuyPart"/> and
    /// <see cref="ShopManager.IsOwned"/>.
    ///
    /// Covers every guard path in BuyPart():
    ///   • null PartDefinition argument
    ///   • missing PlayerWallet SO
    ///   • already-owned gate (requires _inventory)
    ///   • insufficient funds
    ///   • successful purchase — wallet deduction, inventory unlock, event fire, disk persist
    ///   • backwards-compatibility: no inventory assigned → re-purchase allowed
    ///
    /// Covers IsOwned():
    ///   • null part → false
    ///   • no inventory → false
    ///   • part not owned → false
    ///   • part owned → true
    ///
    /// ShopManager is a MonoBehaviour; a headless <see cref="GameObject"/> is created in
    /// SetUp and destroyed in TearDown.  Private serialised fields are injected via
    /// reflection — the same pattern used throughout this test suite.
    /// </summary>
    public class ShopManagerTests
    {
        // ── Scene / MB objects ────────────────────────────────────────────────
        private GameObject   _go;
        private ShopManager  _shop;

        // ── ScriptableObjects ─────────────────────────────────────────────────
        private PlayerWallet    _wallet;
        private PlayerInventory _inventory;
        private PartDefinition  _part;
        private VoidGameEvent   _purchaseEvent;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Set a private field declared on PartDefinition (works even though the class is sealed).
        /// </summary>
        private static void SetPartField(PartDefinition part, string fieldName, object value)
        {
            FieldInfo fi = typeof(PartDefinition)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on PartDefinition.");
            fi.SetValue(part, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestShopManager");
            _shop = _go.AddComponent<ShopManager>();

            // Wallet — reset to 500 default starting balance.
            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            _wallet.Reset();

            // Inventory — fresh empty instance.
            _inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            // Part with cost 100, stable id "test_part".
            _part = ScriptableObject.CreateInstance<PartDefinition>();
            SetPartField(_part, "_partId",      "test_part");
            SetPartField(_part, "_displayName", "Test Part");
            SetPartField(_part, "_cost",        100);

            // Purchase-completed event.
            _purchaseEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            // Wire mandatory fields.
            SetField(_shop, "_wallet",              _wallet);
            SetField(_shop, "_inventory",           _inventory);
            SetField(_shop, "_onPurchaseCompleted", _purchaseEvent);
            // _catalog is not exercised by BuyPart/IsOwned; leave null.
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_inventory);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_purchaseEvent);
            _go            = null;
            _shop          = null;
            _wallet        = null;
            _inventory     = null;
            _part          = null;
            _purchaseEvent = null;
        }

        // ── BuyPart — guard paths ─────────────────────────────────────────────

        [Test]
        public void BuyPart_NullPart_ReturnsFalse()
        {
            bool result = _shop.BuyPart(null);
            Assert.IsFalse(result);
        }

        [Test]
        public void BuyPart_NullPart_WalletUnchanged()
        {
            _shop.BuyPart(null);
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void BuyPart_WalletNotAssigned_ReturnsFalse()
        {
            SetField(_shop, "_wallet", null);
            bool result = _shop.BuyPart(_part);
            Assert.IsFalse(result);
        }

        [Test]
        public void BuyPart_AlreadyOwned_ReturnsFalse()
        {
            _inventory.UnlockPart("test_part");   // pre-own the part
            bool result = _shop.BuyPart(_part);
            Assert.IsFalse(result);
        }

        [Test]
        public void BuyPart_AlreadyOwned_WalletUnchanged()
        {
            _inventory.UnlockPart("test_part");
            _shop.BuyPart(_part);
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void BuyPart_InsufficientFunds_ReturnsFalse()
        {
            // Part costs 100; set cost above wallet balance.
            SetPartField(_part, "_cost", 9999);
            bool result = _shop.BuyPart(_part);
            Assert.IsFalse(result);
        }

        [Test]
        public void BuyPart_InsufficientFunds_WalletUnchanged()
        {
            SetPartField(_part, "_cost", 9999);
            _shop.BuyPart(_part);
            Assert.AreEqual(500, _wallet.Balance);
        }

        // ── BuyPart — success path ────────────────────────────────────────────

        [Test]
        public void BuyPart_Success_ReturnsTrue()
        {
            bool result = _shop.BuyPart(_part);
            Assert.IsTrue(result);
        }

        [Test]
        public void BuyPart_Success_DeductsFromWallet()
        {
            _shop.BuyPart(_part);
            Assert.AreEqual(400, _wallet.Balance);   // 500 - 100
        }

        [Test]
        public void BuyPart_Success_UnlocksInInventory()
        {
            _shop.BuyPart(_part);
            Assert.IsTrue(_inventory.HasPart("test_part"));
        }

        [Test]
        public void BuyPart_Success_FiresPurchaseEvent()
        {
            int callCount = 0;
            _purchaseEvent.RegisterCallback(() => callCount++);

            _shop.BuyPart(_part);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void BuyPart_Success_PersistedWalletBalance()
        {
            // After a successful purchase PersistPurchase() writes the wallet balance to disk.
            // Load the save file back and verify the balance matches.
            _shop.BuyPart(_part);

            SaveData saved = SaveSystem.Load();
            Assert.AreEqual(400, saved.walletBalance);
        }

        // ── BuyPart — backwards compatibility: no inventory ───────────────────

        [Test]
        public void BuyPart_NoInventory_CanRepurchasePart()
        {
            // With _inventory null the already-owned gate is skipped.
            SetField(_shop, "_inventory", null);

            bool first  = _shop.BuyPart(_part);
            _wallet.AddFunds(100);          // restore funds so the second buy can succeed
            bool second = _shop.BuyPart(_part);

            Assert.IsTrue(first,  "first purchase should succeed");
            Assert.IsTrue(second, "second purchase should succeed without inventory gate");
        }

        // ── IsOwned ───────────────────────────────────────────────────────────

        [Test]
        public void IsOwned_NullPart_ReturnsFalse()
        {
            Assert.IsFalse(_shop.IsOwned(null));
        }

        [Test]
        public void IsOwned_NoInventoryAssigned_ReturnsFalse()
        {
            SetField(_shop, "_inventory", null);
            Assert.IsFalse(_shop.IsOwned(_part));
        }

        [Test]
        public void IsOwned_PartNotInInventory_ReturnsFalse()
        {
            // Inventory is empty — part has not been purchased.
            Assert.IsFalse(_shop.IsOwned(_part));
        }

        [Test]
        public void IsOwned_PartInInventory_ReturnsTrue()
        {
            _inventory.UnlockPart("test_part");
            Assert.IsTrue(_shop.IsOwned(_part));
        }
    }
}

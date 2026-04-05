using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerWallet"/> ScriptableObject.
    ///
    /// A wallet instance is created per-test via <see cref="ScriptableObject.CreateInstance{T}"/>
    /// and destroyed in TearDown. Event channel fields default to null, which is safe
    /// because all raise calls use the null-conditional operator.
    /// </summary>
    [TestFixture]
    public sealed class PlayerWalletTests
    {
        private PlayerWallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            // Default _startingBalance serialized field value is 500.
            _wallet.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_wallet);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsBalanceToStartingBalance()
        {
            Assert.AreEqual(500, _wallet.Balance,
                "After Reset(), Balance should equal the default _startingBalance (500).");
        }

        // ── AddFunds ──────────────────────────────────────────────────────────

        [Test]
        public void AddFunds_PositiveAmount_IncreasesBalance()
        {
            _wallet.AddFunds(200);
            Assert.AreEqual(700, _wallet.Balance);
        }

        [Test]
        public void AddFunds_Zero_DoesNotChangeBalance()
        {
            _wallet.AddFunds(0);
            Assert.AreEqual(500, _wallet.Balance,
                "AddFunds(0) should be ignored — balance must not change.");
        }

        [Test]
        public void AddFunds_NegativeAmount_DoesNotChangeBalance()
        {
            _wallet.AddFunds(-100);
            Assert.AreEqual(500, _wallet.Balance,
                "AddFunds with negative amount should be ignored.");
        }

        [Test]
        public void AddFunds_MultipleCallsAccumulate()
        {
            _wallet.AddFunds(100);
            _wallet.AddFunds(50);
            _wallet.AddFunds(25);
            Assert.AreEqual(675, _wallet.Balance);
        }

        // ── Deduct ────────────────────────────────────────────────────────────

        [Test]
        public void Deduct_SufficientFunds_ReturnsTrueAndDeductsAmount()
        {
            bool result = _wallet.Deduct(300);
            Assert.IsTrue(result, "Deduct should succeed when Balance >= amount.");
            Assert.AreEqual(200, _wallet.Balance);
        }

        [Test]
        public void Deduct_ExactBalance_ReturnsTrueAndZerosBalance()
        {
            bool result = _wallet.Deduct(500);
            Assert.IsTrue(result);
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void Deduct_InsufficientFunds_ReturnsFalseAndDoesNotChangeBalance()
        {
            bool result = _wallet.Deduct(501);
            Assert.IsFalse(result, "Deduct should fail when amount > Balance.");
            Assert.AreEqual(500, _wallet.Balance, "Balance must be unchanged on failed Deduct.");
        }

        [Test]
        public void Deduct_ZeroAmount_ReturnsFalseAndDoesNotChangeBalance()
        {
            bool result = _wallet.Deduct(0);
            Assert.IsFalse(result, "Deduct(0) should be rejected.");
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void Deduct_NegativeAmount_ReturnsFalseAndDoesNotChangeBalance()
        {
            bool result = _wallet.Deduct(-50);
            Assert.IsFalse(result, "Deduct with negative amount should be rejected.");
            Assert.AreEqual(500, _wallet.Balance);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsBalanceDirectly()
        {
            // LoadSnapshot is internal; access via reflection is intentional for testing.
            _wallet.LoadSnapshot(1234);
            Assert.AreEqual(1234, _wallet.Balance,
                "LoadSnapshot should set Balance to the provided value.");
        }

        // ── Interaction ───────────────────────────────────────────────────────

        [Test]
        public void AddThenDeduct_ProducesCorrectBalance()
        {
            _wallet.AddFunds(500);   // 1000
            _wallet.Deduct(200);     // 800
            _wallet.AddFunds(100);   // 900
            _wallet.Deduct(900);     // 0
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void Reset_AfterMutations_RestoresStartingBalance()
        {
            _wallet.AddFunds(1000);
            _wallet.Deduct(250);
            _wallet.Reset();
            Assert.AreEqual(500, _wallet.Balance,
                "Reset() should restore the starting balance regardless of prior mutations.");
        }
    }
}

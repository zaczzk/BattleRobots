using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerWallet"/>.
    ///
    /// Uses <c>ScriptableObject.CreateInstance</c> so the SO is allocated on the
    /// UnityEngine heap and properly destroyed in TearDown.
    /// The <c>_onBalanceChanged</c> event channel is left null; PlayerWallet guards
    /// all raises with <c>?.</c> so no NullReferenceException is thrown.
    /// </summary>
    public class PlayerWalletTests
    {
        private PlayerWallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            _wallet.Reset(); // Balance = 500 (field default _startingBalance)
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_wallet);
            _wallet = null;
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_SetsBalanceToFieldDefault()
        {
            // _startingBalance field defaults to 500 in PlayerWallet.cs.
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void Reset_AfterMutation_RestoresStartingBalance()
        {
            _wallet.AddFunds(1000);
            _wallet.Reset();
            Assert.AreEqual(500, _wallet.Balance);
        }

        // ── AddFunds ──────────────────────────────────────────────────────────

        [Test]
        public void AddFunds_PositiveAmount_IncreasesBalance()
        {
            _wallet.AddFunds(100);
            Assert.AreEqual(600, _wallet.Balance);
        }

        [Test]
        public void AddFunds_MultipleCalls_Accumulate()
        {
            _wallet.AddFunds(50);
            _wallet.AddFunds(50);
            Assert.AreEqual(600, _wallet.Balance);
        }

        [Test]
        public void AddFunds_ZeroAmount_BalanceUnchanged()
        {
            _wallet.AddFunds(0);
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void AddFunds_NegativeAmount_BalanceUnchanged()
        {
            _wallet.AddFunds(-50);
            Assert.AreEqual(500, _wallet.Balance);
        }

        // ── Deduct ────────────────────────────────────────────────────────────

        [Test]
        public void Deduct_SufficientFunds_ReturnsTrueAndDecreasesBalance()
        {
            bool result = _wallet.Deduct(200);
            Assert.IsTrue(result);
            Assert.AreEqual(300, _wallet.Balance);
        }

        [Test]
        public void Deduct_ExactBalance_ReturnsTrueAndResultsInZero()
        {
            bool result = _wallet.Deduct(500);
            Assert.IsTrue(result);
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void Deduct_InsufficientFunds_ReturnsFalseAndBalanceUnchanged()
        {
            bool result = _wallet.Deduct(999);
            Assert.IsFalse(result);
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void Deduct_ZeroAmount_ReturnsFalseAndBalanceUnchanged()
        {
            bool result = _wallet.Deduct(0);
            Assert.IsFalse(result);
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void Deduct_NegativeAmount_ReturnsFalseAndBalanceUnchanged()
        {
            bool result = _wallet.Deduct(-10);
            Assert.IsFalse(result);
            Assert.AreEqual(500, _wallet.Balance);
        }

        [Test]
        public void Deduct_SequentialPurchases_AccumulateCorrectly()
        {
            _wallet.Deduct(100);
            _wallet.Deduct(100);
            Assert.AreEqual(300, _wallet.Balance);
        }

        [Test]
        public void Deduct_AfterBalanceIsZero_ReturnsFalse()
        {
            _wallet.Deduct(500); // drain to zero
            bool result = _wallet.Deduct(1);
            Assert.IsFalse(result);
            Assert.AreEqual(0, _wallet.Balance);
        }
    }
}

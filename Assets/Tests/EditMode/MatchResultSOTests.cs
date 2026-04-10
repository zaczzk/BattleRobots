using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchResultSO"/>.
    ///
    /// Verifies that <see cref="MatchResultSO.Write"/> correctly stores all four
    /// result fields and that consecutive writes overwrite previous values.
    /// No scene or asset database required — CreateInstance is sufficient.
    /// </summary>
    public class MatchResultSOTests
    {
        private MatchResultSO _result;

        [SetUp]
        public void SetUp()
        {
            _result = ScriptableObject.CreateInstance<MatchResultSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_result);
            _result = null;
        }

        // ── Initial state ──────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_AllFieldsAreDefaults()
        {
            Assert.IsFalse(_result.PlayerWon);
            Assert.AreEqual(0f,  _result.DurationSeconds,  0.0001f);
            Assert.AreEqual(0,   _result.CurrencyEarned);
            Assert.AreEqual(0,   _result.NewWalletBalance);
        }

        // ── Write — player won ────────────────────────────────────────────────

        [Test]
        public void Write_PlayerWon_True_SetsPlayerWonTrue()
        {
            _result.Write(playerWon: true, durationSeconds: 60f, currencyEarned: 200, newWalletBalance: 700);
            Assert.IsTrue(_result.PlayerWon);
        }

        [Test]
        public void Write_PlayerWon_False_SetsPlayerWonFalse()
        {
            _result.Write(playerWon: false, durationSeconds: 30f, currencyEarned: 50, newWalletBalance: 550);
            Assert.IsFalse(_result.PlayerWon);
        }

        // ── Write — duration ──────────────────────────────────────────────────

        [Test]
        public void Write_StoresDurationSeconds()
        {
            _result.Write(playerWon: true, durationSeconds: 87.5f, currencyEarned: 200, newWalletBalance: 700);
            Assert.AreEqual(87.5f, _result.DurationSeconds, 0.0001f);
        }

        [Test]
        public void Write_ZeroDuration_StoresZero()
        {
            _result.Write(playerWon: false, durationSeconds: 0f, currencyEarned: 50, newWalletBalance: 550);
            Assert.AreEqual(0f, _result.DurationSeconds, 0.0001f);
        }

        // ── Write — currency & balance ────────────────────────────────────────

        [Test]
        public void Write_StoresCurrencyEarned()
        {
            _result.Write(playerWon: true, durationSeconds: 45f, currencyEarned: 300, newWalletBalance: 800);
            Assert.AreEqual(300, _result.CurrencyEarned);
        }

        [Test]
        public void Write_ZeroCurrencyEarned_StoresZero()
        {
            _result.Write(playerWon: false, durationSeconds: 45f, currencyEarned: 0, newWalletBalance: 500);
            Assert.AreEqual(0, _result.CurrencyEarned);
        }

        [Test]
        public void Write_StoresNewWalletBalance()
        {
            _result.Write(playerWon: true, durationSeconds: 45f, currencyEarned: 200, newWalletBalance: 1234);
            Assert.AreEqual(1234, _result.NewWalletBalance);
        }

        [Test]
        public void Write_ZeroWalletBalance_StoresZero()
        {
            _result.Write(playerWon: false, durationSeconds: 45f, currencyEarned: 50, newWalletBalance: 0);
            Assert.AreEqual(0, _result.NewWalletBalance);
        }

        // ── Write — overwrite ─────────────────────────────────────────────────

        [Test]
        public void Write_CalledTwice_OverwritesPreviousValues()
        {
            _result.Write(playerWon: true,  durationSeconds: 30f, currencyEarned: 200, newWalletBalance: 700);
            _result.Write(playerWon: false, durationSeconds: 90f, currencyEarned: 50,  newWalletBalance: 300);

            Assert.IsFalse(_result.PlayerWon);
            Assert.AreEqual(90f, _result.DurationSeconds,  0.0001f);
            Assert.AreEqual(50,  _result.CurrencyEarned);
            Assert.AreEqual(300, _result.NewWalletBalance);
        }

        // ── Write — all fields together ───────────────────────────────────────

        [Test]
        public void Write_AllFields_StoredCorrectly()
        {
            _result.Write(playerWon: true, durationSeconds: 112.3f, currencyEarned: 450, newWalletBalance: 950);

            Assert.IsTrue(_result.PlayerWon);
            Assert.AreEqual(112.3f, _result.DurationSeconds,  0.001f);
            Assert.AreEqual(450,    _result.CurrencyEarned);
            Assert.AreEqual(950,    _result.NewWalletBalance);
        }
    }
}

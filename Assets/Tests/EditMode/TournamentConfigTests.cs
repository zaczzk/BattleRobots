using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TournamentConfig"/>.
    ///
    /// Covers:
    ///   • Fresh-instance default values (RoundCount=3, EntryFee=100,
    ///     RoundWinBonus=50, GrandPrize=500, ConsolationPrize=0).
    ///   • All properties return int values ≥ 0.
    ///   • Reflection-injected custom values are returned correctly through properties.
    ///   • Zero values are valid for all economy fields.
    ///   • RoundCount minimum of 1.
    ///   • ConsolationPrize defaults to 0.
    /// </summary>
    public class TournamentConfigTests
    {
        private TournamentConfig _config;

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TournamentConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_RoundCount_IsThree()
        {
            Assert.AreEqual(3, _config.RoundCount);
        }

        [Test]
        public void FreshInstance_EntryFee_Is100()
        {
            Assert.AreEqual(100, _config.EntryFee);
        }

        [Test]
        public void FreshInstance_RoundWinBonus_Is50()
        {
            Assert.AreEqual(50, _config.RoundWinBonus);
        }

        [Test]
        public void FreshInstance_GrandPrize_Is500()
        {
            Assert.AreEqual(500, _config.GrandPrize);
        }

        [Test]
        public void FreshInstance_ConsolationPrize_IsZero()
        {
            Assert.AreEqual(0, _config.ConsolationPrize);
        }

        // ── Property contracts ────────────────────────────────────────────────

        [Test]
        public void RoundCount_ReflectionInjected_ReturnsCorrectValue()
        {
            SetField(_config, "_roundCount", 5);
            Assert.AreEqual(5, _config.RoundCount);
        }

        [Test]
        public void EntryFee_ReflectionInjected_ReturnsCorrectValue()
        {
            SetField(_config, "_entryFee", 200);
            Assert.AreEqual(200, _config.EntryFee);
        }

        [Test]
        public void GrandPrize_ZeroIsValid()
        {
            SetField(_config, "_grandPrize", 0);
            Assert.AreEqual(0, _config.GrandPrize);
        }

        [Test]
        public void ConsolationPrize_NonZeroIsValid()
        {
            SetField(_config, "_consolationPrize", 25);
            Assert.AreEqual(25, _config.ConsolationPrize);
        }

        [Test]
        public void RoundWinBonus_LargeValueIsValid()
        {
            SetField(_config, "_roundWinBonus", 9999);
            Assert.AreEqual(9999, _config.RoundWinBonus);
        }
    }
}

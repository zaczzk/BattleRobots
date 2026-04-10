using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartUpgradeConfig"/>.
    ///
    /// Covers:
    ///   • MaxTier property matches inspector value
    ///   • GetUpgradeCost returns correct cost per tier or -1 at boundaries
    ///   • GetStatMultiplier returns correct multiplier per tier or 1.0 out-of-range
    ///   • TierCosts and TierStatMultipliers length contracts
    /// </summary>
    public class PartUpgradeConfigTests
    {
        private PartUpgradeConfig _config;

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PartUpgradeConfig>();
            SetField(_config, "_maxTier",            3);
            SetField(_config, "_tierCosts",          new int[]   { 100, 250, 500 });
            SetField(_config, "_tierStatMultipliers", new float[] { 1.0f, 1.1f, 1.25f, 1.5f });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // ── MaxTier ───────────────────────────────────────────────────────────

        [Test]
        public void MaxTier_ReturnsConfiguredValue()
        {
            Assert.AreEqual(3, _config.MaxTier);
        }

        // ── GetUpgradeCost ────────────────────────────────────────────────────

        [Test]
        public void GetUpgradeCost_Tier0_ReturnsFirstCost()
        {
            Assert.AreEqual(100, _config.GetUpgradeCost(0));
        }

        [Test]
        public void GetUpgradeCost_Tier1_ReturnsSecondCost()
        {
            Assert.AreEqual(250, _config.GetUpgradeCost(1));
        }

        [Test]
        public void GetUpgradeCost_Tier2_ReturnsThirdCost()
        {
            Assert.AreEqual(500, _config.GetUpgradeCost(2));
        }

        [Test]
        public void GetUpgradeCost_AtMaxTier_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, _config.GetUpgradeCost(3)); // already at max (tier 3)
        }

        [Test]
        public void GetUpgradeCost_NegativeTier_ReturnsNegativeOne()
        {
            Assert.AreEqual(-1, _config.GetUpgradeCost(-1));
        }

        // ── GetStatMultiplier ─────────────────────────────────────────────────

        [Test]
        public void GetStatMultiplier_Tier0_Returns1_0()
        {
            Assert.AreEqual(1.0f, _config.GetStatMultiplier(0), 0.001f);
        }

        [Test]
        public void GetStatMultiplier_Tier1_ReturnsCorrectValue()
        {
            Assert.AreEqual(1.1f, _config.GetStatMultiplier(1), 0.001f);
        }

        [Test]
        public void GetStatMultiplier_MaxTier_ReturnsLastValue()
        {
            Assert.AreEqual(1.5f, _config.GetStatMultiplier(3), 0.001f);
        }

        [Test]
        public void GetStatMultiplier_OutOfRange_High_Returns1_0()
        {
            Assert.AreEqual(1.0f, _config.GetStatMultiplier(99), 0.001f);
        }

        [Test]
        public void GetStatMultiplier_OutOfRange_Negative_Returns1_0()
        {
            Assert.AreEqual(1.0f, _config.GetStatMultiplier(-1), 0.001f);
        }

        // ── Array length contracts ────────────────────────────────────────────

        [Test]
        public void TierCosts_Length_EqualsMaxTier()
        {
            Assert.AreEqual(_config.MaxTier, _config.TierCosts.Count);
        }

        [Test]
        public void TierStatMultipliers_Length_EqualsMaxTierPlusOne()
        {
            Assert.AreEqual(_config.MaxTier + 1, _config.TierStatMultipliers.Count);
        }
    }
}

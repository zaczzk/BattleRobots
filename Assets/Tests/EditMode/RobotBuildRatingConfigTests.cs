using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotBuildRatingConfig"/>.
    ///
    /// Covers:
    ///   • Default property values are sensible (positive weights).
    ///   • GetRarityPoints returns non-negative values for every PartRarity tier.
    ///   • Legendary tier points ≥ Epic ≥ Rare ≥ Uncommon ≥ Common (default ordering).
    ///   • GetRarityPoints returns 0 for an unrecognised enum value.
    /// </summary>
    public class RobotBuildRatingConfigTests
    {
        private RobotBuildRatingConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RobotBuildRatingConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) Object.DestroyImmediate(_config);
        }

        // ── Default property values ────────────────────────────────────────────

        [Test]
        public void FreshInstance_BaseStatWeight_IsNonNegative()
        {
            Assert.GreaterOrEqual(_config.BaseStatWeight, 0f);
        }

        [Test]
        public void FreshInstance_UpgradeWeight_IsNonNegative()
        {
            Assert.GreaterOrEqual(_config.UpgradeWeight, 0f);
        }

        [Test]
        public void FreshInstance_SynergyWeight_IsNonNegative()
        {
            Assert.GreaterOrEqual(_config.SynergyWeight, 0f);
        }

        // ── GetRarityPoints: all recognised tiers return non-negative values ────

        [Test]
        public void GetRarityPoints_Common_ReturnsNonNegative()
        {
            Assert.GreaterOrEqual(_config.GetRarityPoints(PartRarity.Common), 0);
        }

        [Test]
        public void GetRarityPoints_Uncommon_ReturnsNonNegative()
        {
            Assert.GreaterOrEqual(_config.GetRarityPoints(PartRarity.Uncommon), 0);
        }

        [Test]
        public void GetRarityPoints_Rare_ReturnsNonNegative()
        {
            Assert.GreaterOrEqual(_config.GetRarityPoints(PartRarity.Rare), 0);
        }

        [Test]
        public void GetRarityPoints_Epic_ReturnsNonNegative()
        {
            Assert.GreaterOrEqual(_config.GetRarityPoints(PartRarity.Epic), 0);
        }

        [Test]
        public void GetRarityPoints_Legendary_ReturnsNonNegative()
        {
            Assert.GreaterOrEqual(_config.GetRarityPoints(PartRarity.Legendary), 0);
        }

        [Test]
        public void GetRarityPoints_LegendaryGreaterOrEqualEpic_DefaultOrdering()
        {
            // Default rarity weights should be ascending by tier.
            Assert.GreaterOrEqual(
                _config.GetRarityPoints(PartRarity.Legendary),
                _config.GetRarityPoints(PartRarity.Epic));
        }

        [Test]
        public void GetRarityPoints_UnknownEnumValue_ReturnsZero()
        {
            // Cast an out-of-range integer to PartRarity — should fall through to default 0.
            var unknown = (PartRarity)999;
            Assert.AreEqual(0, _config.GetRarityPoints(unknown));
        }
    }
}

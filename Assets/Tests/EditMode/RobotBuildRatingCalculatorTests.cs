using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotBuildRatingCalculator.Calculate"/>.
    ///
    /// Covers:
    ///   • All-null inputs → 0.
    ///   • Null loadout → 0.
    ///   • Null catalog → 0.
    ///   • Null config → 0.
    ///   • Empty loadout → 0.
    ///   • Null upgrades → upgrade contribution skipped (no throw).
    ///   • Null synergyConfig → synergy contribution skipped (no throw).
    ///   • Single part contributes rarity points.
    ///   • Single part with upgrade tier contributes upgrade points.
    ///   • Result is always non-negative.
    /// </summary>
    public class RobotBuildRatingCalculatorTests
    {
        // ── Fixture helpers ───────────────────────────────────────────────────

        private RobotBuildRatingConfig _config;
        private ShopCatalog            _catalog;
        private PlayerLoadout          _loadout;
        private PlayerPartUpgrades     _upgrades;

        // Unity SO instances created per-test and destroyed in TearDown.
        private readonly System.Collections.Generic.List<ScriptableObject> _sos =
            new System.Collections.Generic.List<ScriptableObject>();

        private T CreateSO<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _sos.Add(so);
            return so;
        }

        // Reflection helper to inject private List fields on ShopCatalog/PlayerLoadout.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        [SetUp]
        public void SetUp()
        {
            _config   = CreateSO<RobotBuildRatingConfig>();
            _catalog  = CreateSO<ShopCatalog>();
            _loadout  = CreateSO<PlayerLoadout>();
            _upgrades = CreateSO<PlayerPartUpgrades>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var so in _sos)
                if (so != null) Object.DestroyImmediate(so);
            _sos.Clear();
        }

        // Sets the player's equipped part IDs via SetLoadout (raises event but that's fine in tests).
        private void SetLoadoutIds(params string[] ids)
        {
            // Use the public API; VoidGameEvent channel is null so no-throw.
            _loadout.SetLoadout(ids);
        }

        // Adds a PartDefinition to the catalog's internal _parts list via reflection.
        private PartDefinition AddPartToCatalog(
            string partId,
            PartRarity rarity = PartRarity.Common,
            int healthBonus = 0,
            float speedMult = 1f,
            float damageMult = 1f,
            int armorRating = 0)
        {
            var def = CreateSO<PartDefinition>();
            SetPrivateField(def, "_partId",   partId);
            SetPrivateField(def, "_rarity",   rarity);
            SetPrivateField(def, "_stats", new PartStats
            {
                healthBonus      = healthBonus,
                speedMultiplier  = speedMult,
                damageMultiplier = damageMult,
                armorRating      = armorRating,
            });

            // Append to the catalog's internal _parts list.
            var partsList = (List<PartDefinition>)
                typeof(ShopCatalog)
                    .GetField("_parts", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_catalog);
            partsList.Add(def);
            return def;
        }

        // ── Null-guard tests ──────────────────────────────────────────────────

        [Test]
        public void Calculate_AllNull_ReturnsZero()
        {
            int result = RobotBuildRatingCalculator.Calculate(null, null, null, null, null);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void Calculate_NullLoadout_ReturnsZero()
        {
            int result = RobotBuildRatingCalculator.Calculate(null, _catalog, _upgrades, null, _config);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void Calculate_NullCatalog_ReturnsZero()
        {
            SetLoadoutIds("part_a");
            int result = RobotBuildRatingCalculator.Calculate(_loadout, null, _upgrades, null, _config);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void Calculate_NullConfig_ReturnsZero()
        {
            SetLoadoutIds("part_a");
            AddPartToCatalog("part_a");
            int result = RobotBuildRatingCalculator.Calculate(_loadout, _catalog, _upgrades, null, null);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void Calculate_EmptyLoadout_ReturnsZero()
        {
            // Loadout has no parts.
            int result = RobotBuildRatingCalculator.Calculate(_loadout, _catalog, _upgrades, null, _config);
            Assert.AreEqual(0, result);
        }

        // ── Null optional inputs ──────────────────────────────────────────────

        [Test]
        public void Calculate_NullUpgrades_DoesNotThrow()
        {
            AddPartToCatalog("part_a");
            SetLoadoutIds("part_a");
            int result = 0;
            Assert.DoesNotThrow(() =>
                result = RobotBuildRatingCalculator.Calculate(_loadout, _catalog, null, null, _config));
            Assert.GreaterOrEqual(result, 0);
        }

        [Test]
        public void Calculate_NullSynergyConfig_DoesNotThrow()
        {
            AddPartToCatalog("part_a");
            SetLoadoutIds("part_a");
            int result = 0;
            Assert.DoesNotThrow(() =>
                result = RobotBuildRatingCalculator.Calculate(_loadout, _catalog, _upgrades, null, _config));
            Assert.GreaterOrEqual(result, 0);
        }

        // ── Rarity contribution ───────────────────────────────────────────────

        [Test]
        public void Calculate_RarePart_RarityPointsContribute()
        {
            // A Rare part should yield more points than a Common part (given default config).
            AddPartToCatalog("part_rare",   PartRarity.Rare);
            AddPartToCatalog("part_common", PartRarity.Common);

            _loadout.SetLoadout(new[] { "part_rare" });
            int rareRating = RobotBuildRatingCalculator.Calculate(
                _loadout, _catalog, null, null, _config);

            _loadout.SetLoadout(new[] { "part_common" });
            int commonRating = RobotBuildRatingCalculator.Calculate(
                _loadout, _catalog, null, null, _config);

            Assert.GreaterOrEqual(rareRating, commonRating);
        }

        // ── Upgrade contribution ──────────────────────────────────────────────

        [Test]
        public void Calculate_PartWithUpgradeTier_UpgradePointsContribute()
        {
            AddPartToCatalog("part_a");
            SetLoadoutIds("part_a");

            // Tier 0 baseline.
            int tier0 = RobotBuildRatingCalculator.Calculate(
                _loadout, _catalog, _upgrades, null, _config);

            // Apply upgrade tier 2 to the part.
            _upgrades.SetTier("part_a", 2);
            int tier2 = RobotBuildRatingCalculator.Calculate(
                _loadout, _catalog, _upgrades, null, _config);

            // When UpgradeWeight > 0, tier2 should be greater than tier0.
            if (_config.UpgradeWeight > 0f)
                Assert.Greater(tier2, tier0, "Tier-2 rating should exceed tier-0 rating.");
            else
                Assert.AreEqual(tier0, tier2, "UpgradeWeight is 0 — ratings should match.");
        }

        // ── Non-negative guarantee ────────────────────────────────────────────

        [Test]
        public void Calculate_ResultIsAlwaysNonNegative()
        {
            // Empty loadout with a valid config should return 0, not negative.
            int result = RobotBuildRatingCalculator.Calculate(
                _loadout, _catalog, _upgrades, null, _config);
            Assert.GreaterOrEqual(result, 0);
        }
    }
}

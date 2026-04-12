using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartRarityConfig"/> and the <see cref="PartRarity"/> enum.
    ///
    /// Covers:
    ///   • Fresh-instance defaults: Tiers not-null, empty.
    ///   • Safe-default fallbacks: GetDisplayName returns rarity.ToString() when no tier matched;
    ///     GetTintColor returns Color.white; GetLootWeightMultiplier returns 1f.
    ///   • Matching-tier paths: all three accessors return configured values.
    ///   • GetLootWeightMultiplier clamps sub-0.1 values to 0.1 at runtime.
    ///   • Multi-tier lookups: correct entry returned without cross-contamination.
    ///   • PartRarity enum: exactly five values, all expected names present.
    /// </summary>
    public class PartRarityConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PartRarityConfig CreateConfig()
            => ScriptableObject.CreateInstance<PartRarityConfig>();

        private static void AddTier(PartRarityConfig config, RarityTierData data)
        {
            var field = typeof(PartRarityConfig)
                .GetField("_tiers", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "_tiers field not found on PartRarityConfig.");
            var list = (List<RarityTierData>)field.GetValue(config);
            list.Add(data);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Tiers_IsNotNull()
        {
            var config = CreateConfig();
            Assert.IsNotNull(config.Tiers);
        }

        [Test]
        public void FreshInstance_Tiers_IsEmpty()
        {
            var config = CreateConfig();
            Assert.AreEqual(0, config.Tiers.Count);
        }

        // ── Safe-default fallbacks ────────────────────────────────────────────

        [Test]
        public void GetDisplayName_NoMatchingTier_ReturnsEnumToString()
        {
            var config = CreateConfig();
            Assert.AreEqual("Rare", config.GetDisplayName(PartRarity.Rare));
        }

        [Test]
        public void GetTintColor_NoMatchingTier_ReturnsWhite()
        {
            var config = CreateConfig();
            Assert.AreEqual(Color.white, config.GetTintColor(PartRarity.Epic));
        }

        [Test]
        public void GetLootWeightMultiplier_NoMatchingTier_ReturnsOne()
        {
            var config = CreateConfig();
            Assert.AreEqual(1f, config.GetLootWeightMultiplier(PartRarity.Legendary), 0.001f);
        }

        // ── Matching-tier paths ───────────────────────────────────────────────

        [Test]
        public void GetDisplayName_MatchingTier_ReturnsConfiguredName()
        {
            var config = CreateConfig();
            AddTier(config, new RarityTierData
            {
                rarity               = PartRarity.Rare,
                displayName          = "Shiny Rare",
                tintColor            = Color.blue,
                lootWeightMultiplier = 0.5f,
            });
            Assert.AreEqual("Shiny Rare", config.GetDisplayName(PartRarity.Rare));
        }

        [Test]
        public void GetTintColor_MatchingTier_ReturnsConfiguredColor()
        {
            var config = CreateConfig();
            AddTier(config, new RarityTierData
            {
                rarity               = PartRarity.Epic,
                displayName          = "Epic",
                tintColor            = Color.magenta,
                lootWeightMultiplier = 0.3f,
            });
            Assert.AreEqual(Color.magenta, config.GetTintColor(PartRarity.Epic));
        }

        [Test]
        public void GetLootWeightMultiplier_MatchingTier_ReturnsConfiguredValue()
        {
            var config = CreateConfig();
            AddTier(config, new RarityTierData
            {
                rarity               = PartRarity.Legendary,
                displayName          = "Legendary",
                tintColor            = Color.yellow,
                lootWeightMultiplier = 0.1f,
            });
            Assert.AreEqual(0.1f, config.GetLootWeightMultiplier(PartRarity.Legendary), 0.001f);
        }

        // ── Runtime clamp ─────────────────────────────────────────────────────

        [Test]
        public void GetLootWeightMultiplier_BelowMinimum_ClampsToPointOne()
        {
            // Struct can be constructed with 0 directly (bypasses [Min(0.1f)] attribute).
            // The accessor must clamp the stored value to ≥ 0.1 at runtime.
            var config = CreateConfig();
            AddTier(config, new RarityTierData
            {
                rarity               = PartRarity.Common,
                displayName          = "Common",
                tintColor            = Color.white,
                lootWeightMultiplier = 0f,
            });
            Assert.GreaterOrEqual(config.GetLootWeightMultiplier(PartRarity.Common), 0.1f);
        }

        // ── Multi-tier correctness ─────────────────────────────────────────────

        [Test]
        public void GetDisplayName_MultipleTiers_ReturnsCorrectEntry()
        {
            var config = CreateConfig();
            AddTier(config, new RarityTierData { rarity = PartRarity.Common,   displayName = "Common",   tintColor = Color.grey,  lootWeightMultiplier = 1.0f });
            AddTier(config, new RarityTierData { rarity = PartRarity.Uncommon, displayName = "Uncommon", tintColor = Color.green, lootWeightMultiplier = 0.7f });
            AddTier(config, new RarityTierData { rarity = PartRarity.Rare,     displayName = "Rare",     tintColor = Color.blue,  lootWeightMultiplier = 0.4f });

            Assert.AreEqual("Uncommon", config.GetDisplayName(PartRarity.Uncommon));
            Assert.AreEqual("Rare",     config.GetDisplayName(PartRarity.Rare));
        }

        [Test]
        public void GetTintColor_MultipleTiers_DoesNotCrossContaminate()
        {
            var config = CreateConfig();
            AddTier(config, new RarityTierData { rarity = PartRarity.Common,    displayName = "Common",    tintColor = Color.white,  lootWeightMultiplier = 1.0f });
            AddTier(config, new RarityTierData { rarity = PartRarity.Legendary, displayName = "Legendary", tintColor = Color.yellow, lootWeightMultiplier = 0.1f });

            Assert.AreEqual(Color.white,  config.GetTintColor(PartRarity.Common));
            Assert.AreEqual(Color.yellow, config.GetTintColor(PartRarity.Legendary));
        }

        // ── PartRarity enum sanity ────────────────────────────────────────────

        [Test]
        public void PartRarity_HasExactlyFiveValues()
        {
            Assert.AreEqual(5, System.Enum.GetValues(typeof(PartRarity)).Length);
        }

        [Test]
        public void PartRarity_AllExpectedValuesExist()
        {
            // If any name changes this test fails before any runtime code does.
            Assert.DoesNotThrow(() => { var _ = PartRarity.Common; });
            Assert.DoesNotThrow(() => { var _ = PartRarity.Uncommon; });
            Assert.DoesNotThrow(() => { var _ = PartRarity.Rare; });
            Assert.DoesNotThrow(() => { var _ = PartRarity.Epic; });
            Assert.DoesNotThrow(() => { var _ = PartRarity.Legendary; });
        }
    }
}

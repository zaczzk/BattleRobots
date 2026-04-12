using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the rarity-aware <c>RollDrop(int, PartRarityConfig)</c>
    /// overload on <see cref="LootTableSO"/>.
    ///
    /// Covers:
    ///   • Null rarityConfig falls back to base RollDrop (same result same seed).
    ///   • Single-entry table always returns that entry regardless of multiplier.
    ///   • Empty table returns null.
    ///   • All-Common parts with neutral multiplier matches base overload.
    ///   • High-multiplier rarity tier is selected more frequently (statistical).
    ///   • Zero multiplier is clamped to 0.1 — part is not excluded.
    ///   • Null-part entries are skipped.
    ///   • Result is deterministic: same seed + same config = same result.
    ///   • Part rarity not present in config uses default multiplier 1.
    /// </summary>
    public class LootTableRarityTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PartDefinition MakePart(PartRarity rarity)
        {
            var part  = ScriptableObject.CreateInstance<PartDefinition>();
            var field = typeof(PartDefinition)
                .GetField("_rarity", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(part, rarity);
            return part;
        }

        private static LootTableSO MakeTable(params (PartDefinition part, float weight)[] entries)
        {
            var table      = ScriptableObject.CreateInstance<LootTableSO>();
            var field      = typeof(LootTableSO)
                .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = new List<LootEntry>();
            foreach (var (part, weight) in entries)
                list.Add(new LootEntry { part = part, weight = weight });
            field.SetValue(table, list);
            return table;
        }

        private static PartRarityConfig MakeConfig(
            params (PartRarity rarity, float multiplier)[] tiers)
        {
            var config = ScriptableObject.CreateInstance<PartRarityConfig>();
            var field  = typeof(PartRarityConfig)
                .GetField("_tiers", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = new List<RarityTierData>();
            foreach (var (rarity, multiplier) in tiers)
                list.Add(new RarityTierData
                {
                    rarity               = rarity,
                    displayName          = rarity.ToString(),
                    tintColor            = Color.white,
                    lootWeightMultiplier = multiplier,
                });
            field.SetValue(config, list);
            return config;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void RollDrop_NullRarityConfig_FallsBackToBaseOverload()
        {
            var part  = MakePart(PartRarity.Common);
            var table = MakeTable((part, 1f));
            // Null config must return the same result as the base overload.
            Assert.AreEqual(table.RollDrop(42), table.RollDrop(42, null));
        }

        [Test]
        public void RollDrop_SingleEntry_AlwaysReturnsThatPart()
        {
            var part   = MakePart(PartRarity.Rare);
            var table  = MakeTable((part, 1f));
            var config = MakeConfig((PartRarity.Rare, 0.5f));
            Assert.AreEqual(part, table.RollDrop(0, config));
        }

        [Test]
        public void RollDrop_EmptyTable_ReturnsNull()
        {
            var table  = ScriptableObject.CreateInstance<LootTableSO>();
            var config = ScriptableObject.CreateInstance<PartRarityConfig>();
            Assert.IsNull(table.RollDrop(1, config));
        }

        [Test]
        public void RollDrop_AllCommonNeutralMultiplier_MatchesBaseOverload()
        {
            var partA  = MakePart(PartRarity.Common);
            var partB  = MakePart(PartRarity.Common);
            var table  = MakeTable((partA, 1f), (partB, 1f));
            var config = MakeConfig((PartRarity.Common, 1f));
            // With a neutral multiplier the distribution is identical to the base overload.
            Assert.AreEqual(table.RollDrop(99), table.RollDrop(99, config));
        }

        [Test]
        public void RollDrop_HighMultiplier_LegendarySelectedMoreFrequently()
        {
            var common    = MakePart(PartRarity.Common);
            var legendary = MakePart(PartRarity.Legendary);
            var table     = MakeTable((common, 1f), (legendary, 1f));
            // Legendary gets ×10 boost; equal base weights → ~90 % legendary drops.
            var config = MakeConfig(
                (PartRarity.Common,    1f),
                (PartRarity.Legendary, 10f));

            int legendaryCount = 0;
            for (int seed = 0; seed < 100; seed++)
                if (table.RollDrop(seed, config) == legendary)
                    legendaryCount++;

            // With a 10:1 ratio we expect ≥70 legendary out of 100 (generous margin).
            Assert.GreaterOrEqual(legendaryCount, 70,
                "Legendary (×10 weight) should be selected in at least 70 % of rolls.");
        }

        [Test]
        public void RollDrop_ZeroMultiplier_ClampsToMinimum_PartNotExcluded()
        {
            // Struct constructed with 0 multiplier (bypasses [Min(0.1f)] inspector attribute).
            // GetLootWeightMultiplier clamps to 0.1 at runtime, so the part must still drop.
            var part   = MakePart(PartRarity.Rare);
            var table  = MakeTable((part, 1f));
            var config = MakeConfig((PartRarity.Rare, 0f));
            Assert.IsNotNull(table.RollDrop(7, config),
                "Part must still be reachable when config multiplier is 0 (clamped to 0.1).");
        }

        [Test]
        public void RollDrop_NullPartEntries_Skipped()
        {
            var good   = MakePart(PartRarity.Common);
            var table  = MakeTable((null, 1f), (good, 1f));
            var config = MakeConfig((PartRarity.Common, 1f));
            Assert.AreEqual(good, table.RollDrop(5, config),
                "Null-part entries must be skipped in rarity-aware roll.");
        }

        [Test]
        public void RollDrop_IsDeterministic_SameSeedSameResult()
        {
            var part   = MakePart(PartRarity.Epic);
            var table  = MakeTable((part, 2f));
            var config = MakeConfig((PartRarity.Epic, 2f));
            var r1     = table.RollDrop(1234, config);
            var r2     = table.RollDrop(1234, config);
            Assert.AreEqual(r1, r2, "Same seed must produce the same result every call.");
        }

        [Test]
        public void RollDrop_RarityNotInConfig_UsesDefaultMultiplierOne()
        {
            // Empty config → GetLootWeightMultiplier returns 1f for any rarity.
            var part   = MakePart(PartRarity.Uncommon);
            var table  = MakeTable((part, 1f));
            var config = ScriptableObject.CreateInstance<PartRarityConfig>(); // no tiers
            Assert.AreEqual(part, table.RollDrop(3, config),
                "Part should be reachable when its rarity has no entry in the config " +
                "(default multiplier 1 keeps base weight unchanged).");
        }
    }
}

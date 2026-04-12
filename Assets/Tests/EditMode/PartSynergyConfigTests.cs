using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartSynergyConfig"/>.
    ///
    /// Covers:
    ///   • Entries list is not null on a fresh instance.
    ///   • GetActiveSynergies: null equippedPartIds → returns empty.
    ///   • GetActiveSynergies: null catalog → returns empty.
    ///   • GetActiveSynergies: empty equippedPartIds → returns empty.
    ///   • GetActiveSynergies: no synergies configured → returns empty.
    ///   • GetActiveSynergies: single requirement met → returns the synergy.
    ///   • GetActiveSynergies: wrong category → returns empty.
    ///   • GetActiveSynergies: rarity below minimum → returns empty.
    ///   • GetActiveSynergies: count below required → returns empty.
    ///   • GetActiveSynergies: multiple requirements all met → returns synergy.
    ///   • GetActiveSynergies: multiple requirements one unmet → returns empty.
    ///   • GetActiveSynergies: multiple synergies, some met some not → returns only met.
    /// </summary>
    public class PartSynergyConfigTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private PartSynergyConfig _config;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PartDefinition MakePart(string id, PartCategory category, PartRarity rarity)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(part, "_partId",   id);
            SetField(part, "_category", category);
            SetField(part, "_rarity",   rarity);
            return part;
        }

        private static ShopCatalog MakeCatalog(params PartDefinition[] parts)
        {
            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(catalog, "_parts", new List<PartDefinition>(parts));
            return catalog;
        }

        private static PartSynergyEntry MakeSynergy(
            string display,
            string bonus,
            params PartSynergyRequirement[] reqs)
            => new PartSynergyEntry
            {
                displayName      = display,
                bonusDescription = bonus,
                requirements     = new List<PartSynergyRequirement>(reqs),
            };

        private static PartSynergyRequirement MakeReq(
            PartCategory cat,
            PartRarity   minRarity,
            int          count)
            => new PartSynergyRequirement
            {
                requiredCategory = cat,
                minimumRarity    = minRarity,
                requiredCount    = count,
            };

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PartSynergyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ── 1. Fresh instance ─────────────────────────────────────────────────

        [Test]
        public void Entries_FreshInstance_IsNotNull()
        {
            Assert.IsNotNull(_config.Entries);
        }

        // ── 2. Null equippedPartIds ───────────────────────────────────────────

        [Test]
        public void GetActiveSynergies_NullEquippedIds_ReturnsEmpty()
        {
            ShopCatalog catalog = MakeCatalog();
            var result = _config.GetActiveSynergies(null, catalog);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Object.DestroyImmediate(catalog);
        }

        // ── 3. Null catalog ───────────────────────────────────────────────────

        [Test]
        public void GetActiveSynergies_NullCatalog_ReturnsEmpty()
        {
            var result = _config.GetActiveSynergies(new List<string> { "part_a" }, null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ── 4. Empty equippedPartIds ──────────────────────────────────────────

        [Test]
        public void GetActiveSynergies_EmptyEquippedIds_ReturnsEmpty()
        {
            ShopCatalog catalog = MakeCatalog();
            var result = _config.GetActiveSynergies(new List<string>(), catalog);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Object.DestroyImmediate(catalog);
        }

        // ── 5. No synergies configured ────────────────────────────────────────

        [Test]
        public void GetActiveSynergies_NoSynergiesConfigured_ReturnsEmpty()
        {
            // _config._entries is empty by default.
            ShopCatalog catalog = MakeCatalog();
            var result = _config.GetActiveSynergies(new List<string> { "part_a" }, catalog);
            Assert.AreEqual(0, result.Count);
            Object.DestroyImmediate(catalog);
        }

        // ── 6. Single requirement met ─────────────────────────────────────────

        [Test]
        public void GetActiveSynergies_SingleRequirementMet_ReturnsSynergy()
        {
            PartDefinition part    = MakePart("weapon_a", PartCategory.Weapon, PartRarity.Common);
            ShopCatalog    catalog = MakeCatalog(part);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Blade Build", "+5% Damage",
                    MakeReq(PartCategory.Weapon, PartRarity.Common, 1))
            });

            var result = _config.GetActiveSynergies(new List<string> { "weapon_a" }, catalog);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Blade Build", result[0].displayName);

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
        }

        // ── 7. Requirement unmet: wrong category ──────────────────────────────

        [Test]
        public void GetActiveSynergies_WrongCategory_ReturnsEmpty()
        {
            PartDefinition part    = MakePart("chassis_a", PartCategory.Chassis, PartRarity.Common);
            ShopCatalog    catalog = MakeCatalog(part);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Blade Build", "+5% Damage",
                    MakeReq(PartCategory.Weapon, PartRarity.Common, 1))
            });

            var result = _config.GetActiveSynergies(new List<string> { "chassis_a" }, catalog);

            Assert.AreEqual(0, result.Count);

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
        }

        // ── 8. Requirement unmet: rarity below minimum ────────────────────────

        [Test]
        public void GetActiveSynergies_RarityTooLow_ReturnsEmpty()
        {
            PartDefinition part    = MakePart("weapon_a", PartCategory.Weapon, PartRarity.Common);
            ShopCatalog    catalog = MakeCatalog(part);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Rare Build", "+10% Damage",
                    MakeReq(PartCategory.Weapon, PartRarity.Rare, 1)) // requires Rare+, part is Common
            });

            var result = _config.GetActiveSynergies(new List<string> { "weapon_a" }, catalog);

            Assert.AreEqual(0, result.Count);

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
        }

        // ── 9. Requirement unmet: count below required ────────────────────────

        [Test]
        public void GetActiveSynergies_CountInsufficient_ReturnsEmpty()
        {
            PartDefinition part    = MakePart("weapon_a", PartCategory.Weapon, PartRarity.Rare);
            ShopCatalog    catalog = MakeCatalog(part);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Dual Blades", "+15% Damage",
                    MakeReq(PartCategory.Weapon, PartRarity.Rare, 2)) // needs 2, player has 1
            });

            var result = _config.GetActiveSynergies(new List<string> { "weapon_a" }, catalog);

            Assert.AreEqual(0, result.Count);

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
        }

        // ── 10. Multiple requirements — all met ───────────────────────────────

        [Test]
        public void GetActiveSynergies_MultipleRequirementsAllMet_ReturnsSynergy()
        {
            PartDefinition weapon  = MakePart("weapon_a",  PartCategory.Weapon,  PartRarity.Epic);
            PartDefinition chassis = MakePart("chassis_a", PartCategory.Chassis, PartRarity.Rare);
            ShopCatalog    catalog = MakeCatalog(weapon, chassis);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Full Armament", "Bonus stats",
                    MakeReq(PartCategory.Weapon,  PartRarity.Rare, 1),   // weapon_a is Epic (≥ Rare) ✓
                    MakeReq(PartCategory.Chassis, PartRarity.Rare, 1))   // chassis_a is Rare          ✓
            });

            var result = _config.GetActiveSynergies(
                new List<string> { "weapon_a", "chassis_a" }, catalog);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Full Armament", result[0].displayName);

            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(chassis);
            Object.DestroyImmediate(catalog);
        }

        // ── 11. Multiple requirements — one unmet ─────────────────────────────

        [Test]
        public void GetActiveSynergies_MultipleRequirementsOneUnmet_ReturnsEmpty()
        {
            PartDefinition weapon  = MakePart("weapon_a",  PartCategory.Weapon,  PartRarity.Epic);
            PartDefinition chassis = MakePart("chassis_a", PartCategory.Chassis, PartRarity.Common); // below Rare
            ShopCatalog    catalog = MakeCatalog(weapon, chassis);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Full Armament", "Bonus stats",
                    MakeReq(PartCategory.Weapon,  PartRarity.Rare, 1),  // weapon_a is Epic ✓
                    MakeReq(PartCategory.Chassis, PartRarity.Rare, 1))  // chassis_a is Common ✗
            });

            var result = _config.GetActiveSynergies(
                new List<string> { "weapon_a", "chassis_a" }, catalog);

            Assert.AreEqual(0, result.Count);

            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(chassis);
            Object.DestroyImmediate(catalog);
        }

        // ── 12. Multiple synergies — returns only the met ones ────────────────

        [Test]
        public void GetActiveSynergies_MultipleSynergies_ReturnsOnlyMet()
        {
            PartDefinition weapon  = MakePart("weapon_a", PartCategory.Weapon, PartRarity.Epic);
            ShopCatalog    catalog = MakeCatalog(weapon);

            SetField(_config, "_entries", new List<PartSynergyEntry>
            {
                MakeSynergy("Blade Build", "+5% Damage",
                    MakeReq(PartCategory.Weapon,  PartRarity.Common, 1)),  // met — weapon_a is Epic
                MakeSynergy("Tank Build",  "+10 Armor",
                    MakeReq(PartCategory.Chassis, PartRarity.Common, 1)),  // unmet — no chassis equipped
            });

            var result = _config.GetActiveSynergies(new List<string> { "weapon_a" }, catalog);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Blade Build", result[0].displayName);

            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }
    }
}

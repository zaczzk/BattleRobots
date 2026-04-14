using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LoadoutValidator"/> Rule 6 — weapon-type unlock
    /// enforcement via <see cref="WeaponTypeUnlockEvaluator"/> (T174).
    ///
    /// LoadoutValidatorWeaponTypeUnlockTests (14):
    ///   Backwards-compatibility:
    ///   • 4-param overload still works and skips rule 6.
    ///   • 7-param overload with null unlockConfig skips rule 6.
    ///   • 7-param overload with null weaponCatalog skips rule 6.
    ///
    ///   Rule 6 — unlocked type:
    ///   • Physical weapon at prestige 0 → no error (always unlocked).
    ///   • Energy weapon at prestige 1 (meets requirement) → no error.
    ///
    ///   Rule 6 — locked type:
    ///   • Energy weapon at prestige 0 → invalid; error added.
    ///   • Locked error message contains the weapon DamageType name.
    ///   • Locked error message contains the lock reason text.
    ///
    ///   Rule 6 — edge cases:
    ///   • Null prestige treated as count 0: Energy weapon locked.
    ///   • Weapon ID not in weapon catalog → no error (unknown type = skip).
    ///   • Non-weapon PartCategory parts are not checked for type unlock.
    ///   • Both a rule-3 ownership error and rule-6 lock error are reported together.
    ///
    ///   PlayerLoadout convenience overload:
    ///   • 7-param PlayerLoadout overload with null loadout → invalid (guard fires).
    ///   • 7-param PlayerLoadout overload valid loadout delegates rule 6 correctly.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class LoadoutValidatorWeaponTypeUnlockTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PartSlot MakeSlot(string slotId, PartCategory category)
            => new PartSlot { slotId = slotId, category = category };

        /// <summary>
        /// Creates a minimal robot def with a single Weapon slot.
        /// </summary>
        private static RobotDefinition CreateRobotDefWeaponOnly()
        {
            var def = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(def, "_slots", new List<PartSlot>
            {
                MakeSlot("weapon_main", PartCategory.Weapon),
            });
            return def;
        }

        /// <summary>
        /// Creates a PartDefinition with the given partId and category.
        /// </summary>
        private static PartDefinition CreatePartDef(string partId, PartCategory category)
        {
            var def = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(def, "_partId",    partId);
            SetField(def, "_category",  category);
            SetField(def, "_displayName", partId);
            return def;
        }

        /// <summary>
        /// Creates a ShopCatalog containing the given PartDefinitions.
        /// </summary>
        private static ShopCatalog CreateCatalog(params PartDefinition[] parts)
        {
            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(catalog, "_parts", new List<PartDefinition>(parts));
            return catalog;
        }

        /// <summary>
        /// Creates a WeaponPartSO linked to the given partId, with the given DamageType.
        /// </summary>
        private static WeaponPartSO CreateWeaponPart(string partId, DamageType type)
        {
            var partDef = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(partDef, "_partId",   partId);
            SetField(partDef, "_category", PartCategory.Weapon);

            var weaponPart = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(weaponPart, "_damageType",      type);
            SetField(weaponPart, "_baseDamage",      10f);
            SetField(weaponPart, "_partDefinition",  partDef);
            return weaponPart;
        }

        /// <summary>
        /// Creates a WeaponPartCatalogSO containing the given weapon parts.
        /// </summary>
        private static WeaponPartCatalogSO CreateWeaponCatalog(params WeaponPartSO[] parts)
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            SetField(catalog, "_parts", new List<WeaponPartSO>(parts));
            return catalog;
        }

        /// <summary>
        /// Creates a WeaponTypeUnlockConfig with the specified prestige requirements.
        /// </summary>
        private static WeaponTypeUnlockConfig CreateUnlockConfig(
            int physical = 0, int energy = 1, int thermal = 4, int shock = 7)
        {
            var cfg = ScriptableObject.CreateInstance<WeaponTypeUnlockConfig>();
            SetField(cfg, "_physicalRequiredPrestige", physical);
            SetField(cfg, "_energyRequiredPrestige",   energy);
            SetField(cfg, "_thermalRequiredPrestige",  thermal);
            SetField(cfg, "_shockRequiredPrestige",    shock);
            return cfg;
        }

        /// <summary>
        /// Creates a PrestigeSystemSO loaded with the given prestige count.
        /// </summary>
        private static PrestigeSystemSO CreatePrestige(int count)
        {
            var p = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            SetField(p, "_maxPrestigeRank", 10);
            p.LoadSnapshot(count);
            return p;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Backwards-compatibility — rule 6 is skipped when params are null
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FourParamOverload_StillWorks_Rule6NotApplied()
        {
            // Energy weapon equipped, prestige 0 — but 4-param overload has no rule 6.
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog);

            Assert.IsTrue(result.IsValid,
                "4-param overload must not apply rule 6 — the loadout is valid.");

            Object.DestroyImmediate(robotDef);
            Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void NullUnlockConfig_SkipsRule6()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var prestige      = CreatePrestige(0); // would fail rule 6 if it ran

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig: null, prestige: prestige, weaponCatalog: weaponCatalog);

            Assert.IsTrue(result.IsValid,
                "Null unlockConfig must skip rule 6 — the loadout is valid.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(prestige);
        }

        [Test]
        public void NullWeaponCatalog_SkipsRule6()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(0); // prestige < energy requirement

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig: unlockConfig, prestige: prestige, weaponCatalog: null);

            Assert.IsTrue(result.IsValid,
                "Null weaponCatalog must skip rule 6 — the loadout is valid.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Rule 6 — unlocked type → no error
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Rule6_PhysicalWeapon_AtPrestige0_NoError()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Physical);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(physical: 0);
            var prestige      = CreatePrestige(0);

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsTrue(result.IsValid,
                "Physical weapon with 0 prestige requirement at prestige 0 must be valid.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Rule6_EnergyWeapon_PrestigeMetRequirement_NoError()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(1); // exactly meets requirement

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsTrue(result.IsValid,
                "Energy weapon with prestige count == requirement must be valid.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Rule 6 — locked type → error reported
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Rule6_EnergyWeapon_PrestigeBelowRequirement_Invalid()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(0); // below requirement

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsFalse(result.IsValid,
                "Energy weapon with prestige 0 < requirement 1 must be invalid.");
            Assert.AreEqual(1, result.Errors.Count,
                "Exactly one error should be reported for one locked weapon.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Rule6_LockedError_ContainsDamageTypeName()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(0);

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("Energy", result.Errors[0],
                "Lock error message should contain the DamageType name 'Energy'.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Rule6_LockedError_ContainsLockReasonText()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(0);

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.AreEqual(1, result.Errors.Count);
            // Lock reason from WeaponTypeUnlockConfig.GetLockReason:
            // "Requires Prestige 1 (Bronze I)"
            StringAssert.Contains("Requires Prestige", result.Errors[0],
                "Lock error message should contain the lock reason 'Requires Prestige'.");
            StringAssert.Contains("1", result.Errors[0],
                "Lock error message should contain the required prestige count '1'.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Rule 6 — edge cases
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Rule6_NullPrestige_TreatedAsCountZero_EnergyLocked()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            // prestige = null → treated as count 0 → Energy (requires 1) is locked

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige: null, weaponCatalog: weaponCatalog);

            Assert.IsFalse(result.IsValid,
                "Null prestige (count 0) with Energy requirement 1 must be invalid.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
        }

        [Test]
        public void Rule6_WeaponIdNotInWeaponCatalog_NoError()
        {
            // The weapon part exists in the shop catalog (category = Weapon) but
            // is absent from the weapon-type catalog → DamageType unknown → skip.
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponCatalog = CreateWeaponCatalog(); // empty weapon catalog
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(0);

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsTrue(result.IsValid,
                "Weapon part absent from weapon catalog must not produce a rule-6 error.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(weaponCatalog);
            Object.DestroyImmediate(unlockConfig); Object.DestroyImmediate(prestige);
        }

        [Test]
        public void Rule6_NonWeaponCategoryPart_Ignored()
        {
            // A Chassis part should never be checked against the unlock config.
            var robotDef = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(robotDef, "_slots", new List<PartSlot>
            {
                MakeSlot("chassis_main", PartCategory.Chassis),
            });

            var chassisPartDef = CreatePartDef("chassis_01", PartCategory.Chassis);
            var catalog        = CreateCatalog(chassisPartDef);
            var unlockConfig   = CreateUnlockConfig(); // lock everything
            var prestige       = CreatePrestige(0);

            // No weapon parts in weapon catalog — but none are equipped either.
            var weaponCatalog = CreateWeaponCatalog();

            var result = LoadoutValidator.Validate(
                new List<string> { "chassis_01" }, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsTrue(result.IsValid,
                "Non-Weapon-category parts must not be checked for weapon type unlock.");

            Object.DestroyImmediate(robotDef); Object.DestroyImmediate(chassisPartDef);
            Object.DestroyImmediate(catalog);  Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige); Object.DestroyImmediate(weaponCatalog);
        }

        [Test]
        public void Rule6_AndRule3_BothErrorsReported()
        {
            // Setup: one weapon part that is locked (rule 6) and not owned (rule 3).
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Energy);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(energy: 1);
            var prestige      = CreatePrestige(0); // Energy locked

            // PlayerInventory that does NOT own weapon_01 (rule 3 fires).
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();
            // inventory is freshly created with no parts → HasPart("weapon_01") = false

            var result = LoadoutValidator.Validate(
                new List<string> { "weapon_01" }, robotDef, inventory, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsFalse(result.IsValid,
                "Both a rule-3 and rule-6 violation must make the result invalid.");
            Assert.GreaterOrEqual(result.Errors.Count, 2,
                "Both the ownership error (rule 3) and the lock error (rule 6) must be reported.");

            Object.DestroyImmediate(robotDef);    Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);     Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);    Object.DestroyImmediate(inventory);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PlayerLoadout convenience overload — 7-param variant
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void PlayerLoadoutOverload_NullLoadout_ReturnsInvalid()
        {
            var unlockConfig  = CreateUnlockConfig();
            var prestige      = CreatePrestige(0);
            var weaponCatalog = CreateWeaponCatalog();
            var robotDef      = CreateRobotDefWeaponOnly();

            var result = LoadoutValidator.Validate(
                (PlayerLoadout)null, robotDef, null, null,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsFalse(result.IsValid,
                "7-param overload must return invalid immediately when loadout is null.");

            Object.DestroyImmediate(unlockConfig); Object.DestroyImmediate(prestige);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(robotDef);
        }

        [Test]
        public void PlayerLoadoutOverload_ValidLoadout_DelegatesRule6Correctly()
        {
            var robotDef      = CreateRobotDefWeaponOnly();
            var weaponPartDef = CreatePartDef("weapon_01", PartCategory.Weapon);
            var catalog       = CreateCatalog(weaponPartDef);
            var weaponPart    = CreateWeaponPart("weapon_01", DamageType.Physical);
            var weaponCatalog = CreateWeaponCatalog(weaponPart);
            var unlockConfig  = CreateUnlockConfig(physical: 0);
            var prestige      = CreatePrestige(0);

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            SetField(loadout, "_equippedPartIds", new List<string> { "weapon_01" });

            var result = LoadoutValidator.Validate(
                loadout, robotDef, null, catalog,
                unlockConfig, prestige, weaponCatalog);

            Assert.IsTrue(result.IsValid,
                "PlayerLoadout overload: Physical weapon at prestige 0 must be valid via rule 6.");

            Object.DestroyImmediate(robotDef);    Object.DestroyImmediate(weaponPartDef);
            Object.DestroyImmediate(catalog);     Object.DestroyImmediate(weaponPart);
            Object.DestroyImmediate(weaponCatalog); Object.DestroyImmediate(unlockConfig);
            Object.DestroyImmediate(prestige);    Object.DestroyImmediate(loadout);
        }
    }
}

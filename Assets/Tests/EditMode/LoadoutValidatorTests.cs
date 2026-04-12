using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LoadoutValidator"/> and
    /// <see cref="LoadoutValidationResult"/>.
    ///
    /// Covers:
    ///   • LoadoutValidationResult.Valid: IsValid = true, Errors empty and non-null.
    ///   • LoadoutValidationResult.Invalid: IsValid = false, Errors carries messages.
    ///   • Validate — null equippedIds → invalid.
    ///   • Validate — null robotDef → invalid.
    ///   • Validate — null catalog + null inventory + empty ids → valid (no rules apply).
    ///   • Validate — null catalog, owns part, no category check → valid.
    ///   • Validate — catalog provided, part found and owned, category covered → valid.
    ///   • Validate — part not found in catalog → invalid with catalog-error message.
    ///   • Validate — part in catalog but not owned → invalid with ownership-error message.
    ///   • Validate — required category not covered → invalid with category-error message.
    ///   • Validate — null catalog skips catalog + category checks.
    ///   • Validate — null inventory skips ownership check.
    ///   • Validate — multiple violations accumulate all errors.
    ///   • Validate(PlayerLoadout, …) overload: null loadout → invalid.
    ///   • Validate(PlayerLoadout, …) overload: valid loadout delegates correctly.
    /// </summary>
    public class LoadoutValidatorTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private RobotDefinition  _robotDef;
        private PlayerInventory  _inventory;
        private ShopCatalog      _catalog;
        private PartDefinition   _weaponPart;
        private PartDefinition   _chassisPart;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PartSlot MakeSlot(string slotId, PartCategory category)
            => new PartSlot { slotId = slotId, category = category };

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Robot definition with two required categories: Weapon + Chassis.
            _robotDef = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(_robotDef, "_slots", new List<PartSlot>
            {
                MakeSlot("weapon_main",  PartCategory.Weapon),
                MakeSlot("chassis_main", PartCategory.Chassis),
            });

            // Two part definitions.
            _weaponPart  = ScriptableObject.CreateInstance<PartDefinition>();
            _chassisPart = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(_weaponPart,  "_partId",   "weapon_01");
            SetField(_weaponPart,  "_category", PartCategory.Weapon);
            SetField(_chassisPart, "_partId",   "chassis_01");
            SetField(_chassisPart, "_category", PartCategory.Chassis);

            // Catalog containing both parts.
            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(_catalog, "_parts", new List<PartDefinition>
            {
                _weaponPart,
                _chassisPart,
            });

            // Empty inventory (tests unlock parts explicitly as needed).
            _inventory = ScriptableObject.CreateInstance<PlayerInventory>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_robotDef);
            Object.DestroyImmediate(_weaponPart);
            Object.DestroyImmediate(_chassisPart);
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_inventory);
        }

        // ── LoadoutValidationResult ───────────────────────────────────────────

        [Test]
        public void ValidResult_IsValid_IsTrue_AndErrors_IsEmpty()
        {
            LoadoutValidationResult r = LoadoutValidationResult.Valid;
            Assert.IsTrue(r.IsValid);
            Assert.IsNotNull(r.Errors);
            Assert.AreEqual(0, r.Errors.Count);
        }

        [Test]
        public void InvalidResult_IsValid_IsFalse_AndErrors_CarriesMessages()
        {
            var msgs = new List<string> { "Error A", "Error B" };
            LoadoutValidationResult r = LoadoutValidationResult.Invalid(msgs);
            Assert.IsFalse(r.IsValid);
            Assert.AreEqual(2, r.Errors.Count);
            Assert.AreEqual("Error A", r.Errors[0]);
            Assert.AreEqual("Error B", r.Errors[1]);
        }

        // ── Null-guard tests ──────────────────────────────────────────────────

        [Test]
        public void Validate_NullEquippedIds_IsInvalid()
        {
            LoadoutValidationResult r = LoadoutValidator.Validate(
                null, _robotDef, _inventory, _catalog);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors[0].Contains("null"),
                "Error message should mention 'null' equipped part list.");
        }

        [Test]
        public void Validate_NullRobotDef_IsInvalid()
        {
            LoadoutValidationResult r = LoadoutValidator.Validate(
                new List<string>(), null, _inventory, _catalog);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors[0].Length > 0);
        }

        // ── Happy-path tests ──────────────────────────────────────────────────

        [Test]
        public void Validate_EmptyIds_NullCatalog_NullInventory_IsValid()
        {
            // No catalog → no category check; no inventory → no ownership check.
            // Empty list is technically valid when no rules can fire.
            LoadoutValidationResult r = LoadoutValidator.Validate(
                new List<string>(), _robotDef, null, null);
            Assert.IsTrue(r.IsValid, "With null catalog and null inventory, no rules apply.");
        }

        [Test]
        public void Validate_AllPartsOwnedAndCategoriesCovered_IsValid()
        {
            _inventory.UnlockPart("weapon_01");
            _inventory.UnlockPart("chassis_01");

            var ids = new List<string> { "weapon_01", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDef, _inventory, _catalog);
            Assert.IsTrue(r.IsValid, "All parts owned and all required categories covered.");
        }

        // ── Catalog membership ────────────────────────────────────────────────

        [Test]
        public void Validate_PartNotInCatalog_IsInvalid()
        {
            _inventory.UnlockPart("unknown_part");

            var ids = new List<string> { "unknown_part", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDef, _inventory, _catalog);

            Assert.IsFalse(r.IsValid);
            bool hasError = false;
            for (int i = 0; i < r.Errors.Count; i++)
                if (r.Errors[i].Contains("unknown_part")) { hasError = true; break; }
            Assert.IsTrue(hasError, "An error mentioning the unknown part ID should be present.");
        }

        // ── Ownership ─────────────────────────────────────────────────────────

        [Test]
        public void Validate_PartNotOwned_IsInvalid()
        {
            // weapon_01 is in the catalog but NOT in the inventory.
            _inventory.UnlockPart("chassis_01");

            var ids = new List<string> { "weapon_01", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDef, _inventory, _catalog);

            Assert.IsFalse(r.IsValid);
            bool hasOwnershipError = false;
            for (int i = 0; i < r.Errors.Count; i++)
                if (r.Errors[i].Contains("weapon_01")) { hasOwnershipError = true; break; }
            Assert.IsTrue(hasOwnershipError,
                "An error mentioning the unowned part should appear.");
        }

        // ── Slot coverage ─────────────────────────────────────────────────────

        [Test]
        public void Validate_MissingRequiredCategory_IsInvalid()
        {
            // Only Chassis equipped — Weapon slot not filled.
            _inventory.UnlockPart("chassis_01");

            var ids = new List<string> { "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDef, _inventory, _catalog);

            Assert.IsFalse(r.IsValid);
            bool hasCategoryError = false;
            for (int i = 0; i < r.Errors.Count; i++)
                if (r.Errors[i].Contains("Weapon")) { hasCategoryError = true; break; }
            Assert.IsTrue(hasCategoryError,
                "Missing Weapon category should generate an error mentioning 'Weapon'.");
        }

        // ── Partial validation (null catalog / null inventory) ─────────────────

        [Test]
        public void Validate_NullCatalog_SkipsCatalogAndCategoryChecks()
        {
            // Part not in catalog (because catalog is null) + part not owned.
            // With null catalog we only run the ownership check (if inventory non-null).
            _inventory.UnlockPart("weapon_01");
            _inventory.UnlockPart("chassis_01");

            var ids = new List<string> { "weapon_01", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDef, _inventory, null);  // no catalog

            // Ownership passes; category check is skipped → valid.
            Assert.IsTrue(r.IsValid,
                "Null catalog should skip both catalog-membership and category-coverage checks.");
        }

        [Test]
        public void Validate_NullInventory_SkipsOwnershipCheck()
        {
            // Parts are in the catalog but NOT in any inventory.
            var ids = new List<string> { "weapon_01", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDef, null, _catalog);  // no inventory

            // No ownership errors; category coverage is fine → valid.
            Assert.IsTrue(r.IsValid,
                "Null inventory should skip the ownership check.");
        }

        // ── Multiple errors ───────────────────────────────────────────────────

        [Test]
        public void Validate_MultipleViolations_ReportsAllErrors()
        {
            // weapon_01 not owned, chassis_01 not in catalog → two errors minimum.
            _inventory.UnlockPart("weapon_01");
            // chassis_01 is NOT owned.
            // Create a stripped catalog with only the weapon part:
            var strippedCatalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(strippedCatalog, "_parts", new List<PartDefinition> { _weaponPart });

            try
            {
                var ids = new List<string> { "weapon_01", "chassis_01" };
                LoadoutValidationResult r = LoadoutValidator.Validate(
                    ids, _robotDef, _inventory, strippedCatalog);

                Assert.IsFalse(r.IsValid);
                Assert.IsTrue(r.Errors.Count >= 2,
                    "Expected at least 2 errors (catalog miss + missing category), " +
                    $"got {r.Errors.Count}.");
            }
            finally
            {
                Object.DestroyImmediate(strippedCatalog);
            }
        }

        // ── PlayerLoadout overload ────────────────────────────────────────────

        [Test]
        public void Validate_NullPlayerLoadout_IsInvalid()
        {
            LoadoutValidationResult r = LoadoutValidator.Validate(
                (PlayerLoadout)null, _robotDef, _inventory, _catalog);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Count > 0);
        }

        [Test]
        public void Validate_PlayerLoadoutOverload_DelegatesCorrectly()
        {
            PlayerLoadout loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            try
            {
                _inventory.UnlockPart("weapon_01");
                _inventory.UnlockPart("chassis_01");

                loadout.SetLoadout(new List<string> { "weapon_01", "chassis_01" });

                LoadoutValidationResult r = LoadoutValidator.Validate(
                    loadout, _robotDef, _inventory, _catalog);

                Assert.IsTrue(r.IsValid,
                    "PlayerLoadout overload should delegate to base Validate correctly.");
            }
            finally
            {
                Object.DestroyImmediate(loadout);
            }
        }
    }
}

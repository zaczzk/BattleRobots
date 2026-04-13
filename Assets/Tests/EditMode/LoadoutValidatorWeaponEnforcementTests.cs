using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the exactly-one-Weapon enforcement rule (Rule 5) added to
    /// <see cref="LoadoutValidator"/>.
    ///
    /// Covers:
    ///   Null-catalog bypass:
    ///   • Null catalog skips weapon-count check even when robot has a Weapon slot.
    ///
    ///   No-weapon-slot bypass:
    ///   • When the robot definition has no Weapon slot, equipping 0 or 2 weapon parts
    ///     does not generate a weapon-count error.
    ///
    ///   Zero weapons (slot required):
    ///   • 0 weapon parts + Weapon slot required → invalid; error contains "Weapon" and "none".
    ///
    ///   Exactly-one weapons (slot required):
    ///   • 1 weapon part + Weapon slot required → valid (Rule 5 passes).
    ///
    ///   Two weapons (slot required):
    ///   • 2 weapon parts + Weapon slot required → invalid; error contains "Weapon" and "2".
    ///
    ///   Three weapons (slot required):
    ///   • 3 weapon parts + Weapon slot required → invalid; error contains "3".
    ///
    ///   Mixed loadout:
    ///   • 1 weapon + 1 chassis, both required → valid when all other checks pass.
    ///
    ///   Interaction with slot-coverage:
    ///   • Chassis not covered still produces slot-coverage error independent of weapon check.
    ///   • Weapon slot covered by slot-coverage now uses the exact-count path, not the generic path.
    ///
    ///   PlayerLoadout overload:
    ///   • Two weapons via PlayerLoadout overload → invalid with weapon-count error.
    /// </summary>
    public class LoadoutValidatorWeaponEnforcementTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private RobotDefinition _robotDefWeaponOnly;   // requires Weapon
        private RobotDefinition _robotDefNoWeapon;     // requires Chassis only
        private RobotDefinition _robotDefWeaponChassis;// requires Weapon + Chassis

        private PartDefinition _weapon1;
        private PartDefinition _weapon2;
        private PartDefinition _weapon3;
        private PartDefinition _chassisPart;
        private ShopCatalog    _catalog;

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

        private PartDefinition MakePart(string partId, PartCategory category)
        {
            var def = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(def, "_partId",   partId);
            SetField(def, "_category", category);
            return def;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Robot that requires only a Weapon slot.
            _robotDefWeaponOnly = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(_robotDefWeaponOnly, "_slots", new List<PartSlot>
            {
                MakeSlot("weapon_main", PartCategory.Weapon),
            });

            // Robot that requires only a Chassis slot (no Weapon).
            _robotDefNoWeapon = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(_robotDefNoWeapon, "_slots", new List<PartSlot>
            {
                MakeSlot("chassis_main", PartCategory.Chassis),
            });

            // Robot that requires both Weapon and Chassis.
            _robotDefWeaponChassis = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(_robotDefWeaponChassis, "_slots", new List<PartSlot>
            {
                MakeSlot("weapon_main",  PartCategory.Weapon),
                MakeSlot("chassis_main", PartCategory.Chassis),
            });

            _weapon1     = MakePart("weapon_01", PartCategory.Weapon);
            _weapon2     = MakePart("weapon_02", PartCategory.Weapon);
            _weapon3     = MakePart("weapon_03", PartCategory.Weapon);
            _chassisPart = MakePart("chassis_01", PartCategory.Chassis);

            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(_catalog, "_parts", new List<PartDefinition>
            {
                _weapon1, _weapon2, _weapon3, _chassisPart
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_robotDefWeaponOnly);
            Object.DestroyImmediate(_robotDefNoWeapon);
            Object.DestroyImmediate(_robotDefWeaponChassis);
            Object.DestroyImmediate(_weapon1);
            Object.DestroyImmediate(_weapon2);
            Object.DestroyImmediate(_weapon3);
            Object.DestroyImmediate(_chassisPart);
            Object.DestroyImmediate(_catalog);
        }

        // ── Null-catalog bypass ───────────────────────────────────────────────

        [Test]
        public void NullCatalog_SkipsWeaponCountCheck()
        {
            // Even with a Weapon slot required, null catalog bypasses Rule 5.
            var ids = new List<string> { "weapon_01", "weapon_02" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, null);

            // Without a catalog we can't resolve categories, so no weapon check runs.
            Assert.IsTrue(r.IsValid,
                "Null catalog must bypass the weapon-count enforcement.");
        }

        // ── No-weapon-slot bypass ─────────────────────────────────────────────

        [Test]
        public void NoWeaponSlotRequired_TwoWeaponParts_NoWeaponCountError()
        {
            // Robot only requires Chassis; no Weapon slot → Rule 5 does not apply.
            var ids = new List<string> { "weapon_01", "weapon_02", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefNoWeapon, null, _catalog);

            // Chassis category covered; no Weapon slot in robot → valid.
            bool hasWeaponError = false;
            for (int i = 0; i < r.Errors.Count; i++)
                if (r.Errors[i].Contains("Weapon part required")) { hasWeaponError = true; break; }
            Assert.IsFalse(hasWeaponError,
                "No weapon-count error when RobotDefinition has no Weapon slot.");
        }

        // ── Zero weapons (Weapon slot required) ──────────────────────────────

        [Test]
        public void ZeroWeapons_WeaponSlotRequired_IsInvalid()
        {
            var ids = new List<string>(); // nothing equipped
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, _catalog);

            Assert.IsFalse(r.IsValid, "0 weapons when 1 required must be invalid.");
        }

        [Test]
        public void ZeroWeapons_ErrorMessage_ContainsWeaponAndNone()
        {
            var ids = new List<string>();
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, _catalog);

            bool found = false;
            for (int i = 0; i < r.Errors.Count; i++)
            {
                string e = r.Errors[i];
                if (e.Contains("Weapon") && e.Contains("none")) { found = true; break; }
            }
            Assert.IsTrue(found,
                "Error message for 0 weapons must contain 'Weapon' and 'none'.");
        }

        // ── Exactly one weapon (Weapon slot required) ─────────────────────────

        [Test]
        public void ExactlyOneWeapon_WeaponSlotRequired_IsValid()
        {
            var ids = new List<string> { "weapon_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, _catalog);

            Assert.IsTrue(r.IsValid,
                "Exactly 1 weapon with a Weapon slot required must be valid.");
        }

        // ── Two weapons (Weapon slot required) ────────────────────────────────

        [Test]
        public void TwoWeapons_WeaponSlotRequired_IsInvalid()
        {
            var ids = new List<string> { "weapon_01", "weapon_02" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, _catalog);

            Assert.IsFalse(r.IsValid, "2 weapons when exactly 1 required must be invalid.");
        }

        [Test]
        public void TwoWeapons_ErrorMessage_ContainsCountTwo()
        {
            var ids = new List<string> { "weapon_01", "weapon_02" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, _catalog);

            bool found = false;
            for (int i = 0; i < r.Errors.Count; i++)
            {
                string e = r.Errors[i];
                if (e.Contains("Weapon") && e.Contains("2")) { found = true; break; }
            }
            Assert.IsTrue(found,
                "Error message for 2 weapons must contain 'Weapon' and '2'.");
        }

        // ── Three weapons ─────────────────────────────────────────────────────

        [Test]
        public void ThreeWeapons_ErrorMessage_ContainsCountThree()
        {
            var ids = new List<string> { "weapon_01", "weapon_02", "weapon_03" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponOnly, null, _catalog);

            Assert.IsFalse(r.IsValid);
            bool found = false;
            for (int i = 0; i < r.Errors.Count; i++)
            {
                string e = r.Errors[i];
                if (e.Contains("3")) { found = true; break; }
            }
            Assert.IsTrue(found, "Error message for 3 weapons must mention '3'.");
        }

        // ── Mixed loadout ─────────────────────────────────────────────────────

        [Test]
        public void OneWeaponOneChasis_BothSlotsRequired_IsValid()
        {
            var ids = new List<string> { "weapon_01", "chassis_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponChassis, null, _catalog);

            Assert.IsTrue(r.IsValid,
                "1 weapon + 1 chassis covers both required slots — should be valid.");
        }

        // ── Interaction with slot coverage ────────────────────────────────────

        [Test]
        public void OneWeapon_ChassisSlotMissing_SlotCoverageErrorPresent()
        {
            // 1 weapon equipped but chassis slot is also required and not covered.
            var ids = new List<string> { "weapon_01" };
            LoadoutValidationResult r = LoadoutValidator.Validate(
                ids, _robotDefWeaponChassis, null, _catalog);

            Assert.IsFalse(r.IsValid);
            bool chassisError = false;
            for (int i = 0; i < r.Errors.Count; i++)
                if (r.Errors[i].Contains("Chassis")) { chassisError = true; break; }
            Assert.IsTrue(chassisError,
                "Missing Chassis slot must still produce a slot-coverage error.");
        }

        // ── PlayerLoadout overload ────────────────────────────────────────────

        [Test]
        public void PlayerLoadoutOverload_TwoWeapons_IsInvalid()
        {
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            try
            {
                loadout.SetLoadout(new[] { "weapon_01", "weapon_02" });
                LoadoutValidationResult r = LoadoutValidator.Validate(
                    loadout, _robotDefWeaponOnly, null, _catalog);

                Assert.IsFalse(r.IsValid,
                    "PlayerLoadout overload must forward to Validate and apply Rule 5.");
            }
            finally
            {
                Object.DestroyImmediate(loadout);
            }
        }
    }
}

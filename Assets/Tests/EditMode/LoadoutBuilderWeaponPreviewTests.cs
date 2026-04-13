using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the weapon-type preview extension added to
    /// <see cref="LoadoutBuilderController"/> in M23 (T166).
    ///
    /// All tests exercise the private static helper
    /// <c>ResolveWeaponTypeLabel(IReadOnlyList&lt;string&gt;, WeaponPartCatalogSO)</c>
    /// via reflection, following the established private-method testing pattern
    /// used in <see cref="LoadoutBuilderControllerTests"/>.
    ///
    /// Covers:
    ///   Null / empty guards:
    ///   • Null equippedIds → "Type: —"
    ///   • Null catalog → "Type: —"
    ///   • Empty equippedIds → "Type: —"
    ///   • No matching part in catalog → "Type: —"
    ///
    ///   Weapon type labels:
    ///   • Physical weapon → "Type: Physical"
    ///   • Energy weapon → "Type: Energy"
    ///   • Thermal weapon → "Type: Thermal"
    ///   • Shock weapon → "Type: Shock"
    /// </summary>
    public class LoadoutBuilderWeaponPreviewTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        /// <summary>
        /// Invokes <c>LoadoutBuilderController.ResolveWeaponTypeLabel</c> (private static)
        /// via reflection and returns the result string.
        /// </summary>
        private static string InvokeResolveLabel(
            IReadOnlyList<string> ids, WeaponPartCatalogSO catalog)
        {
            MethodInfo mi = typeof(LoadoutBuilderController)
                .GetMethod("ResolveWeaponTypeLabel",
                    BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(mi,
                "ResolveWeaponTypeLabel must exist as a private static method on LoadoutBuilderController.");

            return (string)mi.Invoke(null, new object[] { ids, catalog });
        }

        // ── Setup helpers ─────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic |
                                BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>Creates a WeaponPartSO with the given DamageType linked to a PartDefinition.</summary>
        private static WeaponPartSO CreateWeapon(string partId, DamageType damageType)
        {
            var def = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(def, "_partId", partId);

            var weapon = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(weapon, "_damageType",      damageType);
            SetField(weapon, "_partDefinition",  def);
            return weapon;
        }

        /// <summary>Creates a WeaponPartCatalogSO containing the provided weapon.</summary>
        private static WeaponPartCatalogSO CreateCatalog(WeaponPartSO weapon)
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            SetField(catalog, "_parts", new List<WeaponPartSO> { weapon });
            return catalog;
        }

        // ── Null / empty guards ───────────────────────────────────────────────

        [Test]
        public void ResolveWeaponTypeLabel_NullIds_ReturnsDash()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();

            string result = InvokeResolveLabel(null, catalog);

            StringAssert.Contains("\u2014", result,
                "Null equippedIds must return the em-dash fallback string.");

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ResolveWeaponTypeLabel_NullCatalog_ReturnsDash()
        {
            var ids = new List<string> { "weapon_001" };

            string result = InvokeResolveLabel(ids, null);

            StringAssert.Contains("\u2014", result,
                "Null catalog must return the em-dash fallback string.");
        }

        [Test]
        public void ResolveWeaponTypeLabel_EmptyIds_ReturnsDash()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            var ids     = new List<string>();

            string result = InvokeResolveLabel(ids, catalog);

            StringAssert.Contains("\u2014", result,
                "Empty equippedIds must return the em-dash fallback string.");

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ResolveWeaponTypeLabel_NoMatchInCatalog_ReturnsDash()
        {
            WeaponPartSO weapon  = CreateWeapon("weapon_001", DamageType.Physical);
            WeaponPartCatalogSO catalog = CreateCatalog(weapon);
            var ids = new List<string> { "unknown_999" };  // no match

            string result = InvokeResolveLabel(ids, catalog);

            StringAssert.Contains("\u2014", result,
                "No catalog match must return the em-dash fallback string.");

            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }

        // ── Weapon type labels ────────────────────────────────────────────────

        [Test]
        public void ResolveWeaponTypeLabel_PhysicalWeapon_ReturnsPhysical()
        {
            WeaponPartSO weapon  = CreateWeapon("weapon_001", DamageType.Physical);
            WeaponPartCatalogSO catalog = CreateCatalog(weapon);
            var ids = new List<string> { "weapon_001" };

            string result = InvokeResolveLabel(ids, catalog);

            StringAssert.Contains("Physical", result,
                "Physical weapon must produce a label containing 'Physical'.");

            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ResolveWeaponTypeLabel_EnergyWeapon_ReturnsEnergy()
        {
            WeaponPartSO weapon  = CreateWeapon("weapon_002", DamageType.Energy);
            WeaponPartCatalogSO catalog = CreateCatalog(weapon);
            var ids = new List<string> { "weapon_002" };

            string result = InvokeResolveLabel(ids, catalog);

            StringAssert.Contains("Energy", result,
                "Energy weapon must produce a label containing 'Energy'.");

            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ResolveWeaponTypeLabel_ThermalWeapon_ReturnsThermal()
        {
            WeaponPartSO weapon  = CreateWeapon("weapon_003", DamageType.Thermal);
            WeaponPartCatalogSO catalog = CreateCatalog(weapon);
            var ids = new List<string> { "weapon_003" };

            string result = InvokeResolveLabel(ids, catalog);

            StringAssert.Contains("Thermal", result,
                "Thermal weapon must produce a label containing 'Thermal'.");

            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ResolveWeaponTypeLabel_ShockWeapon_ReturnsShock()
        {
            WeaponPartSO weapon  = CreateWeapon("weapon_004", DamageType.Shock);
            WeaponPartCatalogSO catalog = CreateCatalog(weapon);
            var ids = new List<string> { "weapon_004" };

            string result = InvokeResolveLabel(ids, catalog);

            StringAssert.Contains("Shock", result,
                "Shock weapon must produce a label containing 'Shock'.");

            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }
    }
}

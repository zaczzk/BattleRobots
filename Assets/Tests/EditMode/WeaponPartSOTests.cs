using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponPartSO"/>.
    ///
    /// Covers:
    ///   Default values:
    ///   • WeaponDamageType defaults to Physical.
    ///   • BaseDamage defaults to 10.
    ///
    ///   DamageType round-trips:
    ///   • Energy, Thermal, Shock can be set and read back.
    ///
    ///   BaseDamage:
    ///   • Custom value round-trips.
    ///
    ///   DisplayName fallback chain:
    ///   • Explicit _displayName is returned when set.
    ///   • Falls back to SO asset name when _displayName is empty and _partDefinition is null.
    ///   • Falls back to PartDefinition.DisplayName when _displayName is empty and _partDefinition assigned.
    ///
    ///   Description:
    ///   • Value round-trip.
    ///
    ///   PartDefinition:
    ///   • Returns null when not assigned.
    ///   • Returns the assigned reference when set.
    /// </summary>
    public class WeaponPartSOTests
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

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void DefaultInstance_DamageType_IsPhysical()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            Assert.AreEqual(DamageType.Physical, so.WeaponDamageType);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void DefaultInstance_BaseDamage_IsTen()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            Assert.AreEqual(10f, so.BaseDamage, 0.001f);
            Object.DestroyImmediate(so);
        }

        // ── DamageType round-trips ────────────────────────────────────────────

        [Test]
        public void DamageType_RoundTrip_Energy()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", DamageType.Energy);
            Assert.AreEqual(DamageType.Energy, so.WeaponDamageType);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void DamageType_RoundTrip_Thermal()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", DamageType.Thermal);
            Assert.AreEqual(DamageType.Thermal, so.WeaponDamageType);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void DamageType_RoundTrip_Shock()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", DamageType.Shock);
            Assert.AreEqual(DamageType.Shock, so.WeaponDamageType);
            Object.DestroyImmediate(so);
        }

        // ── BaseDamage ────────────────────────────────────────────────────────

        [Test]
        public void BaseDamage_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_baseDamage", 25f);
            Assert.AreEqual(25f, so.BaseDamage, 0.001f);
            Object.DestroyImmediate(so);
        }

        // ── DisplayName fallback chain ────────────────────────────────────────

        [Test]
        public void DisplayName_WhenExplicitlySet_ReturnsExplicitName()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_displayName", "Plasma Cannon");
            Assert.AreEqual("Plasma Cannon", so.DisplayName);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void DisplayName_WhenEmptyAndNoPartDef_ReturnsAssetName()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_displayName", "");
            SetField(so, "_partDefinition", null);
            // CreateInstance sets name to the type name by default
            Assert.AreEqual(so.name, so.DisplayName);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void DisplayName_WhenEmptyAndPartDefAssigned_FallsBackToPartDefName()
        {
            var so  = ScriptableObject.CreateInstance<WeaponPartSO>();
            var def = ScriptableObject.CreateInstance<PartDefinition>();

            FieldInfo dnField = typeof(PartDefinition)
                .GetField("_displayName", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(dnField, "_displayName field not found on PartDefinition.");
            dnField.SetValue(def, "Laser Arm");

            SetField(so, "_displayName",    "");
            SetField(so, "_partDefinition", def);

            Assert.AreEqual("Laser Arm", so.DisplayName);

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(def);
        }

        // ── Description ───────────────────────────────────────────────────────

        [Test]
        public void Description_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_description", "A high-energy plasma blaster.");
            Assert.AreEqual("A high-energy plasma blaster.", so.Description);
            Object.DestroyImmediate(so);
        }

        // ── PartDefinition ────────────────────────────────────────────────────

        [Test]
        public void PartDefinition_WhenNull_ReturnsNull()
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_partDefinition", null);
            Assert.IsNull(so.PartDefinition);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void PartDefinition_WhenAssigned_ReturnsReference()
        {
            var so  = ScriptableObject.CreateInstance<WeaponPartSO>();
            var def = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(so, "_partDefinition", def);
            Assert.AreSame(def, so.PartDefinition);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(def);
        }
    }
}

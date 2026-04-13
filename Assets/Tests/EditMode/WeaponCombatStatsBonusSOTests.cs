using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WeaponCombatStatsBonusSO"/>.
    ///
    /// Covers:
    ///   Fresh instance defaults:
    ///   • RequiredWeaponType defaults to Physical.
    ///   • FlatDamageBonus defaults to 10.
    ///
    ///   Property round-trips (via reflection):
    ///   • RequiredWeaponType — Energy round-trip.
    ///   • RequiredWeaponType — Thermal round-trip.
    ///   • RequiredWeaponType — Shock round-trip.
    ///   • FlatDamageBonus — non-zero round-trip.
    ///   • FlatDamageBonus — zero round-trip (Min(0f) allows 0).
    /// </summary>
    public class WeaponCombatStatsBonusSOTests
    {
        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Fresh instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_RequiredWeaponType_DefaultsToPhysical()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            Assert.AreEqual(DamageType.Physical, so.RequiredWeaponType,
                "Default RequiredWeaponType must be Physical.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_FlatDamageBonus_DefaultsTen()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            Assert.AreEqual(10f, so.FlatDamageBonus, 0.001f,
                "Default FlatDamageBonus must be 10.");
            Object.DestroyImmediate(so);
        }

        // ── RequiredWeaponType round-trips ────────────────────────────────────

        [Test]
        public void RequiredWeaponType_Energy_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_requiredWeaponType", DamageType.Energy);
            Assert.AreEqual(DamageType.Energy, so.RequiredWeaponType);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RequiredWeaponType_Thermal_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_requiredWeaponType", DamageType.Thermal);
            Assert.AreEqual(DamageType.Thermal, so.RequiredWeaponType);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RequiredWeaponType_Shock_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_requiredWeaponType", DamageType.Shock);
            Assert.AreEqual(DamageType.Shock, so.RequiredWeaponType);
            Object.DestroyImmediate(so);
        }

        // ── FlatDamageBonus round-trips ───────────────────────────────────────

        [Test]
        public void FlatDamageBonus_NonZero_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_flatDamageBonus", 25f);
            Assert.AreEqual(25f, so.FlatDamageBonus, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FlatDamageBonus_Zero_RoundTrip()
        {
            var so = ScriptableObject.CreateInstance<WeaponCombatStatsBonusSO>();
            SetField(so, "_flatDamageBonus", 0f);
            Assert.AreEqual(0f, so.FlatDamageBonus, 0.001f,
                "Min(0f) attribute allows zero — property must return it.");
            Object.DestroyImmediate(so);
        }
    }
}

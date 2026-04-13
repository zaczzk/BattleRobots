using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageResistanceConfig"/> and the
    /// <see cref="DamageType"/> enum added to <see cref="DamageInfo"/>.
    ///
    /// Covers:
    ///   DamageResistanceConfig:
    ///   • Default instance — all resistance values are zero.
    ///   • GetResistance returns the correct field for each DamageType.
    ///   • GetResistance returns 0 for an unknown / out-of-range type.
    ///   • ApplyResistance with 0 resistance returns rawDamage unchanged.
    ///   • ApplyResistance with 0.5 resistance halves damage.
    ///   • ApplyResistance with 0.9 resistance reduces damage by 90 %.
    ///   • ApplyResistance(0 rawDamage) returns 0.
    ///   • ApplyResistance(negative rawDamage) returns 0 (clamped).
    ///   DamageInfo.damageType:
    ///   • Default constructor → damageType is Physical.
    ///   • Four-argument constructor → damageType defaults to Physical.
    ///   • Five-argument constructor sets damageType explicitly.
    ///   • DamageInfo remains a copy-by-value struct after the new field is added.
    ///   • ToString includes the damage type.
    ///   • DamageType enum values have the expected integer backing.
    /// </summary>
    public class DamageResistanceConfigTests
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

        private static DamageResistanceConfig CreateConfig(
            float physical = 0f, float energy = 0f,
            float thermal  = 0f, float shock  = 0f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            SetField(cfg, "_physicalResistance", physical);
            SetField(cfg, "_energyResistance",   energy);
            SetField(cfg, "_thermalResistance",  thermal);
            SetField(cfg, "_shockResistance",    shock);
            return cfg;
        }

        // ── DamageResistanceConfig: default values ────────────────────────────

        [Test]
        public void DefaultInstance_AllResistances_AreZero()
        {
            var cfg = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            Assert.AreEqual(0f, cfg.PhysicalResistance, 0.0001f);
            Assert.AreEqual(0f, cfg.EnergyResistance,   0.0001f);
            Assert.AreEqual(0f, cfg.ThermalResistance,  0.0001f);
            Assert.AreEqual(0f, cfg.ShockResistance,    0.0001f);
            Object.DestroyImmediate(cfg);
        }

        // ── DamageResistanceConfig: GetResistance ─────────────────────────────

        [Test]
        public void GetResistance_Physical_ReturnsPhysicalValue()
        {
            var cfg = CreateConfig(physical: 0.3f);
            Assert.AreEqual(0.3f, cfg.GetResistance(DamageType.Physical), 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetResistance_Energy_ReturnsEnergyValue()
        {
            var cfg = CreateConfig(energy: 0.5f);
            Assert.AreEqual(0.5f, cfg.GetResistance(DamageType.Energy), 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetResistance_Thermal_ReturnsThermalValue()
        {
            var cfg = CreateConfig(thermal: 0.7f);
            Assert.AreEqual(0.7f, cfg.GetResistance(DamageType.Thermal), 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetResistance_Shock_ReturnsShockValue()
        {
            var cfg = CreateConfig(shock: 0.9f);
            Assert.AreEqual(0.9f, cfg.GetResistance(DamageType.Shock), 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetResistance_UnknownType_ReturnsZero()
        {
            var cfg = CreateConfig(physical: 0.5f, energy: 0.5f, thermal: 0.5f, shock: 0.5f);
            // Cast an out-of-range int to DamageType — should not throw and return 0.
            float result = 0f;
            Assert.DoesNotThrow(() => result = cfg.GetResistance((DamageType)99));
            Assert.AreEqual(0f, result, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        // ── DamageResistanceConfig: ApplyResistance ───────────────────────────

        [Test]
        public void ApplyResistance_ZeroResistance_ReturnsDamageUnchanged()
        {
            var cfg = CreateConfig(physical: 0f);
            float result = cfg.ApplyResistance(20f, DamageType.Physical);
            Assert.AreEqual(20f, result, 0.001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ApplyResistance_HalfResistance_HalvesDamage()
        {
            var cfg = CreateConfig(energy: 0.5f);
            float result = cfg.ApplyResistance(40f, DamageType.Energy);
            Assert.AreEqual(20f, result, 0.001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ApplyResistance_MaxResistance_ReducesByNinetyPercent()
        {
            var cfg = CreateConfig(thermal: 0.9f);
            float result = cfg.ApplyResistance(100f, DamageType.Thermal);
            Assert.AreEqual(10f, result, 0.001f, "0.9 resistance should leave 10 % of damage.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ApplyResistance_ZeroRawDamage_ReturnsZero()
        {
            var cfg = CreateConfig(shock: 0.5f);
            float result = cfg.ApplyResistance(0f, DamageType.Shock);
            Assert.AreEqual(0f, result, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ApplyResistance_NegativeRawDamage_ReturnsZero()
        {
            var cfg = CreateConfig(physical: 0.2f);
            float result = cfg.ApplyResistance(-10f, DamageType.Physical);
            Assert.AreEqual(0f, result, 0.0001f,
                "Negative rawDamage must be clamped to 0, not returned negative.");
            Object.DestroyImmediate(cfg);
        }

        // ── DamageInfo.damageType field ───────────────────────────────────────

        [Test]
        public void DamageInfo_DefaultConstructor_DamageTypeIsPhysical()
        {
            var info = new DamageInfo(10f);
            Assert.AreEqual(DamageType.Physical, info.damageType);
        }

        [Test]
        public void DamageInfo_FourArgConstructor_DamageTypeDefaultsToPhysical()
        {
            var info = new DamageInfo(10f, "src", Vector3.zero, null);
            Assert.AreEqual(DamageType.Physical, info.damageType);
        }

        [Test]
        public void DamageInfo_FiveArgConstructor_SetsDamageTypeExplicitly()
        {
            var info = new DamageInfo(15f, "laser", Vector3.one, null, DamageType.Energy);
            Assert.AreEqual(DamageType.Energy, info.damageType);
        }

        [Test]
        public void DamageInfo_DamageTypeField_CopiesByValue()
        {
            var original = new DamageInfo(10f, "src", Vector3.zero, null, DamageType.Thermal);
            DamageInfo copy = original;
            copy.damageType = DamageType.Shock;
            Assert.AreEqual(DamageType.Thermal, original.damageType,
                "Modifying copy.damageType must not affect original.");
        }

        [Test]
        public void DamageInfo_ToString_ContainsDamageType()
        {
            var info = new DamageInfo(25f, "cannon", Vector3.zero, null, DamageType.Thermal);
            string str = info.ToString();
            Assert.IsTrue(str.Contains("Thermal"),
                $"ToString should include the damage type. Got: {str}");
        }

        // ── DamageType enum backing values ────────────────────────────────────

        [Test]
        public void DamageType_EnumBackingValues_AreCorrect()
        {
            Assert.AreEqual(0, (int)DamageType.Physical);
            Assert.AreEqual(1, (int)DamageType.Energy);
            Assert.AreEqual(2, (int)DamageType.Thermal);
            Assert.AreEqual(3, (int)DamageType.Shock);
        }
    }
}

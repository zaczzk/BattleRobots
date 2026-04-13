using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the <see cref="DamageResistanceConfig"/> integration
    /// in <see cref="DamageReceiver"/>.
    ///
    /// Covers:
    ///   • ResistanceConfig property returns null by default.
    ///   • TakeDamage(DamageInfo) with null resistance config passes amount unchanged.
    ///   • TakeDamage(DamageInfo) with a resistance config reduces damage by the
    ///     correct fraction for the info.damageType.
    ///   • TakeDamage(DamageInfo) Energy type with no energy resistance → full damage.
    ///   • TakeDamage(DamageInfo) Thermal type with 0.5 resistance → half damage.
    ///   • TakeDamage(DamageInfo) Physical type with full resistance (0.9) → 10 % damage.
    ///   • TakeDamage(DamageInfo) Shock type routes through shock resistance.
    ///   • TakeDamage(float) bypasses resistance — raw amount unchanged.
    ///   • Null health SO guard fires debug warning without crash.
    ///   • TakeDamage(DamageInfo) with zero amount and resistance config → no health change.
    /// </summary>
    public class DamageReceiverResistanceTests
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

        private static HealthSO CreateHealth(float max = 200f)
        {
            var h = ScriptableObject.CreateInstance<HealthSO>();
            SetField(h, "_maxHealth", max);
            h.Reset();
            return h;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void ResistanceConfig_DefaultsToNull()
        {
            var go = new GameObject();
            var dr = go.AddComponent<DamageReceiver>();
            Assert.IsNull(dr.ResistanceConfig);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TakeDamage_DamageInfo_NullConfig_PassesAmountUnchanged()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(100f);
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", null);

            dr.TakeDamage(new DamageInfo(20f, "", Vector3.zero, null, DamageType.Energy));

            Assert.AreEqual(80f, health.CurrentHealth, 0.001f,
                "With no resistance config, full 20 damage should reach HealthSO.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void TakeDamage_DamageInfo_EnergyTypeNoEnergyResistance_FullDamage()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(100f);
            var cfg    = CreateConfig(energy: 0f);   // zero resistance
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", cfg);

            dr.TakeDamage(new DamageInfo(25f, "", Vector3.zero, null, DamageType.Energy));

            Assert.AreEqual(75f, health.CurrentHealth, 0.001f,
                "Zero energy resistance → full 25 damage.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamage_DamageInfo_ThermalType_HalfResistance_HalvesDamage()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(100f);
            var cfg    = CreateConfig(thermal: 0.5f);
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", cfg);

            dr.TakeDamage(new DamageInfo(40f, "", Vector3.zero, null, DamageType.Thermal));

            Assert.AreEqual(80f, health.CurrentHealth, 0.001f,
                "0.5 thermal resistance should halve 40 → 20 damage.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamage_DamageInfo_PhysicalType_HighResistance_ReducesByNinetyPct()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(200f);
            var cfg    = CreateConfig(physical: 0.9f);
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", cfg);

            dr.TakeDamage(new DamageInfo(100f, "", Vector3.zero, null, DamageType.Physical));

            Assert.AreEqual(190f, health.CurrentHealth, 0.001f,
                "0.9 physical resistance leaves 10 % = 10 damage from 100.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamage_DamageInfo_ShockType_RoutedThroughShockResistance()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(100f);
            var cfg    = CreateConfig(shock: 0.5f);   // only shock reduced
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", cfg);

            dr.TakeDamage(new DamageInfo(60f, "", Vector3.zero, null, DamageType.Shock));

            Assert.AreEqual(70f, health.CurrentHealth, 0.001f,
                "0.5 shock resistance halves 60 → 30 damage.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamage_Float_BypassesResistance_FullDamageApplied()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(100f);
            var cfg    = CreateConfig(physical: 0.9f, energy: 0.9f, thermal: 0.9f, shock: 0.9f);
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", cfg);

            // TakeDamage(float) does NOT apply resistance — only TakeDamage(DamageInfo) does.
            dr.TakeDamage(50f);

            Assert.AreEqual(50f, health.CurrentHealth, 0.001f,
                "TakeDamage(float) must bypass resistance and apply the full amount.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamage_DamageInfo_NullHealth_DoesNotThrow()
        {
            var go  = new GameObject();
            var dr  = go.AddComponent<DamageReceiver>();
            var cfg = CreateConfig(thermal: 0.3f);
            SetField(dr, "_health",           null);
            SetField(dr, "_resistanceConfig", cfg);

            Assert.DoesNotThrow(() =>
                dr.TakeDamage(new DamageInfo(30f, "", Vector3.zero, null, DamageType.Thermal)));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamage_DamageInfo_ZeroAmount_WithResistanceConfig_NoHealthChange()
        {
            var go     = new GameObject();
            var dr     = go.AddComponent<DamageReceiver>();
            var health = CreateHealth(100f);
            var cfg    = CreateConfig(energy: 0.5f);
            SetField(dr, "_health",           health);
            SetField(dr, "_resistanceConfig", cfg);

            dr.TakeDamage(new DamageInfo(0f, "", Vector3.zero, null, DamageType.Energy));

            Assert.AreEqual(100f, health.CurrentHealth, 0.001f,
                "Zero raw damage must result in no health change even with resistance config.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ResistanceConfig_AssignedViaReflection_PropertyReturnsIt()
        {
            var go  = new GameObject();
            var dr  = go.AddComponent<DamageReceiver>();
            var cfg = CreateConfig(energy: 0.3f);
            SetField(dr, "_resistanceConfig", cfg);

            Assert.AreSame(cfg, dr.ResistanceConfig);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }
    }
}

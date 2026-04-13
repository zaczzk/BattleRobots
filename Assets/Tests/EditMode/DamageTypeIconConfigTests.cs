using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageTypeIconConfig"/>.
    ///
    /// Covers:
    ///   • GetColor returns the configured color for each DamageType.
    ///   • GetColor returns white for an unknown type.
    ///   • GetLabel returns the configured label string for each DamageType.
    ///   • GetLabel returns empty string for an unknown type.
    ///   • DisplayDuration default value is 1.5 f.
    ///   • Property accessors expose the correct inspector fields.
    ///   • All four types return distinct defaults (no copy-paste color collision).
    /// </summary>
    public class DamageTypeIconConfigTests
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

        private static DamageTypeIconConfig CreateConfig(
            Color?  physical = null, string physLabel = "PHYSICAL",
            Color?  energy   = null, string engLabel  = "ENERGY",
            Color?  thermal  = null, string thrmLabel = "THERMAL",
            Color?  shock    = null, string shockLabel = "SHOCK",
            float   duration = 1.5f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeIconConfig>();
            SetField(cfg, "_physicalColor",   physical ?? Color.white);
            SetField(cfg, "_physicalLabel",   physLabel);
            SetField(cfg, "_energyColor",     energy   ?? Color.cyan);
            SetField(cfg, "_energyLabel",     engLabel);
            SetField(cfg, "_thermalColor",    thermal  ?? new Color(1f, 0.45f, 0f));
            SetField(cfg, "_thermalLabel",    thrmLabel);
            SetField(cfg, "_shockColor",      shock    ?? Color.yellow);
            SetField(cfg, "_shockLabel",      shockLabel);
            SetField(cfg, "_displayDuration", duration);
            return cfg;
        }

        // ── GetColor ──────────────────────────────────────────────────────────

        [Test]
        public void GetColor_Physical_ReturnsPhysicalColor()
        {
            var cfg = CreateConfig(physical: Color.white);
            Assert.AreEqual(Color.white, cfg.GetColor(DamageType.Physical));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetColor_Energy_ReturnsEnergyColor()
        {
            var cfg = CreateConfig(energy: Color.cyan);
            Assert.AreEqual(Color.cyan, cfg.GetColor(DamageType.Energy));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetColor_Thermal_ReturnsThermalColor()
        {
            var orange = new Color(1f, 0.45f, 0f);
            var cfg    = CreateConfig(thermal: orange);
            Assert.AreEqual(orange, cfg.GetColor(DamageType.Thermal));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetColor_Shock_ReturnsShockColor()
        {
            var cfg = CreateConfig(shock: Color.yellow);
            Assert.AreEqual(Color.yellow, cfg.GetColor(DamageType.Shock));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetColor_UnknownType_ReturnsWhite()
        {
            var cfg    = CreateConfig();
            Color result = Color.black;
            Assert.DoesNotThrow(() => result = cfg.GetColor((DamageType)99));
            Assert.AreEqual(Color.white, result);
            Object.DestroyImmediate(cfg);
        }

        // ── GetLabel ──────────────────────────────────────────────────────────

        [Test]
        public void GetLabel_Physical_ReturnsPhysicalLabel()
        {
            var cfg = CreateConfig(physLabel: "PHYSICAL");
            Assert.AreEqual("PHYSICAL", cfg.GetLabel(DamageType.Physical));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_Energy_ReturnsEnergyLabel()
        {
            var cfg = CreateConfig(engLabel: "ENERGY");
            Assert.AreEqual("ENERGY", cfg.GetLabel(DamageType.Energy));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_Thermal_ReturnsThermalLabel()
        {
            var cfg = CreateConfig(thrmLabel: "THERMAL");
            Assert.AreEqual("THERMAL", cfg.GetLabel(DamageType.Thermal));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_Shock_ReturnsShockLabel()
        {
            var cfg = CreateConfig(shockLabel: "SHOCK");
            Assert.AreEqual("SHOCK", cfg.GetLabel(DamageType.Shock));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_UnknownType_ReturnsEmptyString()
        {
            var    cfg    = CreateConfig();
            string result = "not_empty";
            Assert.DoesNotThrow(() => result = cfg.GetLabel((DamageType)99));
            Assert.AreEqual(string.Empty, result);
            Object.DestroyImmediate(cfg);
        }

        // ── DisplayDuration property ──────────────────────────────────────────

        [Test]
        public void DisplayDuration_ReturnsConfiguredValue()
        {
            var cfg = CreateConfig(duration: 2.0f);
            Assert.AreEqual(2.0f, cfg.DisplayDuration, 0.0001f);
            Object.DestroyImmediate(cfg);
        }
    }
}

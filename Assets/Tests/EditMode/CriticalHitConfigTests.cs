using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CriticalHitConfig"/>.
    ///
    /// Covers:
    ///   • Default field values.
    ///   • RaiseOnCrit null-channel safety.
    ///   • RaiseOnCrit fires the channel when assigned.
    ///   • ComputeCritDamage — null config passthrough.
    ///   • ComputeCritDamage — critChance = 0 (never crits).
    ///   • ComputeCritDamage — critChance = 1 (always crits, multiplies amount).
    ///   • ComputeCritDamage — isCrit out-param set correctly.
    /// </summary>
    public class CriticalHitConfigTests
    {
        private CriticalHitConfig CreateConfig(float chance = 0.1f, float multiplier = 2f)
        {
            var cfg = ScriptableObject.CreateInstance<CriticalHitConfig>();
            // Use reflection to set private serialized fields for test isolation.
            typeof(CriticalHitConfig)
                .GetField("_criticalChance",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(cfg, chance);
            typeof(CriticalHitConfig)
                .GetField("_criticalMultiplier",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(cfg, multiplier);
            return cfg;
        }

        [TearDown]
        public void TearDown()
        {
            // Prevent SO leaks between tests.
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CriticalChance_Is0Point1()
        {
            var cfg = ScriptableObject.CreateInstance<CriticalHitConfig>();
            Assert.AreEqual(0.1f, cfg.CriticalChance, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_CriticalMultiplier_Is2()
        {
            var cfg = ScriptableObject.CreateInstance<CriticalHitConfig>();
            Assert.AreEqual(2f, cfg.CriticalMultiplier, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        // ── RaiseOnCrit ───────────────────────────────────────────────────────

        [Test]
        public void RaiseOnCrit_NullChannel_DoesNotThrow()
        {
            var cfg = CreateConfig();
            Assert.DoesNotThrow(() => cfg.RaiseOnCrit());
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void RaiseOnCrit_WithChannel_RaisesEvent()
        {
            var cfg     = CreateConfig();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised  = 0;
            channel.RegisterCallback(() => raised++);
            typeof(CriticalHitConfig)
                .GetField("_onCriticalHit",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(cfg, channel);

            cfg.RaiseOnCrit();

            Assert.AreEqual(1, raised);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(channel);
        }

        // ── ComputeCritDamage ─────────────────────────────────────────────────

        [Test]
        public void ComputeCritDamage_NullConfig_ReturnRawAmount()
        {
            float result = CriticalHitConfig.ComputeCritDamage(50f, null, out bool isCrit);
            Assert.AreEqual(50f, result, 0.0001f);
            Assert.IsFalse(isCrit);
        }

        [Test]
        public void ComputeCritDamage_ZeroChance_NeverCrits()
        {
            var cfg = CreateConfig(chance: 0f, multiplier: 3f);
            // With chance = 0, Random.value (always ≥ 0) is never < 0 → no crit.
            float result = CriticalHitConfig.ComputeCritDamage(40f, cfg, out bool isCrit);
            Assert.AreEqual(40f, result, 0.0001f);
            Assert.IsFalse(isCrit);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ComputeCritDamage_FullChance_AlwaysCrits_MultipliesAmount()
        {
            var cfg = CreateConfig(chance: 1f, multiplier: 2f);
            // With chance = 1, Random.value (always < 1) → always crit.
            float result = CriticalHitConfig.ComputeCritDamage(30f, cfg, out bool isCrit);
            Assert.AreEqual(60f, result, 0.0001f);
            Assert.IsTrue(isCrit);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ComputeCritDamage_FullChance_IsCritOutParam_IsTrue()
        {
            var cfg = CreateConfig(chance: 1f, multiplier: 1.5f);
            CriticalHitConfig.ComputeCritDamage(10f, cfg, out bool isCrit);
            Assert.IsTrue(isCrit);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ComputeCritDamage_ZeroChance_IsCritOutParam_IsFalse()
        {
            var cfg = CreateConfig(chance: 0f, multiplier: 5f);
            CriticalHitConfig.ComputeCritDamage(10f, cfg, out bool isCrit);
            Assert.IsFalse(isCrit);
            Object.DestroyImmediate(cfg);
        }
    }
}

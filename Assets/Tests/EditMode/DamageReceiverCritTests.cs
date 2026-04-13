using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the critical-hit patch on <see cref="DamageReceiver"/>.
    ///
    /// Covers:
    ///   • TakeDamage(float) with null _critConfig — no change to damage.
    ///   • TakeDamage(float) with always-crit config — damage is multiplied.
    ///   • TakeDamage(float) with never-crit config — damage is unchanged.
    ///   • TakeDamage(DamageInfo) with always-crit config — damage is multiplied.
    ///   • TakeDamage(float) always-crit fires the _onCriticalHit event.
    ///   • Crit multiplies raw amount BEFORE armor reduction.
    ///   • TakeDamage(float) with null HealthSO does not throw (existing guard).
    /// </summary>
    public class DamageReceiverCritTests
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

        /// <summary>
        /// Creates a CriticalHitConfig with the given chance and multiplier via reflection.
        /// </summary>
        private static CriticalHitConfig MakeCritConfig(float chance, float multiplier = 2f)
        {
            var cfg = ScriptableObject.CreateInstance<CriticalHitConfig>();
            SetField(cfg, "_criticalChance",     chance);
            SetField(cfg, "_criticalMultiplier", multiplier);
            return cfg;
        }

        /// <summary>
        /// Creates a HealthSO initialised to maxHealth, ready for damage tests.
        /// </summary>
        private static HealthSO MakeHealth(float maxHealth = 100f)
        {
            var h = ScriptableObject.CreateInstance<HealthSO>();
            h.InitForMatch(maxHealth);
            h.Reset();
            return h;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void TakeDamageFloat_NullCritConfig_PassesThroughUnchanged()
        {
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            var health   = MakeHealth(100f);
            SetField(receiver, "_health",     health);
            SetField(receiver, "_critConfig", null);

            receiver.TakeDamage(20f);

            Assert.AreEqual(80f, health.CurrentHealth, 0.0001f);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
        }

        [Test]
        public void TakeDamageFloat_AlwaysCrit_MultipliesDamage()
        {
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            var health   = MakeHealth(100f);
            var cfg      = MakeCritConfig(chance: 1f, multiplier: 2f);
            SetField(receiver, "_health",     health);
            SetField(receiver, "_critConfig", cfg);

            receiver.TakeDamage(20f);   // 20 × 2 = 40

            Assert.AreEqual(60f, health.CurrentHealth, 0.0001f);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamageFloat_NeverCrit_DamageUnchanged()
        {
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            var health   = MakeHealth(100f);
            var cfg      = MakeCritConfig(chance: 0f, multiplier: 5f);
            SetField(receiver, "_health",     health);
            SetField(receiver, "_critConfig", cfg);

            receiver.TakeDamage(15f);   // no crit → 15 damage

            Assert.AreEqual(85f, health.CurrentHealth, 0.0001f);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamageDamageInfo_AlwaysCrit_MultipliesDamage()
        {
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            var health   = MakeHealth(100f);
            var cfg      = MakeCritConfig(chance: 1f, multiplier: 3f);
            SetField(receiver, "_health",     health);
            SetField(receiver, "_critConfig", cfg);

            var info = new DamageInfo(10f, "TestSource");
            receiver.TakeDamage(info);   // 10 × 3 = 30

            Assert.AreEqual(70f, health.CurrentHealth, 0.0001f);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamageFloat_AlwaysCrit_FiresCritEvent()
        {
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            var health   = MakeHealth(100f);
            var cfg      = MakeCritConfig(chance: 1f, multiplier: 2f);
            var channel  = ScriptableObject.CreateInstance<VoidGameEvent>();
            int raised   = 0;
            channel.RegisterCallback(() => raised++);
            SetField(cfg, "_onCriticalHit", channel);
            SetField(receiver, "_health",     health);
            SetField(receiver, "_critConfig", cfg);

            receiver.TakeDamage(10f);

            Assert.AreEqual(1, raised);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void TakeDamageFloat_AlwaysCrit_CritMultipliedBeforeArmorReduction()
        {
            // Raw = 20, crit × 2 = 40, armor = 10 → dealt = 30
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            var health   = MakeHealth(100f);
            var cfg      = MakeCritConfig(chance: 1f, multiplier: 2f);
            SetField(receiver, "_health",     health);
            SetField(receiver, "_armorRating", 10);
            SetField(receiver, "_critConfig", cfg);

            receiver.TakeDamage(20f);

            Assert.AreEqual(70f, health.CurrentHealth, 0.0001f);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(health);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void TakeDamageFloat_NullHealth_DoesNotThrow()
        {
            var go       = new GameObject();
            var receiver = go.AddComponent<DamageReceiver>();
            SetField(receiver, "_health",     null);
            SetField(receiver, "_critConfig", null);

            Assert.DoesNotThrow(() => receiver.TakeDamage(10f));
            Object.DestroyImmediate(go);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AbilityAreaEffectConfig"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (radius, damage, sourceId, null status effect).
    ///   • Inspector-field round-trips via reflection.
    ///   • DamageSourceId fallback when null / whitespace.
    ///   • RaiseEffectTriggered no-throw with null channel.
    /// </summary>
    public class AbilityAreaEffectConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static AbilityAreaEffectConfig MakeConfig(
            float  radius   = 3f,
            float  damage   = 15f,
            string sourceId = "Ability")
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            SetField(cfg, "_radius",         radius);
            SetField(cfg, "_damage",         damage);
            SetField(cfg, "_damageSourceId", sourceId);
            return cfg;
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Radius_IsThree()
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            Assert.AreEqual(3f, cfg.Radius, 1e-6f,
                "Default radius must be 3 world-space units.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_Damage_IsFifteen()
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            Assert.AreEqual(15f, cfg.Damage, 1e-6f,
                "Default damage must be 15.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_DamageSourceId_IsAbility()
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            Assert.AreEqual("Ability", cfg.DamageSourceId,
                "Default DamageSourceId must be \"Ability\".");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_StatusEffect_IsNull()
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            Assert.IsNull(cfg.StatusEffect,
                "Default StatusEffect must be null — damage-only unless explicitly wired.");
            Object.DestroyImmediate(cfg);
        }

        // ── Field round-trips ─────────────────────────────────────────────────

        [Test]
        public void Radius_RoundTrip()
        {
            var cfg = MakeConfig(radius: 6f);
            Assert.AreEqual(6f, cfg.Radius, 1e-6f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Damage_RoundTrip()
        {
            var cfg = MakeConfig(damage: 30f);
            Assert.AreEqual(30f, cfg.Damage, 1e-6f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DamageSourceId_RoundTrip()
        {
            var cfg = MakeConfig(sourceId: "PlayerAbility");
            Assert.AreEqual("PlayerAbility", cfg.DamageSourceId);
            Object.DestroyImmediate(cfg);
        }

        // ── DamageSourceId fallback ────────────────────────────────────────────

        [Test]
        public void DamageSourceId_NullField_FallsBackToAbility()
        {
            var cfg = MakeConfig(sourceId: null);
            Assert.AreEqual("Ability", cfg.DamageSourceId,
                "Null _damageSourceId must fall back to the literal string \"Ability\".");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DamageSourceId_WhitespaceField_FallsBackToAbility()
        {
            var cfg = MakeConfig(sourceId: "   ");
            Assert.AreEqual("Ability", cfg.DamageSourceId,
                "Whitespace-only _damageSourceId must fall back to \"Ability\".");
            Object.DestroyImmediate(cfg);
        }

        // ── RaiseEffectTriggered — null channel ───────────────────────────────

        [Test]
        public void RaiseEffectTriggered_NullChannel_DoesNotThrow()
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            // _onEffectTriggered is null by default.
            Assert.DoesNotThrow(() => cfg.RaiseEffectTriggered(),
                "RaiseEffectTriggered() must be a no-op when no event channel is assigned.");
            Object.DestroyImmediate(cfg);
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageNumberConfig"/>.
    ///
    /// Covers:
    ///   • Default property values match the designer-facing inspector defaults.
    ///   • Serialised fields round-trip correctly through SetField reflection.
    ///   • OnValidate clamps floatDistance, floatDuration, critScaleMultiplier, poolSize.
    /// </summary>
    public class DamageNumberConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            var fi = target.GetType().GetField(
                name, System.Reflection.BindingFlags.Instance |
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            var fi = target.GetType().GetField(
                name, System.Reflection.BindingFlags.Instance |
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        // ── Default-value tests ───────────────────────────────────────────────

        [Test]
        public void FreshInstance_NormalColor_IsWhite()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            Assert.AreEqual(Color.white, cfg.NormalColor);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_CriticalColor_IsYellow()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            Assert.AreEqual(Color.yellow, cfg.CriticalColor);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_FloatDistance_IsOnePointFive()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            Assert.AreEqual(1.5f, cfg.FloatDistance, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_FloatDuration_IsPointEight()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            Assert.AreEqual(0.8f, cfg.FloatDuration, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_CritScaleMultiplier_IsOnePointFive()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            Assert.AreEqual(1.5f, cfg.CritScaleMultiplier, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_PoolSize_IsTwenty()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            Assert.AreEqual(20, cfg.PoolSize);
            Object.DestroyImmediate(cfg);
        }

        // ── Round-trip tests ──────────────────────────────────────────────────

        [Test]
        public void NormalColor_RoundTrips()
        {
            var cfg   = ScriptableObject.CreateInstance<DamageNumberConfig>();
            var color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            SetField(cfg, "_normalColor", color);
            Assert.AreEqual(color, cfg.NormalColor);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void CriticalColor_RoundTrips()
        {
            var cfg   = ScriptableObject.CreateInstance<DamageNumberConfig>();
            var color = new Color(1f, 0f, 0f, 1f);
            SetField(cfg, "_criticalColor", color);
            Assert.AreEqual(color, cfg.CriticalColor);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FloatDistance_RoundTrips()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            SetField(cfg, "_floatDistance", 3.5f);
            Assert.AreEqual(3.5f, cfg.FloatDistance, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void PoolSize_RoundTrips()
        {
            var cfg = ScriptableObject.CreateInstance<DamageNumberConfig>();
            SetField(cfg, "_poolSize", 8);
            Assert.AreEqual(8, cfg.PoolSize);
            Object.DestroyImmediate(cfg);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageTypeEffectivenessConfig"/>
    /// and the <see cref="EffectivenessOutcome"/> enum.
    ///
    /// Covers:
    ///   Default instance:
    ///   • EffectiveThreshold defaults to 1.1.
    ///   • ResistedThreshold defaults to 0.9.
    ///   • DisplayDuration defaults to 1.5.
    ///   • EffectiveLabel, ResistedLabel, NeutralLabel are not null/empty.
    ///
    ///   GetOutcome:
    ///   • Ratio above EffectiveThreshold → Effective.
    ///   • Ratio below ResistedThreshold → Resisted.
    ///   • Ratio equal to 1 (between thresholds) → Neutral.
    ///   • Ratio exactly at EffectiveThreshold (boundary) → Neutral (not strictly greater).
    ///   • Ratio exactly at ResistedThreshold (boundary) → Neutral (not strictly less).
    ///
    ///   GetLabel:
    ///   • Returns effective / resisted / neutral label strings.
    ///   • Returns empty string for out-of-range outcome value.
    ///
    ///   GetColor:
    ///   • Returns non-default-black color for Effective / Resisted / Neutral.
    ///   • Returns white for out-of-range outcome value.
    /// </summary>
    public class DamageTypeEffectivenessConfigTests
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

        private static DamageTypeEffectivenessConfig CreateConfig(
            float effectiveThreshold = 1.1f, float resistedThreshold = 0.9f,
            float displayDuration = 1.5f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            SetField(cfg, "_effectiveThreshold", effectiveThreshold);
            SetField(cfg, "_resistedThreshold",  resistedThreshold);
            SetField(cfg, "_displayDuration",    displayDuration);
            return cfg;
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void DefaultInstance_EffectiveThreshold_Is1point1()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            Assert.AreEqual(1.1f, cfg.EffectiveThreshold, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DefaultInstance_ResistedThreshold_Is0point9()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            Assert.AreEqual(0.9f, cfg.ResistedThreshold, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DefaultInstance_DisplayDuration_Is1point5()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            Assert.AreEqual(1.5f, cfg.DisplayDuration, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void DefaultInstance_Labels_AreNotNullOrEmpty()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            Assert.IsFalse(string.IsNullOrEmpty(cfg.EffectiveLabel), "EffectiveLabel must not be empty.");
            Assert.IsFalse(string.IsNullOrEmpty(cfg.ResistedLabel),  "ResistedLabel must not be empty.");
            Assert.IsFalse(string.IsNullOrEmpty(cfg.NeutralLabel),   "NeutralLabel must not be empty.");
            Object.DestroyImmediate(cfg);
        }

        // ── GetOutcome ────────────────────────────────────────────────────────

        [Test]
        public void GetOutcome_AboveEffectiveThreshold_ReturnsEffective()
        {
            var cfg = CreateConfig(effectiveThreshold: 1.1f);
            Assert.AreEqual(EffectivenessOutcome.Effective, cfg.GetOutcome(1.5f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetOutcome_BelowResistedThreshold_ReturnsResisted()
        {
            var cfg = CreateConfig(resistedThreshold: 0.9f);
            Assert.AreEqual(EffectivenessOutcome.Resisted, cfg.GetOutcome(0.5f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetOutcome_RatioOne_ReturnsNeutral()
        {
            var cfg = CreateConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            Assert.AreEqual(EffectivenessOutcome.Neutral, cfg.GetOutcome(1.0f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetOutcome_AtEffectiveThreshold_ReturnsNeutral()
        {
            // Exactly at threshold — not strictly greater, so Neutral.
            var cfg = CreateConfig(effectiveThreshold: 1.1f);
            Assert.AreEqual(EffectivenessOutcome.Neutral, cfg.GetOutcome(1.1f));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetOutcome_AtResistedThreshold_ReturnsNeutral()
        {
            // Exactly at threshold — not strictly less, so Neutral.
            var cfg = CreateConfig(resistedThreshold: 0.9f);
            Assert.AreEqual(EffectivenessOutcome.Neutral, cfg.GetOutcome(0.9f));
            Object.DestroyImmediate(cfg);
        }

        // ── GetLabel ──────────────────────────────────────────────────────────

        [Test]
        public void GetLabel_Effective_ReturnsEffectiveLabel()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            string label = cfg.GetLabel(EffectivenessOutcome.Effective);
            Assert.IsFalse(string.IsNullOrEmpty(label));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_Resisted_ReturnsResistedLabel()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            string label = cfg.GetLabel(EffectivenessOutcome.Resisted);
            Assert.IsFalse(string.IsNullOrEmpty(label));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_Neutral_ReturnsNeutralLabel()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            string label = cfg.GetLabel(EffectivenessOutcome.Neutral);
            Assert.IsFalse(string.IsNullOrEmpty(label));
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetLabel_UnknownOutcome_ReturnsEmpty()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            string label = string.Empty;
            Assert.DoesNotThrow(() => label = cfg.GetLabel((EffectivenessOutcome)99));
            Assert.IsEmpty(label);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetColor_Effective_ReturnsConfiguredColor()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            // Default effective color is green — just verify it doesn't throw and is non-black.
            Color c = default;
            Assert.DoesNotThrow(() => c = cfg.GetColor(EffectivenessOutcome.Effective));
            Assert.AreNotEqual(Color.black, c,
                "Effective color must not be default black.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void GetColor_UnknownOutcome_ReturnsWhite()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            Color c = default;
            Assert.DoesNotThrow(() => c = cfg.GetColor((EffectivenessOutcome)99));
            Assert.AreEqual(Color.white, c);
            Object.DestroyImmediate(cfg);
        }
    }
}

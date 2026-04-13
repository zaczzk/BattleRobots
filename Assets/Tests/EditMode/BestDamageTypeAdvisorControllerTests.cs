using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BestDamageTypeAdvisorController"/>.
    ///
    /// Covers:
    ///   Fresh instance:
    ///   • SelectedOpponent / EffectivenessConfig null.
    ///   • MaxRankings defaults to 4.
    ///
    ///   Null-safety:
    ///   • OnEnable with null channel does not throw.
    ///   • OnDisable with null channel does not throw.
    ///   • Refresh with all-null fields does not throw.
    ///   • Refresh with null _rankingLabels array does not throw.
    ///   • Refresh with null opponent does not throw.
    ///
    ///   Refresh behaviour:
    ///   • Null _effectivenessConfig → labels show "{rank}. {TypeName}".
    ///   • High vulnerability type is ranked first.
    ///   • High resistance type is ranked last.
    ///   • With _effectivenessConfig → labels show "{rank}. {Type}: {OutcomeLabel}".
    ///   • _maxRankings limit clears excess labels to empty string.
    ///
    ///   OnDisable:
    ///   • Unregisters delegate — raising channel after disable does not re-refresh.
    /// </summary>
    public class BestDamageTypeAdvisorControllerTests
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

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        private static DamageTypeEffectivenessConfig CreateEffectivenessConfig(
            float effectiveThreshold = 1.1f, float resistedThreshold = 0.9f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeEffectivenessConfig>();
            SetField(cfg, "_effectiveThreshold", effectiveThreshold);
            SetField(cfg, "_resistedThreshold",  resistedThreshold);
            SetField(cfg, "_effectiveLabel",     "EFFECTIVE!");
            SetField(cfg, "_resistedLabel",      "RESISTED!");
            SetField(cfg, "_neutralLabel",       "NEUTRAL");
            return cfg;
        }

        private static DamageResistanceConfig CreateResistanceConfig(
            float physical = 0f, float energy = 0f, float thermal = 0f, float shock = 0f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            SetField(cfg, "_physicalResistance", physical);
            SetField(cfg, "_energyResistance",   energy);
            SetField(cfg, "_thermalResistance",  thermal);
            SetField(cfg, "_shockResistance",    shock);
            return cfg;
        }

        private static DamageVulnerabilityConfig CreateVulnerabilityConfig(
            float physical = 1f, float energy = 1f, float thermal = 1f, float shock = 1f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageVulnerabilityConfig>();
            SetField(cfg, "_physicalMultiplier", physical);
            SetField(cfg, "_energyMultiplier",   energy);
            SetField(cfg, "_thermalMultiplier",  thermal);
            SetField(cfg, "_shockMultiplier",    shock);
            return cfg;
        }

        private static SelectedOpponentSO CreateSelectedOpponent(
            DamageResistanceConfig resist = null, DamageVulnerabilityConfig vuln = null)
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            SetField(profile, "_displayName", "TestOpponent");
            if (resist != null) SetField(profile, "_damageResistanceConfig",    resist);
            if (vuln   != null) SetField(profile, "_damageVulnerabilityConfig", vuln);
            var so = ScriptableObject.CreateInstance<SelectedOpponentSO>();
            so.Select(profile);
            return so;
        }

        private static Text CreateText()
        {
            var go = new GameObject();
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_SelectedOpponent_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            Assert.IsNull(ctl.SelectedOpponent);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_EffectivenessConfig_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            Assert.IsNull(ctl.EffectivenessConfig);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_MaxRankings_IsFour()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            Assert.AreEqual(4, ctl.MaxRankings,
                "MaxRankings should default to 4 (all DamageTypes).");
            Object.DestroyImmediate(go);
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            SetField(ctl, "_onOpponentChanged", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            SetField(ctl, "_onOpponentChanged", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_AllNull_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            Assert.DoesNotThrow(() => ctl.Refresh());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullRankingLabels_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<BestDamageTypeAdvisorController>();
            SetField(ctl, "_rankingLabels", null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Null _rankingLabels array should be handled gracefully.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullOpponent_DoesNotThrow()
        {
            var go     = new GameObject();
            var ctl    = go.AddComponent<BestDamageTypeAdvisorController>();
            var textGo = CreateText();
            SetField(ctl, "_selectedOpponent", null);
            SetField(ctl, "_rankingLabels", new[] { textGo.GetComponent<Text>() });
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Null _selectedOpponent should default to neutral (no throw).");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
        }

        // ── Refresh behaviour ─────────────────────────────────────────────────

        [Test]
        public void Refresh_NullEffectivenessConfig_ShowsTypeNameInLabels()
        {
            // Without a config, label[0] should be "{rank}. {TypeName}" (no outcome text).
            var go      = new GameObject();
            var ctl     = go.AddComponent<BestDamageTypeAdvisorController>();
            var textGo  = CreateText();
            var label   = textGo.GetComponent<Text>();

            SetField(ctl, "_selectedOpponent",    null);
            SetField(ctl, "_effectivenessConfig",  null);
            SetField(ctl, "_rankingLabels", new[] { label });

            ctl.Refresh();

            // All types neutral — insertion sort stable → Physical first.
            StringAssert.StartsWith("1. ", label.text,
                "Label should start with '1. ' (rank prefix).");
            // Type name should be in the label (no colon-separated outcome).
            Assert.IsFalse(label.text.Contains(":"),
                "Without effectivenessConfig, label should not contain ':' (no outcome).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
        }

        [Test]
        public void Refresh_HighVulnerabilityType_RankedFirst()
        {
            // Shock vulnerability ×3 → ratio = 3.0 → must be ranked #1.
            var go      = new GameObject();
            var ctl     = go.AddComponent<BestDamageTypeAdvisorController>();
            var vuln    = CreateVulnerabilityConfig(physical: 1f, energy: 1f, thermal: 1f, shock: 3f);
            var opSO    = CreateSelectedOpponent(vuln: vuln);
            var textGo  = CreateText();
            var label   = textGo.GetComponent<Text>();

            SetField(ctl, "_selectedOpponent",    opSO);
            SetField(ctl, "_effectivenessConfig",  null);
            SetField(ctl, "_rankingLabels", new[] { label });

            ctl.Refresh();

            StringAssert.Contains("Shock", label.text,
                "Shock (vulnerability ×3) should be ranked first.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(opSO);
            Object.DestroyImmediate(vuln);
        }

        [Test]
        public void Refresh_HighResistanceType_RankedLast()
        {
            // Physical resistance 0.9 → ratio = (1-0.9)×1 = 0.1 → must be ranked #4 (last).
            var go      = new GameObject();
            var ctl     = go.AddComponent<BestDamageTypeAdvisorController>();
            var resist  = CreateResistanceConfig(physical: 0.9f);
            var opSO    = CreateSelectedOpponent(resist: resist);
            // Four labels to capture full ranking.
            var texts   = new Text[4];
            var gos     = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                texts[i] = CreateText();
                gos[i]   = texts[i].gameObject;
            }

            SetField(ctl, "_selectedOpponent",    opSO);
            SetField(ctl, "_effectivenessConfig",  null);
            SetField(ctl, "_rankingLabels", texts);

            ctl.Refresh();

            StringAssert.Contains("Physical", texts[3].text,
                "Physical (resistance 0.9) should be ranked last (index 3).");

            Object.DestroyImmediate(go);
            foreach (var g in gos) Object.DestroyImmediate(g);
            Object.DestroyImmediate(opSO);
            Object.DestroyImmediate(resist);
        }

        [Test]
        public void Refresh_WithEffectivenessConfig_ShowsOutcomeLabel()
        {
            // Energy vulnerability ×2 → ratio 2.0 > 1.1 threshold → EFFECTIVE!
            // Label[0] should be "1. Energy: EFFECTIVE!".
            var go      = new GameObject();
            var ctl     = go.AddComponent<BestDamageTypeAdvisorController>();
            var vuln    = CreateVulnerabilityConfig(energy: 2f);
            var opSO    = CreateSelectedOpponent(vuln: vuln);
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var textGo  = CreateText();
            var label   = textGo.GetComponent<Text>();

            SetField(ctl, "_selectedOpponent",    opSO);
            SetField(ctl, "_effectivenessConfig",  cfg);
            SetField(ctl, "_rankingLabels", new[] { label });

            ctl.Refresh();

            StringAssert.Contains("Energy",     label.text, "Label should contain type name 'Energy'.");
            StringAssert.Contains("EFFECTIVE!", label.text, "Label should contain outcome 'EFFECTIVE!'.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(opSO);
            Object.DestroyImmediate(vuln);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Refresh_MaxRankingsLimit_ExcessLabelsCleared()
        {
            // _maxRankings = 2 with 4 labels → labels[2] and [3] must be empty.
            var go     = new GameObject();
            var ctl    = go.AddComponent<BestDamageTypeAdvisorController>();
            var texts  = new Text[4];
            var gos    = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                texts[i] = CreateText();
                gos[i]   = texts[i].gameObject;
                texts[i].text = "PLACEHOLDER"; // pre-fill to verify they get cleared
            }

            SetField(ctl, "_selectedOpponent",    null);
            SetField(ctl, "_effectivenessConfig",  null);
            SetField(ctl, "_rankingLabels", texts);
            SetField(ctl, "_maxRankings",   2);

            ctl.Refresh();

            Assert.IsFalse(string.IsNullOrEmpty(texts[0].text), "Label[0] should have content.");
            Assert.IsFalse(string.IsNullOrEmpty(texts[1].text), "Label[1] should have content.");
            Assert.AreEqual(string.Empty, texts[2].text, "Label[2] should be cleared (exceeds maxRankings).");
            Assert.AreEqual(string.Empty, texts[3].text, "Label[3] should be cleared (exceeds maxRankings).");

            Object.DestroyImmediate(go);
            foreach (var g in gos) Object.DestroyImmediate(g);
        }

        // ── OnDisable — unregisters ───────────────────────────────────────────

        [Test]
        public void OnDisable_Unregisters_ChannelNoLongerRefreshes()
        {
            // After OnDisable, raising _onOpponentChanged should NOT trigger Refresh.
            // Discriminator: initial opponent has Shock ×3 → labels[0] = "1. Shock".
            // After OnDisable we null out _selectedOpponent (so next Refresh would
            // produce "1. Physical" since all ratios equal → stable order).
            // If unregistered correctly, the channel raise doesn't run Refresh and
            // labels[0] stays "1. Shock".
            var go      = new GameObject();
            var ctl     = go.AddComponent<BestDamageTypeAdvisorController>();
            var vuln    = CreateVulnerabilityConfig(shock: 3f);
            var opSO    = CreateSelectedOpponent(vuln: vuln);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var textGo  = CreateText();
            var label   = textGo.GetComponent<Text>();

            SetField(ctl, "_selectedOpponent",    opSO);
            SetField(ctl, "_effectivenessConfig",  null);
            SetField(ctl, "_onOpponentChanged",    channel);
            SetField(ctl, "_rankingLabels", new[] { label });

            // Awake + OnEnable → Refresh → label[0] = "1. Shock".
            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");
            StringAssert.Contains("Shock", label.text,
                "Before OnDisable, label[0] should show Shock as best type.");

            // OnDisable → unregisters.
            InvokePrivate(ctl, "OnDisable");

            // Replace opponent with null — next Refresh (if it ran) would produce "1. Physical".
            SetField(ctl, "_selectedOpponent", null);

            // Raise the channel — should NOT trigger Refresh (unregistered).
            channel.Raise();

            StringAssert.Contains("Shock", label.text,
                "After OnDisable the channel raise should not trigger Refresh. " +
                "Label should still show Shock.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(opSO);
            Object.DestroyImmediate(vuln);
            Object.DestroyImmediate(channel);
        }
    }
}

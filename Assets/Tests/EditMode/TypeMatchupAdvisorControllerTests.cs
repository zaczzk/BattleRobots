using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TypeMatchupAdvisorController"/> and the
    /// <see cref="OpponentProfileSO"/> damage-profile extension (T161).
    ///
    /// TypeMatchupAdvisorController covers:
    ///   Fresh instance:
    ///   • WeaponPart / SelectedOpponent / EffectivenessConfig all null.
    ///
    ///   Property round-trips:
    ///   • WeaponPart / SelectedOpponent / EffectivenessConfig return injected values.
    ///
    ///   Null-safety:
    ///   • OnEnable with all-null fields does not throw.
    ///   • Refresh with null weapon part does not throw.
    ///   • Refresh with null selected opponent does not throw.
    ///   • Refresh with null advisor text does not throw.
    ///
    ///   Refresh behaviour:
    ///   • Null effectivenessConfig → advisor text shows weapon type name.
    ///   • Null effectivenessConfig → weaponTypeText shows weapon type name.
    ///   • Effective matchup (vulnerability ×2) → advisor text = "EFFECTIVE!".
    ///   • Resisted matchup (resistance = 0.8) → advisor text = "RESISTED!".
    ///   • Neutral matchup (ratio = 1.0) → advisor text = "NEUTRAL".
    ///   • Combined resistance + vulnerability → correct ratio computed.
    ///   • No opponent selection → neutral defaults (resistance=0, vuln=1).
    ///
    ///   SetWeaponPart:
    ///   • Updates WeaponPart and calls Refresh (advisor text reflects new type).
    ///   • Null part → no throw.
    ///
    ///   OpponentProfileSO extension:
    ///   • OpponentResistance defaults to null.
    ///   • OpponentVulnerability defaults to null.
    ///   • Both fields are readable after reflection-injection.
    /// </summary>
    public class TypeMatchupAdvisorControllerTests
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

        private static WeaponPartSO CreateWeaponPart(DamageType type = DamageType.Physical,
                                                      float damage    = 10f)
        {
            var so = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(so, "_damageType", type);
            SetField(so, "_baseDamage", damage);
            return so;
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
            SetField(cfg, "_displayDuration",    1.5f);
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

        private static SelectedOpponentSO CreateSelectedOpponent(OpponentProfileSO profile)
        {
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
        public void FreshInstance_WeaponPart_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            Assert.IsNull(ctl.WeaponPart);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_SelectedOpponent_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            Assert.IsNull(ctl.SelectedOpponent);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_EffectivenessConfig_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            Assert.IsNull(ctl.EffectivenessConfig);
            Object.DestroyImmediate(go);
        }

        // ── Property round-trips ──────────────────────────────────────────────

        [Test]
        public void WeaponPart_Property_ReturnsInjectedValue()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<TypeMatchupAdvisorController>();
            var part = CreateWeaponPart(DamageType.Energy);
            SetField(ctl, "_weaponPart", part);
            Assert.AreSame(part, ctl.WeaponPart);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void SelectedOpponent_Property_ReturnsInjectedValue()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            var so  = ScriptableObject.CreateInstance<SelectedOpponentSO>();
            SetField(ctl, "_selectedOpponent", so);
            Assert.AreSame(so, ctl.SelectedOpponent);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EffectivenessConfig_Property_ReturnsInjectedValue()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            var cfg = CreateEffectivenessConfig();
            SetField(ctl, "_effectivenessConfig", cfg);
            Assert.AreSame(cfg, ctl.EffectivenessConfig);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullFields_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullWeaponPart_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            SetField(ctl, "_weaponPart", null);
            Assert.DoesNotThrow(() => ctl.Refresh());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullSelectedOpponent_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            SetField(ctl, "_selectedOpponent", null);
            Assert.DoesNotThrow(() => ctl.Refresh());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullAdvisorText_DoesNotThrow()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<TypeMatchupAdvisorController>();
            var cfg  = CreateEffectivenessConfig();
            var part = CreateWeaponPart(DamageType.Energy);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_advisorText",         null);
            Assert.DoesNotThrow(() => ctl.Refresh());
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(part);
        }

        // ── Refresh behaviour ─────────────────────────────────────────────────

        [Test]
        public void Refresh_NullEffectivenessConfig_SetsAdvisorTextToWeaponTypeName()
        {
            var go       = new GameObject();
            var ctl      = go.AddComponent<TypeMatchupAdvisorController>();
            var part     = CreateWeaponPart(DamageType.Shock);
            var textGo   = CreateText();
            var advisor  = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",         part);
            SetField(ctl, "_effectivenessConfig", null);
            SetField(ctl, "_advisorText",         advisor);

            ctl.Refresh();
            Assert.AreEqual("Shock", advisor.text,
                "With null effectivenessConfig, advisor text should show the weapon type name.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Refresh_NullEffectivenessConfig_SetsWeaponTypeText()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var part    = CreateWeaponPart(DamageType.Thermal);
            var textGo  = CreateText();
            var typeText = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_effectivenessConfig",  null);
            SetField(ctl, "_weaponTypeText",       typeText);

            ctl.Refresh();
            Assert.AreEqual("Thermal", typeText.text,
                "_weaponTypeText should always show the weapon type name.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Refresh_EffectiveMatchup_SetsEffectiveLabel()
        {
            // Energy weapon vs. Energy vulnerability ×2 → ratio 2.0 > 1.1 threshold → Effective
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var part    = CreateWeaponPart(DamageType.Energy);
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var vuln    = CreateVulnerabilityConfig(energy: 2f);
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            SetField(profile, "_damageVulnerabilityConfig", vuln);
            var opSO    = CreateSelectedOpponent(profile);
            var textGo  = CreateText();
            var advisor = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_effectivenessConfig",  cfg);
            SetField(ctl, "_selectedOpponent",     opSO);
            SetField(ctl, "_advisorText",          advisor);

            ctl.Refresh();
            Assert.AreEqual("EFFECTIVE!", advisor.text,
                "Energy weapon vs. Energy vulnerability ×2 should be EFFECTIVE.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(vuln);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(opSO);
        }

        [Test]
        public void Refresh_ResistedMatchup_SetsResistedLabel()
        {
            // Physical weapon vs. 80 % physical resistance → ratio 0.2 < 0.9 threshold → Resisted
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var part    = CreateWeaponPart(DamageType.Physical);
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var resist  = CreateResistanceConfig(physical: 0.8f);
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            SetField(profile, "_damageResistanceConfig", resist);
            var opSO    = CreateSelectedOpponent(profile);
            var textGo  = CreateText();
            var advisor = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_effectivenessConfig",  cfg);
            SetField(ctl, "_selectedOpponent",     opSO);
            SetField(ctl, "_advisorText",          advisor);

            ctl.Refresh();
            Assert.AreEqual("RESISTED!", advisor.text,
                "Physical weapon vs. 80 % physical resistance should be RESISTED.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(opSO);
        }

        [Test]
        public void Refresh_NeutralMatchup_SetsNeutralLabel()
        {
            // Thermal weapon, no resistance, no vulnerability → ratio 1.0 → Neutral
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var part    = CreateWeaponPart(DamageType.Thermal);
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            var opSO    = CreateSelectedOpponent(profile);
            var textGo  = CreateText();
            var advisor = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_effectivenessConfig",  cfg);
            SetField(ctl, "_selectedOpponent",     opSO);
            SetField(ctl, "_advisorText",          advisor);

            ctl.Refresh();
            Assert.AreEqual("NEUTRAL", advisor.text,
                "Thermal weapon with no resistance/vulnerability should be NEUTRAL.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(opSO);
        }

        [Test]
        public void Refresh_NoOpponentSelection_DefaultsToNeutralRatio()
        {
            // No opponent selected → resistance=0, vuln=1 → ratio=1.0 → Neutral
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var part    = CreateWeaponPart(DamageType.Shock);
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var opSO    = ScriptableObject.CreateInstance<SelectedOpponentSO>();  // HasSelection=false
            var textGo  = CreateText();
            var advisor = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_effectivenessConfig",  cfg);
            SetField(ctl, "_selectedOpponent",     opSO);
            SetField(ctl, "_advisorText",          advisor);

            ctl.Refresh();
            Assert.AreEqual("NEUTRAL", advisor.text,
                "No opponent selected means neutral defaults → NEUTRAL label.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(opSO);
        }

        [Test]
        public void Refresh_CombinedResistanceAndVulnerability_ComputesCorrectRatio()
        {
            // Energy: resistance=0.5, vulnerability=2 → ratio = (1-0.5)×2 = 1.0 → Neutral
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var part    = CreateWeaponPart(DamageType.Energy);
            var cfg     = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var resist  = CreateResistanceConfig(energy: 0.5f);
            var vuln    = CreateVulnerabilityConfig(energy: 2f);
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            SetField(profile, "_damageResistanceConfig",    resist);
            SetField(profile, "_damageVulnerabilityConfig", vuln);
            var opSO    = CreateSelectedOpponent(profile);
            var textGo  = CreateText();
            var advisor = textGo.GetComponent<Text>();

            SetField(ctl, "_weaponPart",          part);
            SetField(ctl, "_effectivenessConfig",  cfg);
            SetField(ctl, "_selectedOpponent",     opSO);
            SetField(ctl, "_advisorText",          advisor);

            ctl.Refresh();
            // ratio = (1-0.5) × 2 = 1.0 → within neutral band [0.9, 1.1]
            Assert.AreEqual("NEUTRAL", advisor.text,
                "Combined resistance 0.5 × vulnerability 2 = ratio 1.0 → NEUTRAL.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(part);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(vuln);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(opSO);
        }

        // ── SetWeaponPart ─────────────────────────────────────────────────────

        [Test]
        public void SetWeaponPart_UpdatesWeaponPartAndRefreshes()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<TypeMatchupAdvisorController>();
            var partA   = CreateWeaponPart(DamageType.Physical);
            var partB   = CreateWeaponPart(DamageType.Shock);
            var textGo  = CreateText();
            var typeText = textGo.GetComponent<Text>();
            SetField(ctl, "_weaponTypeText", typeText);

            ctl.SetWeaponPart(partA);
            Assert.AreSame(partA, ctl.WeaponPart);
            Assert.AreEqual("Physical", typeText.text, "WeaponTypeText should reflect partA.");

            ctl.SetWeaponPart(partB);
            Assert.AreSame(partB, ctl.WeaponPart);
            Assert.AreEqual("Shock", typeText.text, "WeaponTypeText should reflect partB after swap.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGo.gameObject);
            Object.DestroyImmediate(partA);
            Object.DestroyImmediate(partB);
        }

        [Test]
        public void SetWeaponPart_Null_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<TypeMatchupAdvisorController>();
            Assert.DoesNotThrow(() => ctl.SetWeaponPart(null));
            Assert.IsNull(ctl.WeaponPart);
            Object.DestroyImmediate(go);
        }

        // ── OpponentProfileSO extension ───────────────────────────────────────

        [Test]
        public void OpponentProfileSO_OpponentResistance_DefaultsNull()
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            Assert.IsNull(profile.OpponentResistance,
                "OpponentResistance should default to null (backwards-compatible).");
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void OpponentProfileSO_OpponentVulnerability_DefaultsNull()
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            Assert.IsNull(profile.OpponentVulnerability,
                "OpponentVulnerability should default to null (backwards-compatible).");
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void OpponentProfileSO_OpponentResistance_RoundTrip()
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            var resist  = CreateResistanceConfig(physical: 0.4f);
            SetField(profile, "_damageResistanceConfig", resist);
            Assert.AreSame(resist, profile.OpponentResistance);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(resist);
        }

        [Test]
        public void OpponentProfileSO_OpponentVulnerability_RoundTrip()
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            var vuln    = CreateVulnerabilityConfig(thermal: 1.8f);
            SetField(profile, "_damageVulnerabilityConfig", vuln);
            Assert.AreSame(vuln, profile.OpponentVulnerability);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(vuln);
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="EquippedWeaponMatchupController"/>.
    ///
    /// Covers:
    ///   Null-safety:
    ///   • OnEnable / OnDisable with all null fields → no throw.
    ///   • OnEnable / OnDisable with null channels → no throw.
    ///
    ///   Unregistration:
    ///   • OnDisable unregisters from _onOpponentChanged (raising channel does not re-refresh).
    ///   • OnDisable unregisters from _onLoadoutChanged (raising channel does not re-refresh).
    ///
    ///   Refresh — panel hidden:
    ///   • Null _playerLoadout → panel hidden.
    ///   • Null _selectedOpponent → panel hidden.
    ///   • No weapon resolved from catalog → panel hidden.
    ///   • Null _effectivenessConfig → panel hidden.
    ///
    ///   Refresh — ratio + outcome:
    ///   • Neutral resistance + neutral vulnerability → ratio = 1.0 → NEUTRAL outcome.
    ///   • High physical resistance → ratio &lt; 0.9 → RESISTED outcome.
    ///
    ///   Refresh — panel shown:
    ///   • Weapon + opponent + effectivenessConfig all present → panel activated.
    ///
    ///   Event-driven refresh:
    ///   • Raising _onOpponentChanged after OnEnable triggers Refresh (label updates).
    /// </summary>
    public class EquippedWeaponMatchupControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic |
                                BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic |
                                   BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, System.Array.Empty<object>());
        }

        private static Text CreateText()
        {
            var go = new GameObject("TextHelper");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        private static void SetField_PartDefinition(PartDefinition def, string fieldName, object value)
        {
            FieldInfo fi = typeof(PartDefinition)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on PartDefinition.");
            fi.SetValue(def, value);
        }

        /// <summary>Creates a WeaponPartSO with the given damage type and a PartDefinition link.</summary>
        private static WeaponPartSO CreateWeapon(string partId, DamageType type)
        {
            var def = ScriptableObject.CreateInstance<PartDefinition>();
            SetField_PartDefinition(def, "_partId", partId);

            var weapon = ScriptableObject.CreateInstance<WeaponPartSO>();
            SetField(weapon, "_damageType",     type);
            SetField(weapon, "_partDefinition", def);
            return weapon;
        }

        private static WeaponPartCatalogSO CreateCatalog(WeaponPartSO weapon)
        {
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>();
            SetField(catalog, "_parts", new List<WeaponPartSO> { weapon });
            return catalog;
        }

        private static SelectedOpponentSO CreateSelectedOpponent(
            DamageResistanceConfig resist       = null,
            DamageVulnerabilityConfig vuln      = null)
        {
            var profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
            SetField(profile, "_displayName",               "TestOpponent");
            SetField(profile, "_damageResistanceConfig",    resist);
            SetField(profile, "_damageVulnerabilityConfig", vuln);

            var so = ScriptableObject.CreateInstance<SelectedOpponentSO>();
            so.Select(profile);
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
            return cfg;
        }

        private static DamageResistanceConfig CreateResistanceConfig(float physical = 0f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageResistanceConfig>();
            SetField(cfg, "_physicalResistance", physical);
            SetField(cfg, "_energyResistance",   0f);
            SetField(cfg, "_thermalResistance",  0f);
            SetField(cfg, "_shockResistance",    0f);
            return cfg;
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullAll_DoesNotThrow()
        {
            var go  = new GameObject("Test");
            var ctl = go.AddComponent<EquippedWeaponMatchupController>();
            // AddComponent fires Awake + OnEnable — verify no throw occurred.
            Assert.IsNotNull(ctl, "Controller should be created without exception.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullAll_DoesNotThrow()
        {
            var go  = new GameObject("Test");
            var ctl = go.AddComponent<EquippedWeaponMatchupController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all-null fields must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject("Test");
            var ctl = go.AddComponent<EquippedWeaponMatchupController>();
            SetField(ctl, "_onOpponentChanged", null);
            SetField(ctl, "_onLoadoutChanged",  null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject("Test");
            var ctl = go.AddComponent<EquippedWeaponMatchupController>();
            SetField(ctl, "_onOpponentChanged", null);
            SetField(ctl, "_onLoadoutChanged",  null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── Unregistration ────────────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOpponentChanged()
        {
            // Setup: weapon + opponent → panel shown after OnEnable.
            // OnDisable → null loadout (so next Refresh would hide panel).
            // Raise _onOpponentChanged → Refresh must NOT fire → panel stays shown.
            var go           = new GameObject("Test");
            var ctl          = go.AddComponent<EquippedWeaponMatchupController>();
            var channel      = ScriptableObject.CreateInstance<VoidGameEvent>();
            var weapon       = CreateWeapon("w001", DamageType.Physical);
            var catalog      = CreateCatalog(weapon);
            var opponent     = CreateSelectedOpponent();
            var cfg          = CreateEffectivenessConfig();
            var panelGO      = new GameObject("Panel");

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",     loadout);
            SetField(ctl, "_weaponCatalog",     catalog);
            SetField(ctl, "_selectedOpponent",  opponent);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_onOpponentChanged", channel);
            SetField(ctl, "_outcomePanel",      panelGO);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");
            Assert.IsTrue(panelGO.activeSelf, "Pre-condition: panel must be shown after OnEnable.");

            // Disable → unregisters.
            InvokePrivate(ctl, "OnDisable");

            // Null loadout so next Refresh would hide panel.
            SetField(ctl, "_playerLoadout", null);

            // Raise channel → should NOT trigger Refresh.
            channel.Raise();

            Assert.IsTrue(panelGO.activeSelf,
                "After OnDisable the channel raise must not trigger Refresh; panel stays shown.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDisable_UnregistersFromLoadoutChanged()
        {
            // Same pattern with _onLoadoutChanged.
            var go       = new GameObject("Test");
            var ctl      = go.AddComponent<EquippedWeaponMatchupController>();
            var channel  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var weapon   = CreateWeapon("w001", DamageType.Physical);
            var catalog  = CreateCatalog(weapon);
            var opponent = CreateSelectedOpponent();
            var cfg      = CreateEffectivenessConfig();
            var panelGO  = new GameObject("Panel");

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",     loadout);
            SetField(ctl, "_weaponCatalog",     catalog);
            SetField(ctl, "_selectedOpponent",  opponent);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_onLoadoutChanged",  channel);
            SetField(ctl, "_outcomePanel",      panelGO);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");
            Assert.IsTrue(panelGO.activeSelf, "Pre-condition: panel must be shown after OnEnable.");

            InvokePrivate(ctl, "OnDisable");
            SetField(ctl, "_playerLoadout", null);
            channel.Raise();

            Assert.IsTrue(panelGO.activeSelf,
                "After OnDisable the loadout channel must not trigger Refresh; panel stays shown.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(channel);
        }

        // ── Refresh — panel hidden ────────────────────────────────────────────

        [Test]
        public void Refresh_NullLoadout_HidesPanel()
        {
            var go      = new GameObject("Test");
            var ctl     = go.AddComponent<EquippedWeaponMatchupController>();
            var panelGO = new GameObject("Panel");
            panelGO.SetActive(true);

            SetField(ctl, "_playerLoadout",    null);
            SetField(ctl, "_outcomePanel",     panelGO);

            ctl.Refresh();

            Assert.IsFalse(panelGO.activeSelf, "Null loadout must hide the outcome panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
        }

        [Test]
        public void Refresh_NullOpponent_HidesPanel()
        {
            var go      = new GameObject("Test");
            var ctl     = go.AddComponent<EquippedWeaponMatchupController>();
            var weapon  = CreateWeapon("w001", DamageType.Physical);
            var catalog = CreateCatalog(weapon);
            var panelGO = new GameObject("Panel");
            panelGO.SetActive(true);

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",   loadout);
            SetField(ctl, "_weaponCatalog",   catalog);
            SetField(ctl, "_selectedOpponent", null);
            SetField(ctl, "_outcomePanel",    panelGO);

            ctl.Refresh();

            Assert.IsFalse(panelGO.activeSelf, "Null selectedOpponent must hide the outcome panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_NoWeaponFound_HidesPanel()
        {
            var go      = new GameObject("Test");
            var ctl     = go.AddComponent<EquippedWeaponMatchupController>();
            var catalog = ScriptableObject.CreateInstance<WeaponPartCatalogSO>(); // empty catalog
            var panelGO = new GameObject("Panel");
            panelGO.SetActive(true);

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "unknown_999" });

            SetField(ctl, "_playerLoadout",  loadout);
            SetField(ctl, "_weaponCatalog",  catalog);
            SetField(ctl, "_outcomePanel",   panelGO);

            ctl.Refresh();

            Assert.IsFalse(panelGO.activeSelf,
                "No catalog match for any equipped ID must hide the outcome panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_NullEffectivenessConfig_HidesPanel()
        {
            var go       = new GameObject("Test");
            var ctl      = go.AddComponent<EquippedWeaponMatchupController>();
            var weapon   = CreateWeapon("w001", DamageType.Physical);
            var catalog  = CreateCatalog(weapon);
            var opponent = CreateSelectedOpponent();
            var panelGO  = new GameObject("Panel");
            panelGO.SetActive(true);

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",     loadout);
            SetField(ctl, "_weaponCatalog",     catalog);
            SetField(ctl, "_selectedOpponent",  opponent);
            SetField(ctl, "_effectivenessConfig", null);
            SetField(ctl, "_outcomePanel",      panelGO);

            ctl.Refresh();

            Assert.IsFalse(panelGO.activeSelf,
                "Null effectivenessConfig must hide the outcome panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
        }

        // ── Refresh — ratio + outcome ─────────────────────────────────────────

        [Test]
        public void Refresh_NeutralResistanceVulnerability_NeutralOutcome()
        {
            // null resistance → 0; null vulnerability → ×1; ratio = 1.0 → Neutral.
            var go       = new GameObject("Test");
            var ctl      = go.AddComponent<EquippedWeaponMatchupController>();
            var weapon   = CreateWeapon("w001", DamageType.Physical);
            var catalog  = CreateCatalog(weapon);
            var opponent = CreateSelectedOpponent();  // no resistance / vulnerability configs
            var cfg      = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var labelGO  = CreateText();
            var panelGO  = new GameObject("Panel");

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",      loadout);
            SetField(ctl, "_weaponCatalog",      catalog);
            SetField(ctl, "_selectedOpponent",   opponent);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_outcomeLabel",       labelGO);
            SetField(ctl, "_outcomePanel",       panelGO);

            ctl.Refresh();

            StringAssert.Contains("NEUTRAL", labelGO.text,
                "Neutral ratio (1.0) with thresholds 0.9/1.1 must produce 'NEUTRAL' label.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGO.gameObject);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Refresh_HighPhysicalResistance_ResistedOutcome()
        {
            // Physical resistance 0.9 → ratio = (1-0.9)×1 = 0.1 < 0.9 threshold → RESISTED.
            var go       = new GameObject("Test");
            var ctl      = go.AddComponent<EquippedWeaponMatchupController>();
            var weapon   = CreateWeapon("w001", DamageType.Physical);
            var catalog  = CreateCatalog(weapon);
            var resist   = CreateResistanceConfig(physical: 0.9f);
            var opponent = CreateSelectedOpponent(resist: resist);
            var cfg      = CreateEffectivenessConfig(effectiveThreshold: 1.1f, resistedThreshold: 0.9f);
            var labelGO  = CreateText();
            var panelGO  = new GameObject("Panel");

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",      loadout);
            SetField(ctl, "_weaponCatalog",      catalog);
            SetField(ctl, "_selectedOpponent",   opponent);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_outcomeLabel",       labelGO);
            SetField(ctl, "_outcomePanel",       panelGO);

            ctl.Refresh();

            StringAssert.Contains("RESISTED", labelGO.text,
                "High physical resistance (0.9) must produce 'RESISTED!' label.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGO.gameObject);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
            Object.DestroyImmediate(resist);
            Object.DestroyImmediate(cfg);
        }

        // ── Refresh — panel shown ─────────────────────────────────────────────

        [Test]
        public void Refresh_WithWeaponAndOpponent_ShowsPanel()
        {
            var go       = new GameObject("Test");
            var ctl      = go.AddComponent<EquippedWeaponMatchupController>();
            var weapon   = CreateWeapon("w001", DamageType.Energy);
            var catalog  = CreateCatalog(weapon);
            var opponent = CreateSelectedOpponent();
            var cfg      = CreateEffectivenessConfig();
            var panelGO  = new GameObject("Panel");
            panelGO.SetActive(false);

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "w001" });

            SetField(ctl, "_playerLoadout",      loadout);
            SetField(ctl, "_weaponCatalog",      catalog);
            SetField(ctl, "_selectedOpponent",   opponent);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_outcomePanel",       panelGO);

            ctl.Refresh();

            Assert.IsTrue(panelGO.activeSelf,
                "Panel must be activated when weapon + opponent + config are all present.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
            Object.DestroyImmediate(cfg);
        }

        // ── Event-driven refresh ──────────────────────────────────────────────

        [Test]
        public void OnOpponentChanged_Raised_TriggersRefresh()
        {
            // After OnEnable the panel is hidden (no weapon).
            // Assign weapon + catalog, then raise channel → Refresh fires → panel shown.
            var go           = new GameObject("Test");
            go.SetActive(false);
            var ctl          = go.AddComponent<EquippedWeaponMatchupController>();
            var channel      = ScriptableObject.CreateInstance<VoidGameEvent>();
            var weapon       = CreateWeapon("w001", DamageType.Energy);
            var catalog      = CreateCatalog(weapon);
            var opponent     = CreateSelectedOpponent();
            var cfg          = CreateEffectivenessConfig();
            var panelGO      = new GameObject("Panel");
            panelGO.SetActive(false);

            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            // No weapon ID yet → Refresh hides panel.
            SetField(ctl, "_playerLoadout",      loadout);
            SetField(ctl, "_weaponCatalog",      catalog);
            SetField(ctl, "_selectedOpponent",   opponent);
            SetField(ctl, "_effectivenessConfig", cfg);
            SetField(ctl, "_onOpponentChanged",  channel);
            SetField(ctl, "_outcomePanel",       panelGO);

            go.SetActive(true);  // Awake + OnEnable fire → Refresh → panel hidden (no weapon ID)
            Assert.IsFalse(panelGO.activeSelf, "Pre-condition: panel hidden with empty loadout.");

            // Now give the loadout a matching weapon and raise the channel.
            loadout.SetLoadout(new List<string> { "w001" });
            channel.Raise();   // triggers Refresh via registered delegate

            Assert.IsTrue(panelGO.activeSelf,
                "Raising _onOpponentChanged after loading a weapon must trigger Refresh and show panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(loadout);
            Object.DestroyImmediate(weapon.PartDefinition);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(opponent);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(channel);
        }
    }
}

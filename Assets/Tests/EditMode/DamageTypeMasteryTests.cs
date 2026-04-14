using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T179:
    ///   <see cref="DamageTypeMasteryConfig"/>,
    ///   <see cref="DamageTypeMasterySO"/>, and
    ///   <see cref="MasteryController"/>.
    ///
    /// DamageTypeMasteryConfigTests (10):
    ///   FreshInstance_AllThresholdsAreOneThousand ×4 (one per type)
    ///   GetThreshold_ReturnsCorrectField ×4 (Physical/Energy/Thermal/Shock)
    ///   GetThreshold_UnknownType_ReturnsOne ×1
    ///   PropertyRoundTrip_Physical ×1 (spot-check)
    ///
    /// DamageTypeMasterySOTests (12):
    ///   FreshInstance_AllAccumsZero ×1
    ///   FreshInstance_NoneAreMastered ×1
    ///   AddDealt_ZeroAmount_NoChange ×1
    ///   AddDealt_Physical_AccumCorrect ×1
    ///   AddDealt_BelowThreshold_NotMastered ×1
    ///   AddDealt_AtThreshold_IsMastered ×1
    ///   AddDealt_AlreadyMastered_NoSecondEvent ×1
    ///   GetProgress_NullConfig_ReturnsZero ×1
    ///   GetProgress_BelowThreshold_CorrectRatio ×1
    ///   GetProgress_AtThreshold_ReturnsOne ×1
    ///   LoadSnapshot_RestoresAccumsAndFlags ×1
    ///   Reset_ClearsAllState ×1
    ///
    /// MasteryControllerTests (8):
    ///   OnEnable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow ×1
    ///   OnEnable_NullChannel_DoesNotThrow ×1
    ///   OnDisable_NullChannel_DoesNotThrow ×1
    ///   OnDisable_Unregisters ×1
    ///   Refresh_NullMastery_SetsZeroPercentLabel ×1
    ///   Refresh_Mastered_ShowsBadge ×1
    ///   FreshInstance_MasteryIsNull ×1
    ///
    /// Total: 30 new EditMode tests.
    /// </summary>
    public class DamageTypeMasteryTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static DamageTypeMasteryConfig CreateConfig(
            float physical = 1000f, float energy = 1000f,
            float thermal  = 1000f, float shock  = 1000f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            SetField(cfg, "_physicalThreshold", physical);
            SetField(cfg, "_energyThreshold",   energy);
            SetField(cfg, "_thermalThreshold",  thermal);
            SetField(cfg, "_shockThreshold",    shock);
            return cfg;
        }

        private static DamageTypeMasterySO CreateMastery(DamageTypeMasteryConfig cfg = null)
        {
            var so = ScriptableObject.CreateInstance<DamageTypeMasterySO>();
            if (cfg != null)
                SetField(so, "_config", cfg);
            return so;
        }

        private static VoidGameEvent CreateEvent()
            => ScriptableObject.CreateInstance<VoidGameEvent>();

        // ══════════════════════════════════════════════════════════════════════
        // DamageTypeMasteryConfigTests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Config_FreshInstance_PhysicalThresholdIsOneThousand()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            Assert.AreEqual(1000f, cfg.PhysicalThreshold, 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_FreshInstance_EnergyThresholdIsOneThousand()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            Assert.AreEqual(1000f, cfg.EnergyThreshold, 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_FreshInstance_ThermalThresholdIsOneThousand()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            Assert.AreEqual(1000f, cfg.ThermalThreshold, 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_FreshInstance_ShockThresholdIsOneThousand()
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            Assert.AreEqual(1000f, cfg.ShockThreshold, 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetThreshold_Physical_ReturnsPhysicalField()
        {
            var cfg = CreateConfig(physical: 500f);
            Assert.AreEqual(500f, cfg.GetThreshold(DamageType.Physical), 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetThreshold_Energy_ReturnsEnergyField()
        {
            var cfg = CreateConfig(energy: 750f);
            Assert.AreEqual(750f, cfg.GetThreshold(DamageType.Energy), 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetThreshold_Thermal_ReturnsThermalField()
        {
            var cfg = CreateConfig(thermal: 1200f);
            Assert.AreEqual(1200f, cfg.GetThreshold(DamageType.Thermal), 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetThreshold_Shock_ReturnsShockField()
        {
            var cfg = CreateConfig(shock: 800f);
            Assert.AreEqual(800f, cfg.GetThreshold(DamageType.Shock), 0.01f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_GetThreshold_UnknownType_ReturnsOne()
        {
            var cfg = CreateConfig();
            Assert.AreEqual(1f, cfg.GetThreshold((DamageType)999), 0.001f,
                "Unknown DamageType should return the minimal threshold 1.");
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Config_PropertyRoundTrip_Physical()
        {
            var cfg = CreateConfig(physical: 2500f);
            Assert.AreEqual(2500f, cfg.PhysicalThreshold, 0.01f);
            Object.DestroyImmediate(cfg);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DamageTypeMasterySOTests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Mastery_FreshInstance_AllAccumsZero()
        {
            var so = CreateMastery();
            Assert.AreEqual(0f, so.GetAccumulation(DamageType.Physical), 0.001f);
            Assert.AreEqual(0f, so.GetAccumulation(DamageType.Energy),   0.001f);
            Assert.AreEqual(0f, so.GetAccumulation(DamageType.Thermal),  0.001f);
            Assert.AreEqual(0f, so.GetAccumulation(DamageType.Shock),    0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Mastery_FreshInstance_NoneAreMastered()
        {
            var so = CreateMastery();
            Assert.IsFalse(so.IsTypeMastered(DamageType.Physical));
            Assert.IsFalse(so.IsTypeMastered(DamageType.Energy));
            Assert.IsFalse(so.IsTypeMastered(DamageType.Thermal));
            Assert.IsFalse(so.IsTypeMastered(DamageType.Shock));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Mastery_AddDealt_ZeroAmount_NoChange()
        {
            var cfg = CreateConfig(physical: 100f);
            var so  = CreateMastery(cfg);
            so.AddDealt(0f, DamageType.Physical);
            Assert.AreEqual(0f, so.GetAccumulation(DamageType.Physical), 0.001f);
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Mastery_AddDealt_Physical_AccumCorrect()
        {
            var cfg = CreateConfig(physical: 1000f);
            var so  = CreateMastery(cfg);
            so.AddDealt(300f, DamageType.Physical);
            Assert.AreEqual(300f, so.GetAccumulation(DamageType.Physical), 0.01f);
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Mastery_AddDealt_BelowThreshold_NotMastered()
        {
            var cfg = CreateConfig(physical: 1000f);
            var so  = CreateMastery(cfg);
            so.AddDealt(999f, DamageType.Physical);
            Assert.IsFalse(so.IsTypeMastered(DamageType.Physical));
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Mastery_AddDealt_AtThreshold_IsMastered()
        {
            var cfg = CreateConfig(physical: 1000f);
            var so  = CreateMastery(cfg);
            so.AddDealt(1000f, DamageType.Physical);
            Assert.IsTrue(so.IsTypeMastered(DamageType.Physical),
                "Accumulation == threshold should grant mastery.");
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Mastery_AddDealt_AlreadyMastered_NoSecondEventFired()
        {
            int eventCount = 0;
            var ch  = CreateEvent();
            ch.RegisterCallback(() => eventCount++);

            var cfg = CreateConfig(physical: 100f);
            var so  = CreateMastery(cfg);
            SetField(so, "_onMasteryUnlocked", ch);

            so.AddDealt(100f, DamageType.Physical); // crosses threshold → event fires once
            so.AddDealt(100f, DamageType.Physical); // already mastered → no second event

            Assert.AreEqual(1, eventCount,
                "_onMasteryUnlocked should fire exactly once per type.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Mastery_GetProgress_NullConfig_ReturnsZero()
        {
            var so = CreateMastery(); // no config
            so.AddDealt(500f, DamageType.Energy);
            Assert.AreEqual(0f, so.GetProgress(DamageType.Energy), 0.001f,
                "Without a config, GetProgress should return 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Mastery_GetProgress_BelowThreshold_CorrectRatio()
        {
            var cfg = CreateConfig(energy: 1000f);
            var so  = CreateMastery(cfg);
            so.AddDealt(400f, DamageType.Energy);
            Assert.AreEqual(0.4f, so.GetProgress(DamageType.Energy), 0.001f);
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Mastery_GetProgress_AtThreshold_ReturnsOne()
        {
            var cfg = CreateConfig(shock: 500f);
            var so  = CreateMastery(cfg);
            so.AddDealt(500f, DamageType.Shock);
            Assert.AreEqual(1f, so.GetProgress(DamageType.Shock), 0.001f);
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Mastery_LoadSnapshot_RestoresAccumsAndFlags()
        {
            var so = CreateMastery();
            so.LoadSnapshot(100f, 200f, 300f, 400f, true, false, true, false);

            Assert.AreEqual(100f, so.GetAccumulation(DamageType.Physical), 0.01f);
            Assert.AreEqual(200f, so.GetAccumulation(DamageType.Energy),   0.01f);
            Assert.AreEqual(300f, so.GetAccumulation(DamageType.Thermal),  0.01f);
            Assert.AreEqual(400f, so.GetAccumulation(DamageType.Shock),    0.01f);
            Assert.IsTrue(so.IsTypeMastered(DamageType.Physical));
            Assert.IsFalse(so.IsTypeMastered(DamageType.Energy));
            Assert.IsTrue(so.IsTypeMastered(DamageType.Thermal));
            Assert.IsFalse(so.IsTypeMastered(DamageType.Shock));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void Mastery_Reset_ClearsAllState()
        {
            var cfg = CreateConfig(physical: 100f);
            var so  = CreateMastery(cfg);
            so.AddDealt(100f, DamageType.Physical);
            so.Reset();

            Assert.AreEqual(0f, so.GetAccumulation(DamageType.Physical), 0.001f);
            Assert.IsFalse(so.IsTypeMastered(DamageType.Physical));
            Object.DestroyImmediate(so); Object.DestroyImmediate(cfg);
        }

        // ══════════════════════════════════════════════════════════════════════
        // MasteryControllerTests
        // ══════════════════════════════════════════════════════════════════════

        private static MasteryController CreateController()
        {
            var go = new GameObject("MasteryController_Test");
            return go.AddComponent<MasteryController>();
        }

        [Test]
        public void MasteryCtrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MasteryCtrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MasteryCtrl_OnEnable_NullChannel_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MasteryCtrl_OnDisable_NullChannel_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MasteryCtrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable, only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void MasteryCtrl_Refresh_NullMastery_SetsZeroPercentLabel()
        {
            var ctrl   = CreateController();
            var labelGO = new GameObject("Label");
            var text    = labelGO.AddComponent<UnityEngine.UI.Text>();
            SetField(ctrl, "_physicalText", text);
            // _mastery is null.

            ctrl.Refresh();

            Assert.AreEqual("0%", text.text,
                "Null mastery should show 0% progress for each type.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void MasteryCtrl_Refresh_MasteredType_ShowsBadge()
        {
            var ctrl   = CreateController();
            var cfg    = CreateConfig(physical: 100f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(100f, DamageType.Physical);

            var badgeGO = new GameObject("Badge");
            badgeGO.SetActive(false);
            SetField(ctrl, "_mastery",       mastery);
            SetField(ctrl, "_physicalBadge", badgeGO);

            ctrl.Refresh();

            Assert.IsTrue(badgeGO.activeSelf,
                "Mastered type badge should be shown after Refresh().");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badgeGO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void MasteryCtrl_FreshInstance_MasteryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Mastery, "Mastery property should default to null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}

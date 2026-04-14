using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T186:
    ///   <see cref="CareerMasteryStatsController"/>.
    ///
    /// CareerMasteryStatsControllerTests (14):
    ///   FreshInstance_MasteryIsNull ×1
    ///   FreshInstance_CatalogIsNull ×1
    ///   OnEnable_NullRefs_DoesNotThrow ×1
    ///   OnDisable_NullRefs_DoesNotThrow ×1
    ///   OnDisable_Unregisters_MasteryChannel ×1
    ///   OnDisable_Unregisters_MatchEndedChannel ×1
    ///   Refresh_NullMastery_DoesNotThrow ×1
    ///   Refresh_WithMastery_SetsAccumLabel ×1
    ///   Refresh_WithMastery_SetsProgressBar ×1
    ///   Refresh_TypeMastered_SetsBadgeActive ×1
    ///   Refresh_TypeNotMastered_SetsBadgeInactive ×1
    ///   Refresh_NullCatalog_NoTotalBonusLabel ×1
    ///   Refresh_WithCatalog_SetsTotalBonusLabel ×1
    ///   Refresh_NullAllUI_DoesNotThrow ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class CareerMasteryStatsControllerTests
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

        private static DamageTypeMasteryConfig CreateConfig(float threshold = 100f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            SetField(cfg, "_physicalThreshold", threshold);
            SetField(cfg, "_energyThreshold",   threshold);
            SetField(cfg, "_thermalThreshold",  threshold);
            SetField(cfg, "_shockThreshold",    threshold);
            return cfg;
        }

        private static DamageTypeMasterySO CreateMastery(DamageTypeMasteryConfig cfg = null)
        {
            var so = ScriptableObject.CreateInstance<DamageTypeMasterySO>();
            if (cfg != null) SetField(so, "_config", cfg);
            return so;
        }

        private static MasteryBonusCatalogSO CreateCatalog(MasteryBonusEntry[] entries)
        {
            var so = ScriptableObject.CreateInstance<MasteryBonusCatalogSO>();
            SetField(so, "_entries", entries);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static CareerMasteryStatsController CreateController()
        {
            var go = new GameObject("CareerMasteryStats_Test");
            return go.AddComponent<CareerMasteryStatsController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_MasteryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Mastery);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_CatalogIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_MasteryChannel()
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
                "OnDisable must unregister from the mastery channel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_MatchEndedChannel()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "OnDisable must unregister from the match-ended channel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Refresh_NullMastery_DoesNotThrow()
        {
            var ctrl = CreateController();
            // _mastery is null
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when mastery is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_WithMastery_SetsAccumLabel()
        {
            var ctrl   = CreateController();
            var cfg    = CreateConfig(1000f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(250f, DamageType.Physical);

            var labelGO = new GameObject("PhysicalAccum");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_mastery",              mastery);
            SetField(ctrl, "_physicalAccumLabel",   label);

            ctrl.Refresh();

            Assert.AreEqual("250", label.text,
                "Refresh must set the accumulation label to the rounded accumulation value.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Ctrl_Refresh_WithMastery_SetsProgressBar()
        {
            var ctrl    = CreateController();
            var cfg     = CreateConfig(200f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(100f, DamageType.Energy); // 50% progress

            var barGO = new GameObject("EnergyBar");
            var bar   = barGO.AddComponent<Slider>();

            SetField(ctrl, "_mastery",           mastery);
            SetField(ctrl, "_energyProgressBar", bar);

            ctrl.Refresh();

            Assert.AreEqual(0.5f, bar.value, 0.001f,
                "Refresh must set the progress bar value to GetProgress(Energy) = 0.5.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(barGO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Ctrl_Refresh_TypeMastered_SetsBadgeActive()
        {
            var ctrl    = CreateController();
            var cfg     = CreateConfig(50f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(50f, DamageType.Thermal); // masters Thermal

            var badge = new GameObject("ThermalBadge");
            badge.SetActive(false);

            SetField(ctrl, "_mastery",       mastery);
            SetField(ctrl, "_thermalBadge",  badge);

            ctrl.Refresh();

            Assert.IsTrue(badge.activeSelf,
                "Refresh must activate the badge when the type is mastered.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Ctrl_Refresh_TypeNotMastered_SetsBadgeInactive()
        {
            var ctrl    = CreateController();
            var mastery = CreateMastery(); // no config → never mastered

            var badge = new GameObject("ShockBadge");
            badge.SetActive(true);

            SetField(ctrl, "_mastery",     mastery);
            SetField(ctrl, "_shockBadge",  badge);

            ctrl.Refresh();

            Assert.IsFalse(badge.activeSelf,
                "Refresh must hide the badge when the type is not mastered.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(badge);
            Object.DestroyImmediate(mastery);
        }

        [Test]
        public void Ctrl_Refresh_NullCatalog_TotalBonusLabelUnchanged()
        {
            var ctrl    = CreateController();
            var mastery = CreateMastery();

            var labelGO = new GameObject("TotalBonus");
            var label   = labelGO.AddComponent<Text>();
            label.text  = "initial";

            SetField(ctrl, "_mastery",          mastery);
            SetField(ctrl, "_totalBonusLabel",  label);
            // _catalog is null

            ctrl.Refresh();

            Assert.AreEqual("initial", label.text,
                "Refresh must not touch _totalBonusLabel when catalog is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(mastery);
        }

        [Test]
        public void Ctrl_Refresh_WithCatalog_SetsTotalBonusLabel()
        {
            var ctrl    = CreateController();
            var cfg     = CreateConfig(10f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(10f, DamageType.Physical);  // masters Physical

            var catalog = CreateCatalog(new[]
            {
                new MasteryBonusEntry { requiredType = DamageType.Physical, bonusMultiplier = 1.5f },
            });

            var labelGO = new GameObject("TotalBonus");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_mastery",          mastery);
            SetField(ctrl, "_catalog",          catalog);
            SetField(ctrl, "_totalBonusLabel",  label);

            ctrl.Refresh();

            Assert.AreEqual("x1.50", label.text,
                "Refresh must show the total catalog multiplier in the total bonus label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Ctrl_Refresh_NullAllUI_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var mastery = CreateMastery();
            SetField(ctrl, "_mastery", mastery);
            // All UI refs null

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when all UI refs are null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(mastery);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T260: <see cref="ControlZonePresenterController"/>.
    ///
    /// ControlZonePresenterControllerTests (12):
    ///   FreshInstance_Catalog_Null                                         ×1
    ///   FreshInstance_DominanceSO_Null                                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters_DominanceChanged                             ×1
    ///   Refresh_NullCatalog_HidesPanel                                     ×1
    ///   Refresh_WithCatalog_ShowsPanel                                     ×1
    ///   Refresh_SummaryLabel_ShowsCorrectCount                             ×1
    ///   Refresh_DominanceBar_ReflectsRatio                                 ×1
    ///   Refresh_NullDominanceSO_BarIsZero                                  ×1
    ///   OnDominanceChanged_Event_TriggersRefresh                           ×1
    ///   Refresh_NullUIRefs_DoesNotThrow                                    ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ControlZonePresenterControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ControlZoneSO CreateZoneSO(bool captured = false)
        {
            var zone = ScriptableObject.CreateInstance<ControlZoneSO>();
            if (captured)
                zone.CaptureProgress(100f);   // fast-forward past any CaptureTime
            return zone;
        }

        private static ControlZoneCatalogSO CreateCatalog(ControlZoneSO[] zones)
        {
            var cat = ScriptableObject.CreateInstance<ControlZoneCatalogSO>();
            SetFieldOnType(cat, "_zones", zones);
            return cat;
        }

        private static void SetFieldOnType(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static ControlZonePresenterController CreateController() =>
            new GameObject("ZonePresenter_Test").AddComponent<ControlZonePresenterController>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        private static Text CreateLabel()
        {
            var go = new GameObject("Label_Test");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        private static Slider CreateSlider()
        {
            var go = new GameObject("Slider_Test");
            return go.AddComponent<Slider>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Catalog_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog,
                "Catalog must be null on a fresh ControlZonePresenterController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_DominanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.DominanceSO,
                "DominanceSO must be null on a fresh ControlZonePresenterController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_DominanceChanged()
        {
            var ctrl      = CreateController();
            var panel     = CreatePanel();
            var catalog   = CreateCatalog(new ControlZoneSO[0]);
            var evt       = CreateEvent();

            SetField(ctrl, "_catalog",              catalog);
            SetField(ctrl, "_onDominanceChanged",   evt);
            SetField(ctrl, "_panel",                panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Hide the panel after enable.
            panel.SetActive(false);

            InvokePrivate(ctrl, "OnDisable");

            // Raising event after disable must not show the panel again.
            evt.Raise();
            Assert.IsFalse(panel.activeSelf,
                "After OnDisable, _onDominanceChanged must not trigger Refresh.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullCatalog_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when Catalog is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithCatalog_ShowsPanel()
        {
            var ctrl    = CreateController();
            var panel   = CreatePanel();
            var catalog = CreateCatalog(new ControlZoneSO[0]);
            panel.SetActive(false);
            SetField(ctrl, "_catalog", catalog);
            SetField(ctrl, "_panel",   panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh must show the panel when Catalog is assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_SummaryLabel_ShowsCorrectCount()
        {
            var ctrl    = CreateController();
            var label   = CreateLabel();
            var zone0   = CreateZoneSO(captured: true);
            var zone1   = CreateZoneSO(captured: false);
            var catalog = CreateCatalog(new[] { zone0, zone1 });
            SetField(ctrl, "_catalog",       catalog);
            SetField(ctrl, "_summaryLabel",  label);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Zones: 1/2", label.text,
                "Refresh must update summary label with correct captured/total count.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
            Object.DestroyImmediate(zone0);
            Object.DestroyImmediate(zone1);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_DominanceBar_ReflectsRatio()
        {
            var ctrl      = CreateController();
            var bar       = CreateSlider();
            var dominance = CreateDominanceSO();
            var catalog   = CreateCatalog(new ControlZoneSO[0]);
            SetField(ctrl, "_catalog",      catalog);
            SetField(ctrl, "_dominanceSO",  dominance);
            SetField(ctrl, "_dominanceBar", bar);
            InvokePrivate(ctrl, "Awake");

            dominance.AddPlayerZone();   // 1 of 3 → ratio = 0.333...
            ctrl.Refresh();

            Assert.AreEqual(dominance.DominanceRatio, bar.value, 0.001f,
                "Refresh must set _dominanceBar.value to DominanceSO.DominanceRatio.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bar.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_NullDominanceSO_BarIsZero()
        {
            var ctrl    = CreateController();
            var bar     = CreateSlider();
            var catalog = CreateCatalog(new ControlZoneSO[0]);
            bar.value   = 0.8f;
            SetField(ctrl, "_catalog",      catalog);
            SetField(ctrl, "_dominanceBar", bar);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(0f, bar.value, 0.001f,
                "Refresh must set _dominanceBar.value to 0 when DominanceSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bar.gameObject);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void OnDominanceChanged_Event_TriggersRefresh()
        {
            var ctrl      = CreateController();
            var label     = CreateLabel();
            var zone0     = CreateZoneSO(captured: true);
            var catalog   = CreateCatalog(new[] { zone0 });
            var evt       = CreateEvent();
            SetField(ctrl, "_catalog",            catalog);
            SetField(ctrl, "_summaryLabel",       label);
            SetField(ctrl, "_onDominanceChanged", evt);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            evt.Raise();

            Assert.AreEqual("Zones: 1/1", label.text,
                "_onDominanceChanged must trigger Refresh and update the summary label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
            Object.DestroyImmediate(zone0);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullUIRefs_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var catalog = CreateCatalog(new ControlZoneSO[0]);
            SetField(ctrl, "_catalog", catalog);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when all UI refs are null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(catalog);
        }
    }
}

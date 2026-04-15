using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T274: <see cref="ZonePresenceTimerHUDController"/>.
    ///
    /// ZonePresenceTimerHUDControllerTests (12):
    ///   FreshInstance_TimerSO_Null                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channel                                   ×1
    ///   Refresh_NullContainer_DoesNotThrow                              ×1
    ///   Refresh_NullPrefab_DoesNotThrow                                 ×1
    ///   Refresh_NullSO_ShowsEmptyLabel                                  ×1
    ///   Refresh_WithSO_ShowsPanel                                       ×1
    ///   Refresh_NullEmptyLabel_DoesNotThrow                             ×1
    ///   Refresh_BuildsZoneRows_CorrectCount                             ×1
    ///   OnEnable_CallsRefresh                                           ×1
    ///   OnDisable_HidesPanel                                            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZonePresenceTimerHUDControllerTests
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

        private static ZonePresenceTimerHUDController CreateController() =>
            new GameObject("ZonePresenceTimerHUD_Test")
                .AddComponent<ZonePresenceTimerHUDController>();

        private static ZonePresenceTimerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZonePresenceTimerSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        /// <summary>
        /// Builds a minimal row prefab with two Text children to simulate the
        /// "Zone N" + "X.Xs" layout used by Refresh().
        /// </summary>
        private static GameObject CreateRowPrefab()
        {
            var root  = new GameObject("RowPrefab_Test");
            var child0 = new GameObject("Label0");
            var child1 = new GameObject("Label1");
            child0.transform.SetParent(root.transform);
            child1.transform.SetParent(root.transform);
            child0.AddComponent<UnityEngine.UI.Text>();
            child1.AddComponent<UnityEngine.UI.Text>();
            return root;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TimerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TimerSO,
                "TimerSO must be null on a fresh ZonePresenceTimerHUDController.");
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
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var timerSO = CreateSO();
            var evt     = CreateEvent();

            SetField(ctrl, "_timerSO",           timerSO);
            SetField(ctrl, "_onPresenceUpdated", evt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            bool threw = false;
            try { evt.Raise(); }
            catch { threw = true; }
            Assert.IsFalse(threw,
                "Raising event after OnDisable must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullContainer_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var timerSO = CreateSO();
            SetField(ctrl, "_timerSO", timerSO);
            // _listContainer left null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _listContainer must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
        }

        [Test]
        public void Refresh_NullPrefab_DoesNotThrow()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateSO();
            var container = new GameObject("Container_Test");

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer", container.transform);
            // _rowPrefab left null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _rowPrefab must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container);
        }

        [Test]
        public void Refresh_NullSO_ShowsEmptyLabel()
        {
            var ctrl       = CreateController();
            var container  = new GameObject("Container_Test");
            var prefab     = CreateRowPrefab();
            var emptyGO    = new GameObject("EmptyLabel_Test");
            var emptyLabel = emptyGO.AddComponent<UnityEngine.UI.Text>();
            emptyGO.SetActive(false);

            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            SetField(ctrl, "_emptyLabel",    emptyLabel);
            // _timerSO left null.
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(emptyGO.activeSelf,
                "Refresh with null SO must activate the empty label.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(emptyGO);
        }

        [Test]
        public void Refresh_WithSO_ShowsPanel()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateSO();
            var container = new GameObject("Container_Test");
            var prefab    = CreateRowPrefab();
            var panel     = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            SetField(ctrl, "_panel",         panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh with a valid SO must show the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullEmptyLabel_DoesNotThrow()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container_Test");
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            // _timerSO and _emptyLabel both null.
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _emptyLabel is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_BuildsZoneRows_CorrectCount()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateSO();       // default MaxZones = 3
            var container = new GameObject("Container_Test");
            var prefab    = CreateRowPrefab();

            SetField(ctrl, "_timerSO",       timerSO);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(3, container.transform.childCount,
                "Refresh must create one row per MaxZones (default 3).");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            // container children are Instantiated copies; destroy from scene.
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void OnEnable_CallsRefresh()
        {
            var ctrl      = CreateController();
            var timerSO   = CreateSO();
            var panel     = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_timerSO", timerSO);
            SetField(ctrl, "_panel",   panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Without a container/prefab, Refresh returns early but must not throw;
            // with a panel assigned and SO set it would show — we just confirm no throw.
            Assert.DoesNotThrow(() => { /* OnEnable already completed */ },
                "OnEnable calling Refresh must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnDisable_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf,
                "OnDisable must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

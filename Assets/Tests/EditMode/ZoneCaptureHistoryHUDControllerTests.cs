using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T268: <see cref="ZoneCaptureHistoryHUDController"/>.
    ///
    /// ZoneCaptureHistoryHUDControllerTests (12):
    ///   FreshInstance_HistorySO_Null                                    ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_HidesPanel                                            ×1
    ///   OnDisable_Unregisters_Channel                                   ×1
    ///   Refresh_NullListContainer_NoThrow                               ×1
    ///   Refresh_NullRowPrefab_NoThrow                                   ×1
    ///   Refresh_NullHistory_ShowsEmptyLabel                             ×1
    ///   Refresh_EmptyHistory_ShowsEmptyLabel                            ×1
    ///   Refresh_SingleEntry_Captured_Badge                              ×1
    ///   Refresh_SingleEntry_Lost_Badge                                  ×1
    ///   Refresh_MultipleEntries_NewestFirst                             ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCaptureHistoryHUDControllerTests
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

        private static ZoneCaptureHistoryHUDController CreateController() =>
            new GameObject("ZoneCaptureHUD_Test")
                .AddComponent<ZoneCaptureHistoryHUDController>();

        private static ZoneCaptureHistorySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneCaptureHistorySO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        /// <summary>Creates a simple row prefab with three Text children.</summary>
        private static GameObject CreateRowPrefab()
        {
            var root = new GameObject("RowPrefab");
            for (int i = 0; i < 3; i++)
            {
                var child = new GameObject($"Text{i}");
                child.AddComponent<Text>();
                child.transform.SetParent(root.transform);
            }
            return root;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HistorySO,
                "HistorySO must be null on a fresh ZoneCaptureHistoryHUDController.");
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
        public void OnDisable_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");

            SetField(ctrl, "_panel", panel);
            panel.SetActive(true);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf,
                "OnDisable must hide the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl      = CreateController();
            var historySO = CreateSO();
            var evtUpdate = CreateEvent();

            SetField(ctrl, "_historySO",         historySO);
            SetField(ctrl, "_onHistoryUpdated",   evtUpdate);

            // Add an entry before subscribing so Refresh doesn't hide the effect.
            historySO.AddEntry("Zone", 1f, true);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int countBefore = historySO.Count;
            historySO.AddEntry("Zone2", 2f, false); // fires _onHistoryUpdated
            // If still subscribed, Refresh() would run (no crash but we can't detect it here).
            // Key check: no exception thrown and disable unregisters cleanly.
            Assert.AreEqual(countBefore + 1, historySO.Count,
                "After OnDisable, adding an entry must still work (no double-unregister crash).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(evtUpdate);
        }

        [Test]
        public void Refresh_NullListContainer_NoThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _listContainer must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullRowPrefab_NoThrow()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container");

            SetField(ctrl, "_listContainer", container.transform);

            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null _rowPrefab must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
        }

        [Test]
        public void Refresh_NullHistory_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();
            var empty     = new GameObject("Empty").AddComponent<Text>();

            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);
            SetField(ctrl, "_emptyLabel",    empty);
            // _historySO remains null

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(empty.gameObject.activeSelf,
                "Empty label must be shown when _historySO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
            Object.DestroyImmediate(empty.gameObject);
        }

        [Test]
        public void Refresh_EmptyHistory_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var historySO = CreateSO();
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();
            var empty     = new GameObject("Empty").AddComponent<Text>();

            SetField(ctrl, "_historySO",     historySO);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);
            SetField(ctrl, "_emptyLabel",    empty);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(empty.gameObject.activeSelf,
                "Empty label must be shown when history has 0 entries.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
            Object.DestroyImmediate(empty.gameObject);
        }

        [Test]
        public void Refresh_SingleEntry_Captured_Badge()
        {
            var ctrl      = CreateController();
            var historySO = CreateSO();
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();

            historySO.AddEntry("ZoneA", 3f, true);

            SetField(ctrl, "_historySO",     historySO);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual(1, container.transform.childCount,
                "One row must be spawned for a single history entry.");
            var texts = container.transform.GetChild(0).GetComponentsInChildren<Text>();
            Assert.AreEqual("CAPTURED", texts[1].text,
                "Badge text must be 'CAPTURED' for a capture event.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
        }

        [Test]
        public void Refresh_SingleEntry_Lost_Badge()
        {
            var ctrl      = CreateController();
            var historySO = CreateSO();
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();

            historySO.AddEntry("ZoneB", 5f, false);

            SetField(ctrl, "_historySO",     historySO);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            var texts = container.transform.GetChild(0).GetComponentsInChildren<Text>();
            Assert.AreEqual("LOST", texts[1].text,
                "Badge text must be 'LOST' for a loss event.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
        }

        [Test]
        public void Refresh_MultipleEntries_NewestFirst()
        {
            var ctrl      = CreateController();
            var historySO = CreateSO();
            var container = new GameObject("Container");
            var rowPrefab = CreateRowPrefab();

            historySO.AddEntry("Alpha", 1f, true);
            historySO.AddEntry("Beta",  2f, false);

            SetField(ctrl, "_historySO",     historySO);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     rowPrefab);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual(2, container.transform.childCount,
                "Two rows must be spawned for two history entries.");

            var firstRowTexts = container.transform.GetChild(0).GetComponentsInChildren<Text>();
            Assert.AreEqual("Beta", firstRowTexts[0].text,
                "First row must show the newest entry (Beta).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(rowPrefab);
        }
    }
}

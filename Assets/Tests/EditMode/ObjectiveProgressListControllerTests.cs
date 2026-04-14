using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T224: <see cref="ObjectiveProgressListController"/>.
    ///
    /// ObjectiveProgressListControllerTests (12):
    ///   FreshInstance_ObjectivesNull                          ×1
    ///   OnEnable_NullRefs_DoesNotThrow                        ×1
    ///   OnDisable_NullRefs_DoesNotThrow                       ×1
    ///   OnDisable_Unregisters                                 ×1
    ///   Refresh_NullObjectives_HidesPanel                     ×1
    ///   Refresh_EmptyObjectives_HidesPanel                    ×1
    ///   Refresh_NullContainer_HidesPanel                      ×1
    ///   Refresh_ShowsPanel_WithObjectives                     ×1
    ///   Refresh_IncompleteObjective_LabelContainsProgress     ×1
    ///   Refresh_CompleteObjective_LabelIsDone                 ×1
    ///   Refresh_AllComplete_ShowsAllCompleteLabel             ×1
    ///   Refresh_NullAllCompleteLabel_NoThrow                  ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ObjectiveProgressListControllerTests
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

        private static InMatchObjectiveSO CreateObjectiveSO(int targetCount = 3,
                                                              string title = "Test Objective")
        {
            var so = ScriptableObject.CreateInstance<InMatchObjectiveSO>();
            SetField(so, "_targetCount",    targetCount);
            SetField(so, "_objectiveTitle", title);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ObjectiveProgressListController CreateController() =>
            new GameObject("ObjListCtrl_Test").AddComponent<ObjectiveProgressListController>();

        /// <summary>
        /// Creates a minimal row prefab with three Text children.
        /// GetComponentsInChildren returns them in the order they appear in hierarchy.
        /// </summary>
        private static GameObject CreateRowPrefab(Transform parent = null)
        {
            var row = new GameObject("RowPrefab");
            if (parent != null) row.transform.SetParent(parent);
            for (int i = 0; i < 3; i++)
            {
                var textGO = new GameObject("Text" + i);
                textGO.transform.SetParent(row.transform);
                textGO.AddComponent<Text>();
            }
            return row;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ObjectivesNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Objectives);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onObjectiveChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullObjectives_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel",      panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh(); // _objectives is null

            Assert.IsFalse(panel.activeSelf,
                "Panel should be hidden when _objectives is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_EmptyObjectives_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_objectives", new InMatchObjectiveSO[0]);
            SetField(ctrl, "_panel",      panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel should be hidden when objectives array is empty.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullContainer_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            var so    = CreateObjectiveSO();

            SetField(ctrl, "_objectives",     new[] { so });
            SetField(ctrl, "_listContainer",  (Transform)null);
            SetField(ctrl, "_panel",          panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel should be hidden when _listContainer is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ShowsPanel_WithObjectives()
        {
            var ctrl      = CreateController();
            var panel     = new GameObject("panel");
            panel.SetActive(false);
            var container = new GameObject("container");
            var so        = CreateObjectiveSO();

            SetField(ctrl, "_objectives",    new[] { so });
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_panel",         panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel should be shown when objectives are present and container is set.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_IncompleteObjective_LabelContainsProgress()
        {
            var ctrl      = CreateController();
            var container = new GameObject("container");
            var prefab    = CreateRowPrefab();
            var so        = CreateObjectiveSO(targetCount: 5);
            so.Increment(); // CurrentCount = 1

            SetField(ctrl, "_objectives",    new[] { so });
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            // Find the instantiated row (first child of container).
            var row   = container.transform.GetChild(0).gameObject;
            var texts = row.GetComponentsInChildren<Text>(includeInactive: true);

            Assert.IsTrue(texts.Length >= 2,
                "Row prefab should have at least 2 Text children.");
            StringAssert.Contains("1", texts[1].text,
                "Progress label should contain current count.");
            StringAssert.Contains("5", texts[1].text,
                "Progress label should contain target count.");
            StringAssert.DoesNotContain("DONE", texts[1].text,
                "Progress label should not say DONE for an incomplete objective.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_CompleteObjective_LabelIsDone()
        {
            var ctrl      = CreateController();
            var container = new GameObject("container");
            var prefab    = CreateRowPrefab();
            var so        = CreateObjectiveSO(targetCount: 1);
            so.Increment(); // completes

            SetField(ctrl, "_objectives",    new[] { so });
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            var row   = container.transform.GetChild(0).gameObject;
            var texts = row.GetComponentsInChildren<Text>(includeInactive: true);

            Assert.IsTrue(texts.Length >= 2,
                "Row prefab should have at least 2 Text children.");
            Assert.AreEqual("DONE", texts[1].text,
                "Progress label should be 'DONE' when objective is complete.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Refresh_AllComplete_ShowsAllCompleteLabel()
        {
            var ctrl         = CreateController();
            var container    = new GameObject("container");
            var prefab       = CreateRowPrefab();
            var labelGO      = new GameObject("allComplete");
            var allCompLabel = labelGO.AddComponent<Text>();
            labelGO.SetActive(false);
            var so           = CreateObjectiveSO(targetCount: 1);
            so.Increment(); // complete

            SetField(ctrl, "_objectives",       new[] { so });
            SetField(ctrl, "_listContainer",    container.transform);
            SetField(ctrl, "_rowPrefab",        prefab);
            SetField(ctrl, "_allCompleteLabel", allCompLabel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(labelGO.activeSelf,
                "_allCompleteLabel should be activated when all objectives are complete.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Refresh_NullAllCompleteLabel_NoThrow()
        {
            var ctrl      = CreateController();
            var container = new GameObject("container");
            var prefab    = CreateRowPrefab();
            var so        = CreateObjectiveSO(targetCount: 1);
            so.Increment(); // complete

            SetField(ctrl, "_objectives",       new[] { so });
            SetField(ctrl, "_listContainer",    container.transform);
            SetField(ctrl, "_rowPrefab",        prefab);
            SetField(ctrl, "_allCompleteLabel", (Text)null);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh should not throw when _allCompleteLabel is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(so);
        }
    }
}

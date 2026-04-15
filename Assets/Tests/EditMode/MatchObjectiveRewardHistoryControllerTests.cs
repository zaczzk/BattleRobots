using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T241: <see cref="MatchObjectiveRewardHistoryController"/>.
    ///
    /// MatchObjectiveRewardHistoryControllerTests (12):
    ///   FreshInstance_HistoryNull                               ×1
    ///   OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_NullRefs_DoesNotThrow                         ×1
    ///   OnDisable_Unregisters_Channel                           ×1
    ///   Refresh_NullListContainer_DoesNotThrow                  ×1
    ///   Refresh_NullRowPrefab_DoesNotThrow                      ×1
    ///   Refresh_NullHistory_ShowsEmptyLabel                     ×1
    ///   Refresh_EmptyHistory_ShowsEmptyLabel                    ×1
    ///   Refresh_SingleEntry_Completed_ShowsCompletedBadge       ×1
    ///   Refresh_SingleEntry_NotCompleted_ShowsFailedBadge       ×1
    ///   Refresh_SingleEntry_RewardText_Format                   ×1
    ///   Refresh_MultipleEntries_NewestFirst                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class MatchObjectiveRewardHistoryControllerTests
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

        private static MatchObjectiveRewardHistoryController CreateController() =>
            new GameObject("ObjRewardHistory_Test")
                .AddComponent<MatchObjectiveRewardHistoryController>();

        private static MatchObjectivePersistenceSO CreateHistory() =>
            ScriptableObject.CreateInstance<MatchObjectivePersistenceSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        /// <summary>
        /// Builds a minimal row prefab with N Text children (inactive by default so
        /// GetComponentsInChildren(includeInactive:true) can find them).
        /// </summary>
        private static GameObject BuildRowPrefab(int textCount)
        {
            var root = new GameObject("RowPrefab_Test");
            for (int i = 0; i < textCount; i++)
            {
                var child = new GameObject($"Text_{i}");
                child.transform.SetParent(root.transform);
                child.AddComponent<Text>();
            }
            return root;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_HistoryNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.History,
                "History must be null on a fresh MatchObjectiveRewardHistoryController instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var ctrl = CreateController();
            var evt  = CreateEvent();
            SetField(ctrl, "_onHistoryUpdated", evt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable, only external callbacks fire on _onHistoryUpdated.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullListContainer_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() with null _listContainer must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullRowPrefab_DoesNotThrow()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container_Test");
            SetField(ctrl, "_listContainer", container.transform);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() with null _rowPrefab must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
        }

        [Test]
        public void Refresh_NullHistory_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var container = new GameObject("Container_Test");
            var prefab    = BuildRowPrefab(3);
            var emptyGO   = new GameObject("Empty_Test");
            var emptyText = emptyGO.AddComponent<Text>();

            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            SetField(ctrl, "_emptyLabel",    emptyText);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(emptyGO.activeSelf,
                "_emptyLabel must be visible when history is null.");
            Assert.AreEqual(0, container.transform.childCount,
                "No rows must be created when history is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(emptyGO);
        }

        [Test]
        public void Refresh_EmptyHistory_ShowsEmptyLabel()
        {
            var ctrl      = CreateController();
            var history   = CreateHistory();   // freshly created → 0 entries
            var container = new GameObject("Container_Test");
            var prefab    = BuildRowPrefab(3);
            var emptyGO   = new GameObject("Empty_Test");
            var emptyText = emptyGO.AddComponent<Text>();

            SetField(ctrl, "_history",       history);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            SetField(ctrl, "_emptyLabel",    emptyText);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(emptyGO.activeSelf,
                "_emptyLabel must be visible when history is empty.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(emptyGO);
        }

        [Test]
        public void Refresh_SingleEntry_Completed_ShowsCompletedBadge()
        {
            var ctrl      = CreateController();
            var history   = CreateHistory();
            var container = new GameObject("Container_Test");
            var prefab    = BuildRowPrefab(3);

            history.Record("Kill 5 Robots", completed: true, reward: 50);
            SetField(ctrl, "_history",       history);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(1, container.transform.childCount,
                "One row must be created for one entry.");
            var texts = container.transform.GetChild(0)
                .GetComponentsInChildren<Text>(includeInactive: true);
            Assert.AreEqual("COMPLETED", texts[1].text,
                "Badge text must be 'COMPLETED' for a completed entry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_SingleEntry_NotCompleted_ShowsFailedBadge()
        {
            var ctrl      = CreateController();
            var history   = CreateHistory();
            var container = new GameObject("Container_Test");
            var prefab    = BuildRowPrefab(3);

            history.Record("Survive 3 Minutes", completed: false, reward: 0);
            SetField(ctrl, "_history",       history);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            var texts = container.transform.GetChild(0)
                .GetComponentsInChildren<Text>(includeInactive: true);
            Assert.AreEqual("FAILED", texts[1].text,
                "Badge text must be 'FAILED' for a non-completed entry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_SingleEntry_RewardText_Format()
        {
            var ctrl      = CreateController();
            var history   = CreateHistory();
            var container = new GameObject("Container_Test");
            var prefab    = BuildRowPrefab(3);

            history.Record("Objective A", completed: true, reward: 150);
            SetField(ctrl, "_history",       history);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            var texts = container.transform.GetChild(0)
                .GetComponentsInChildren<Text>(includeInactive: true);
            Assert.AreEqual("+150 credits", texts[2].text,
                "Reward text must be formatted as '+N credits' when reward > 0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Refresh_MultipleEntries_NewestFirst()
        {
            var ctrl      = CreateController();
            var history   = CreateHistory();
            var container = new GameObject("Container_Test");
            var prefab    = BuildRowPrefab(3);

            history.Record("First Objective",  completed: true,  reward: 10);
            history.Record("Second Objective", completed: false, reward: 0);
            history.Record("Third Objective",  completed: true,  reward: 30);
            SetField(ctrl, "_history",       history);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual(3, container.transform.childCount,
                "Three rows must be created for three entries.");

            // Newest-first: row 0 = "Third Objective", row 1 = "Second", row 2 = "First"
            var row0Texts = container.transform.GetChild(0)
                .GetComponentsInChildren<Text>(includeInactive: true);
            var row2Texts = container.transform.GetChild(2)
                .GetComponentsInChildren<Text>(includeInactive: true);

            Assert.AreEqual("Third Objective", row0Texts[0].text,
                "First displayed row must be the newest entry.");
            Assert.AreEqual("First Objective", row2Texts[0].text,
                "Last displayed row must be the oldest entry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
        }
    }
}

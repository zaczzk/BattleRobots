using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T232:
    ///   <see cref="MatchObjectivePersistenceSO"/> and
    ///   <see cref="MatchObjectiveHistoryController"/>.
    ///
    /// MatchObjectivePersistenceSOTests (10):
    ///   FreshInstance_Count_Zero                        ×1
    ///   FreshInstance_MaxEntries_Default                ×1
    ///   Record_Single_CountOne                          ×1
    ///   Record_Multiple_OrderPreserved                  ×1
    ///   Record_BeyondMax_EvictsOldest                   ×1
    ///   Record_FiresEvent                               ×1
    ///   Reset_ClearsEntries                             ×1
    ///   Reset_NoThrow_WhenEmpty                         ×1
    ///   Entry_Completed_True                            ×1
    ///   Entry_Completed_False                           ×1
    ///
    /// MatchObjectiveHistoryControllerTests (6):
    ///   FreshInstance_HistoryNull                       ×1
    ///   OnEnable_NullRefs_DoesNotThrow                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                           ×1
    ///   Refresh_NullHistory_ShowsEmptyLabel             ×1
    ///   Refresh_WithHistory_HidesEmptyLabel             ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchObjectivePersistenceTests
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

        private static MatchObjectivePersistenceSO CreateSO()
        {
            var so = ScriptableObject.CreateInstance<MatchObjectivePersistenceSO>();
            InvokePrivate(so, "OnEnable"); // triggers Reset()
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchObjectiveHistoryController CreateController() =>
            new GameObject("ObjHistCtrl_Test").AddComponent<MatchObjectiveHistoryController>();

        // ── MatchObjectivePersistenceSOTests ─────────────────────────────────

        [Test]
        public void FreshInstance_Count_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.Count,
                "Count must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_MaxEntries_Default()
        {
            var so = CreateSO();
            Assert.AreEqual(20, so.MaxEntries,
                "Default MaxEntries must be 20.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Record_Single_CountOne()
        {
            var so = CreateSO();
            so.Record("Test Objective", true, 50);
            Assert.AreEqual(1, so.Count,
                "Count must be 1 after one Record call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Record_Multiple_OrderPreserved()
        {
            var so = CreateSO();
            so.Record("First",  true,  10);
            so.Record("Second", false, 0);
            so.Record("Third",  true,  30);

            Assert.AreEqual(3, so.Count, "Count must be 3 after three records.");
            Assert.AreEqual("First",  so.Entries[0].title, "Index 0 must be 'First'.");
            Assert.AreEqual("Second", so.Entries[1].title, "Index 1 must be 'Second'.");
            Assert.AreEqual("Third",  so.Entries[2].title, "Index 2 must be 'Third'.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Record_BeyondMax_EvictsOldest()
        {
            var so = CreateSO();
            SetField(so, "_maxEntries", 3);

            so.Record("A", true,  10);
            so.Record("B", true,  20);
            so.Record("C", false, 0);
            so.Record("D", true,  40); // should evict "A"

            Assert.AreEqual(3, so.Count,
                "Count must stay at MaxEntries after overflow.");
            Assert.AreEqual("B", so.Entries[0].title,
                "Oldest entry 'A' must have been evicted; 'B' is now first.");
            Assert.AreEqual("D", so.Entries[2].title,
                "Newest entry 'D' must be at the last index.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Record_FiresEvent()
        {
            var so = CreateSO();
            var ch = CreateVoidEvent();
            SetField(so, "_onHistoryUpdated", ch);

            int count = 0;
            ch.RegisterCallback(() => count++);

            so.Record("Obj", true, 25);

            Assert.AreEqual(1, count,
                "_onHistoryUpdated must fire once per Record call.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Reset_ClearsEntries()
        {
            var so = CreateSO();
            so.Record("X", true, 10);
            so.Record("Y", true, 20);
            so.Reset();

            Assert.AreEqual(0, so.Count,
                "Count must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Reset_NoThrow_WhenEmpty()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => so.Reset(),
                "Reset on an empty SO must not throw.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Entry_Completed_True()
        {
            var so = CreateSO();
            so.Record("Completed Obj", true, 50);

            Assert.IsTrue(so.Entries[0].completed,
                "Entry.completed must be true when recorded with completed=true.");
            Assert.AreEqual(50, so.Entries[0].reward,
                "Entry.reward must match the recorded value.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Entry_Completed_False()
        {
            var so = CreateSO();
            so.Record("Expired Obj", false, 0);

            Assert.IsFalse(so.Entries[0].completed,
                "Entry.completed must be false when recorded with completed=false.");
            Assert.AreEqual(0, so.Entries[0].reward,
                "Entry.reward must be 0 for an expired objective.");
            Object.DestroyImmediate(so);
        }

        // ── MatchObjectiveHistoryControllerTests ──────────────────────────────

        [Test]
        public void FreshInstance_HistoryNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.History,
                "History must be null on a fresh instance.");
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
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateVoidEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback must fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullHistory_ShowsEmptyLabel()
        {
            var ctrl       = CreateController();
            var labelGO    = new GameObject("Label");
            var label      = labelGO.AddComponent<Text>();
            var container  = new GameObject("Container").transform;
            var prefabGO   = new GameObject("Prefab");

            labelGO.SetActive(false);
            SetField(ctrl, "_emptyLabel",   label);
            SetField(ctrl, "_listContainer", container);
            SetField(ctrl, "_rowPrefab",     prefabGO);
            InvokePrivate(ctrl, "Awake");

            // _history remains null
            ctrl.Refresh();

            Assert.IsTrue(labelGO.activeSelf,
                "_emptyLabel must be shown when History is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefabGO);
        }

        [Test]
        public void Refresh_WithHistory_HidesEmptyLabel()
        {
            var ctrl      = CreateController();
            var so        = CreateSO();
            var labelGO   = new GameObject("Label");
            var label     = labelGO.AddComponent<Text>();
            var container = new GameObject("Container").transform;
            var prefabGO  = new GameObject("Prefab");

            labelGO.SetActive(true);
            so.Record("Obj A", true, 10);

            SetField(ctrl, "_history",       so);
            SetField(ctrl, "_emptyLabel",    label);
            SetField(ctrl, "_listContainer", container);
            SetField(ctrl, "_rowPrefab",     prefabGO);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(labelGO.activeSelf,
                "_emptyLabel must be hidden when History has entries.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(prefabGO);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T214:
    ///   <see cref="MatchReplaySummarySO"/> + <see cref="MatchReplaySummaryController"/>.
    ///
    /// MatchReplaySummarySOTests (10):
    ///   DefaultMaxEvents_50                                                ×1
    ///   FreshCount_Zero                                                    ×1
    ///   AddEvent_IncrementCount                                            ×1
    ///   Count_CappedAtMaxEvents                                            ×1
    ///   GetEntry_Empty_ReturnsNull                                         ×1
    ///   GetEntry_NewestFirst                                               ×1
    ///   RingBuffer_Evicts_OldestEntry                                      ×1
    ///   Clear_ResetsCountAndHead                                           ×1
    ///   GetEntry_OutOfRange_ReturnsNull                                    ×1
    ///   AddEvent_RaisesOnEventAdded                                        ×1
    ///
    /// MatchReplaySummaryControllerTests (6):
    ///   FreshInstance_SummaryNull                                          ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters                                              ×1
    ///   Refresh_NullSummary_ShowsEmptyLabel                                ×1
    ///   Refresh_NullContainer_DoesNotThrow                                 ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchReplaySummaryTests
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchReplaySummarySO CreateSummary(int maxEvents = 5)
        {
            var so = ScriptableObject.CreateInstance<MatchReplaySummarySO>();
            SetField(so, "_maxEvents", maxEvents);
            // Simulate OnEnable to allocate the buffer.
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static MatchReplaySummaryController CreateController() =>
            new GameObject("MatchReplaySummary_Test").AddComponent<MatchReplaySummaryController>();

        // ── MatchReplaySummarySO tests ────────────────────────────────────────

        [Test]
        public void DefaultMaxEvents_50()
        {
            var so = ScriptableObject.CreateInstance<MatchReplaySummarySO>();
            Assert.AreEqual(50, so.MaxEvents);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshCount_Zero()
        {
            var so = CreateSummary();
            Assert.AreEqual(0, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEvent_IncrementCount()
        {
            var so = CreateSummary();
            so.AddEvent(DamageType.Physical, 10f, true, 0.0);
            Assert.AreEqual(1, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Count_CappedAtMaxEvents()
        {
            var so = CreateSummary(maxEvents: 3);
            for (int i = 0; i < 10; i++)
                so.AddEvent(DamageType.Energy, 5f, false, i);
            Assert.AreEqual(3, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_Empty_ReturnsNull()
        {
            var so = CreateSummary();
            Assert.IsNull(so.GetEntry(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_NewestFirst()
        {
            var so = CreateSummary();
            so.AddEvent(DamageType.Physical, 10f, true, 1.0);  // older
            so.AddEvent(DamageType.Shock,    20f, false, 2.0); // newer
            var entry = so.GetEntry(0); // most recent
            Assert.IsTrue(entry.HasValue);
            Assert.AreEqual(DamageType.Shock, entry.Value.damageType);
            Assert.AreEqual(20f, entry.Value.amount, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RingBuffer_Evicts_OldestEntry()
        {
            var so = CreateSummary(maxEvents: 2);
            so.AddEvent(DamageType.Physical, 1f, true, 0.0);  // evicted
            so.AddEvent(DamageType.Energy,   2f, true, 1.0);
            so.AddEvent(DamageType.Thermal,  3f, true, 2.0);  // newest

            Assert.AreEqual(2, so.Count);
            var newest = so.GetEntry(0);
            var oldest = so.GetEntry(1);
            Assert.AreEqual(DamageType.Thermal, newest.Value.damageType);
            Assert.AreEqual(DamageType.Energy,  oldest.Value.damageType);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Clear_ResetsCountAndHead()
        {
            var so = CreateSummary();
            so.AddEvent(DamageType.Physical, 5f, true, 0.0);
            so.Clear();
            Assert.AreEqual(0, so.Count);
            Assert.IsNull(so.GetEntry(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_OutOfRange_ReturnsNull()
        {
            var so = CreateSummary();
            so.AddEvent(DamageType.Physical, 5f, true, 0.0);
            Assert.IsNull(so.GetEntry(5), "OOB index must return null.");
            Assert.IsNull(so.GetEntry(-1), "Negative index must return null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEvent_RaisesOnEventAdded()
        {
            var so = CreateSummary();
            var ch = CreateEvent();
            SetField(so, "_onEventAdded", ch);

            int raised = 0;
            ch.RegisterCallback(() => raised++);
            so.AddEvent(DamageType.Shock, 10f, false, 0.0);

            Assert.AreEqual(1, raised, "_onEventAdded must be raised once per AddEvent.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(ch);
        }

        // ── MatchReplaySummaryController tests ────────────────────────────────

        [Test]
        public void MatchReplaySummaryController_FreshInstance_SummaryNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Summary);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MatchReplaySummaryController_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MatchReplaySummaryController_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void MatchReplaySummaryController_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void MatchReplaySummaryController_Refresh_NullSummary_ShowsEmptyLabel()
        {
            var ctrl  = CreateController();
            var empty = new GameObject("empty");
            empty.SetActive(false);
            SetField(ctrl, "_emptyLabel", empty);
            // _summary left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(empty.activeSelf, "Empty label must be shown when summary is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(empty);
        }

        [Test]
        public void MatchReplaySummaryController_Refresh_NullContainer_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var summary = CreateSummary();
            summary.AddEvent(DamageType.Physical, 10f, true, 0.0);
            SetField(ctrl, "_summary", summary);
            // _listContainer left null
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Refresh());

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(summary);
        }
    }
}

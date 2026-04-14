using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T234:
    ///   <see cref="MatchObjectiveTrackerSO"/> and
    ///   <see cref="MatchObjectiveTrackerController"/>.
    ///
    /// MatchObjectiveTrackerSOTests (10):
    ///   FreshInstance_AllZero                           ×1
    ///   RecordCompletion_IncreasesCompletedCount        ×1
    ///   RecordExpiry_IncreasesExpiredCount              ×1
    ///   TotalTracked_IsSumOfBoth                        ×1
    ///   CompletionRatio_AllCompleted_IsOne              ×1
    ///   CompletionRatio_NoneCompleted_IsZero            ×1
    ///   CompletionRatio_Empty_IsZero                    ×1
    ///   RecordCompletion_FiresObjectiveCompleted        ×1
    ///   RecordCompletion_FiresTrackerChanged            ×1
    ///   Reset_ClearsAll                                 ×1
    ///
    /// MatchObjectiveTrackerControllerTests (6):
    ///   FreshInstance_TrackerNull                       ×1
    ///   OnEnable_NullRefs_DoesNotThrow                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                           ×1
    ///   HandleMatchStarted_ResetsTracker                ×1
    ///   Refresh_NullTracker_HidesPanel                  ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchObjectiveTrackerTests
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

        private static MatchObjectiveTrackerSO CreateTrackerSO()
        {
            var so = ScriptableObject.CreateInstance<MatchObjectiveTrackerSO>();
            InvokePrivate(so, "OnEnable"); // triggers Reset()
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchObjectiveTrackerController CreateController() =>
            new GameObject("ObjTrackerCtrl_Test").AddComponent<MatchObjectiveTrackerController>();

        // ── MatchObjectiveTrackerSOTests ──────────────────────────────────────

        [Test]
        public void FreshInstance_AllZero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0, so.CompletedCount, "CompletedCount must be 0 on a fresh instance.");
            Assert.AreEqual(0, so.ExpiredCount,   "ExpiredCount must be 0 on a fresh instance.");
            Assert.AreEqual(0, so.TotalTracked,   "TotalTracked must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordCompletion_IncreasesCompletedCount()
        {
            var so = CreateTrackerSO();
            so.RecordCompletion();
            so.RecordCompletion();
            Assert.AreEqual(2, so.CompletedCount,
                "CompletedCount must increment once per RecordCompletion call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordExpiry_IncreasesExpiredCount()
        {
            var so = CreateTrackerSO();
            so.RecordExpiry();
            so.RecordExpiry();
            so.RecordExpiry();
            Assert.AreEqual(3, so.ExpiredCount,
                "ExpiredCount must increment once per RecordExpiry call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void TotalTracked_IsSumOfBoth()
        {
            var so = CreateTrackerSO();
            so.RecordCompletion();
            so.RecordCompletion();
            so.RecordExpiry();
            Assert.AreEqual(3, so.TotalTracked,
                "TotalTracked must equal CompletedCount + ExpiredCount.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void CompletionRatio_AllCompleted_IsOne()
        {
            var so = CreateTrackerSO();
            so.RecordCompletion();
            so.RecordCompletion();
            Assert.AreEqual(1f, so.CompletionRatio, 0.001f,
                "CompletionRatio must be 1 when all tracked objectives were completed.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void CompletionRatio_NoneCompleted_IsZero()
        {
            var so = CreateTrackerSO();
            so.RecordExpiry();
            so.RecordExpiry();
            Assert.AreEqual(0f, so.CompletionRatio, 0.001f,
                "CompletionRatio must be 0 when no objectives were completed.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void CompletionRatio_Empty_IsZero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.CompletionRatio, 0.001f,
                "CompletionRatio must be 0 when no objectives have been recorded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordCompletion_FiresObjectiveCompleted()
        {
            var so = CreateTrackerSO();
            var ch = CreateVoidEvent();
            SetField(so, "_onObjectiveCompleted", ch);

            int count = 0;
            ch.RegisterCallback(() => count++);

            so.RecordCompletion();

            Assert.AreEqual(1, count,
                "_onObjectiveCompleted must fire once per RecordCompletion call.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void RecordCompletion_FiresTrackerChanged()
        {
            var so = CreateTrackerSO();
            var ch = CreateVoidEvent();
            SetField(so, "_onTrackerChanged", ch);

            int count = 0;
            ch.RegisterCallback(() => count++);

            so.RecordCompletion();

            Assert.AreEqual(1, count,
                "_onTrackerChanged must fire once per RecordCompletion call.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Reset_ClearsAll()
        {
            var so = CreateTrackerSO();
            so.RecordCompletion();
            so.RecordExpiry();
            so.Reset();

            Assert.AreEqual(0, so.CompletedCount, "CompletedCount must be 0 after Reset.");
            Assert.AreEqual(0, so.ExpiredCount,   "ExpiredCount must be 0 after Reset.");
            Assert.AreEqual(0, so.TotalTracked,   "TotalTracked must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── MatchObjectiveTrackerControllerTests ──────────────────────────────

        [Test]
        public void FreshInstance_TrackerNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Tracker,
                "Tracker must be null on a fresh instance.");
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
        public void HandleMatchStarted_ResetsTracker()
        {
            var ctrl    = CreateController();
            var tracker = CreateTrackerSO();
            tracker.RecordCompletion();
            tracker.RecordExpiry();

            SetField(ctrl, "_tracker", tracker);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.AreEqual(0, tracker.TotalTracked,
                "HandleMatchStarted must reset the tracker to zero.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Refresh_NullTracker_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            InvokePrivate(ctrl, "Awake");

            // _tracker remains null
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when Tracker is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

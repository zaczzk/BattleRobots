using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T267:
    ///   <see cref="ZoneCaptureHistorySO"/> and
    ///   <see cref="ZoneCaptureHistoryController"/>.
    ///
    /// ZoneCaptureHistorySOTests (6):
    ///   FreshInstance_Count_IsZero                                      ×1
    ///   FreshInstance_MaxEntries_IsTen                                  ×1
    ///   AddEntry_Increments_Count                                       ×1
    ///   AddEntry_Count_Capped_AtMaxEntries                              ×1
    ///   GetEntry_ValidIndex_ReturnsNewestFirst                          ×1
    ///   GetEntry_OutOfRange_ReturnsDefault                              ×1
    ///
    /// ZoneCaptureHistoryControllerTests (6):
    ///   FreshInstance_HistorySO_Null                                    ×1
    ///   FreshInstance_IsMatchRunning_False                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_MatchChannels                             ×1
    ///   HandleMatchStarted_ClearsHistory                                ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCaptureHistoryTests
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

        private static ZoneCaptureHistorySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneCaptureHistorySO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneCaptureHistoryController CreateController() =>
            new GameObject("ZoneCaptureHistCtrl_Test")
                .AddComponent<ZoneCaptureHistoryController>();

        // ── ZoneCaptureHistorySO tests ─────────────────────────────────────────

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.Count,
                "A fresh ZoneCaptureHistorySO must have Count == 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_MaxEntries_IsTen()
        {
            var so = CreateSO();
            Assert.AreEqual(10, so.MaxEntries,
                "Default MaxEntries must be 10.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEntry_Increments_Count()
        {
            var so = CreateSO();
            so.AddEntry("ZoneA", 1f, true);
            Assert.AreEqual(1, so.Count,
                "Count must be 1 after one AddEntry call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEntry_Count_Capped_AtMaxEntries()
        {
            var so = CreateSO();
            for (int i = 0; i < 15; i++)
                so.AddEntry("Zone", i, i % 2 == 0);

            Assert.AreEqual(so.MaxEntries, so.Count,
                "Count must not exceed MaxEntries.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_ValidIndex_ReturnsNewestFirst()
        {
            var so = CreateSO();
            so.AddEntry("First", 1f, true);
            so.AddEntry("Second", 2f, false);

            var newest = so.GetEntry(0);
            Assert.AreEqual("Second", newest.zoneId,
                "GetEntry(0) must return the most recently added entry.");
            Assert.AreEqual(2f, newest.timestamp, 0.001f,
                "GetEntry(0).timestamp must match the second entry.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_OutOfRange_ReturnsDefault()
        {
            var so = CreateSO();
            so.AddEntry("Zone", 1f, true);

            var entry = so.GetEntry(5);
            Assert.AreEqual(default(ZoneCaptureHistoryEntry).zoneId, entry.zoneId,
                "Out-of-range GetEntry must return a default entry.");
            Object.DestroyImmediate(so);
        }

        // ── ZoneCaptureHistoryController tests ────────────────────────────────

        [Test]
        public void FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HistorySO,
                "HistorySO must be null on a fresh ZoneCaptureHistoryController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_IsMatchRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false on a fresh controller.");
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
        public void OnDisable_Unregisters_MatchChannels()
        {
            var ctrl       = CreateController();
            var historySO  = CreateSO();
            var evtStarted = CreateEvent();

            SetField(ctrl, "_historySO",      historySO);
            SetField(ctrl, "_onMatchStarted", evtStarted);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raising match-started after disable must NOT clear history.
            historySO.AddEntry("Zone", 0f, true);
            Assert.AreEqual(1, historySO.Count, "Pre-condition: history should have 1 entry.");

            evtStarted.Raise(); // should not call HandleMatchStarted
            Assert.AreEqual(1, historySO.Count,
                "After OnDisable, match-started event must not clear history.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(evtStarted);
        }

        [Test]
        public void HandleMatchStarted_ClearsHistory()
        {
            var ctrl      = CreateController();
            var historySO = CreateSO();

            SetField(ctrl, "_historySO", historySO);

            historySO.AddEntry("Zone", 1f, true);
            Assert.AreEqual(1, historySO.Count, "Pre-condition: history must have 1 entry.");

            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();

            Assert.AreEqual(0, historySO.Count,
                "HandleMatchStarted must clear the history SO.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(historySO);
        }
    }
}

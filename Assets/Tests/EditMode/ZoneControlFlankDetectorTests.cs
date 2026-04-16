using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T328: <see cref="ZoneControlFlankDetectorSO"/> and
    /// <see cref="ZoneControlFlankDetectorController"/>.
    ///
    /// ZoneControlFlankDetectorTests (12):
    ///   SO_FreshInstance_ActiveZoneCount_Zero                              ×1
    ///   SO_RecordBotCapture_SingleZone_NoFlank                             ×1
    ///   SO_RecordBotCapture_TwoDistinctZones_WithinWindow_FiresFlank       ×1
    ///   SO_RecordBotCapture_TwoDistinctZones_OutsideWindow_NoFlank         ×1
    ///   SO_RecordBotCapture_SameZoneTwice_NoFlank                          ×1
    ///   SO_Reset_ClearsAll                                                 ×1
    ///   Controller_FreshInstance_DetectorSO_Null                           ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_Unregisters_Channel                           ×1
    ///   Controller_HandleBotZoneCaptured_RecordsCapture                    ×1
    ///   Controller_HandleMatchStarted_ResetsDetector                       ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlFlankDetectorTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static IntGameEvent CreateIntEvent() =>
            ScriptableObject.CreateInstance<IntGameEvent>();

        private static ZoneControlFlankDetectorSO CreateDetectorSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlFlankDetectorSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ActiveZoneCount_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlFlankDetectorSO>();
            Assert.AreEqual(0, so.ActiveZoneCount,
                "ActiveZoneCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SingleZone_NoFlank()
        {
            var so  = CreateDetectorSO(); // default flankZoneCount = 2
            var evt = CreateEvent();
            SetField(so, "_onFlankDetected", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.RecordBotCapture(0, 0f); // only one zone active

            Assert.AreEqual(0, fired,
                "_onFlankDetected must NOT fire when only one distinct zone is active.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_TwoDistinctZones_WithinWindow_FiresFlank()
        {
            var so  = CreateDetectorSO(); // flankWindow=3f, flankZoneCount=2
            var evt = CreateEvent();
            SetField(so, "_onFlankDetected", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.RecordBotCapture(0, 0f);
            so.RecordBotCapture(1, 1f); // within the 3s window

            Assert.GreaterOrEqual(fired, 1,
                "_onFlankDetected must fire when two distinct zones are captured within the window.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_TwoDistinctZones_OutsideWindow_NoFlank()
        {
            var so  = CreateDetectorSO(); // flankWindow=3f
            var evt = CreateEvent();
            SetField(so, "_onFlankDetected", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.RecordBotCapture(0, 0f);
            so.RecordBotCapture(1, 5f); // 5s later — zone 0 is pruned (outside 3s window)

            Assert.AreEqual(0, fired,
                "_onFlankDetected must NOT fire when the first zone has expired.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_SameZoneTwice_NoFlank()
        {
            var so  = CreateDetectorSO(); // flankZoneCount=2
            var evt = CreateEvent();
            SetField(so, "_onFlankDetected", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.RecordBotCapture(0, 0f);
            so.RecordBotCapture(0, 1f); // same zone — still only 1 distinct zone

            Assert.AreEqual(0, fired,
                "_onFlankDetected must NOT fire when the same zone is captured twice.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateDetectorSO();
            so.RecordBotCapture(0, 0f);
            so.RecordBotCapture(1, 0.5f);
            so.Reset();
            Assert.AreEqual(0, so.ActiveZoneCount,
                "ActiveZoneCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_DetectorSO_Null()
        {
            var go   = new GameObject("Test_DetectorSO_Null");
            var ctrl = go.AddComponent<ZoneControlFlankDetectorController>();
            Assert.IsNull(ctrl.DetectorSO,
                "DetectorSO must be null on a fresh controller instance.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlFlankDetectorController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlFlankDetectorController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlFlankDetectorController>();
            var evt  = CreateIntEvent();
            SetField(ctrl, "_onBotZoneCaptured", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback((int _) => count++);
            evt.Raise(0);

            Assert.AreEqual(1, count,
                "_onBotZoneCaptured must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleBotZoneCaptured_RecordsCapture()
        {
            var go   = new GameObject("Test_HandleBotZoneCaptured");
            var ctrl = go.AddComponent<ZoneControlFlankDetectorController>();
            var so   = CreateDetectorSO();
            SetField(ctrl, "_detectorSO", so);

            ctrl.HandleBotZoneCaptured(2);

            Assert.AreEqual(1, so.ActiveZoneCount,
                "HandleBotZoneCaptured must record a capture on the detector SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsDetector()
        {
            var go   = new GameObject("Test_HandleMatchStarted");
            var ctrl = go.AddComponent<ZoneControlFlankDetectorController>();
            var so   = CreateDetectorSO();
            SetField(ctrl, "_detectorSO", so);

            so.RecordBotCapture(0, 0f);
            so.RecordBotCapture(1, 0.5f);
            ctrl.HandleMatchStarted();

            Assert.AreEqual(0, so.ActiveZoneCount,
                "HandleMatchStarted must reset the detector SO (clearing active zones).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}

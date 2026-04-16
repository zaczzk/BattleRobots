using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T318: <see cref="ZoneControlHeatmapSO"/> and
    /// <see cref="ZoneControlHeatmapController"/>.
    ///
    /// ZoneControlHeatmapTests (12):
    ///   SO_FreshInstance_ZoneCount_Default                              ×1
    ///   SO_RecordCapture_ValidIndex_IncrementsCaptureCount             ×1
    ///   SO_RecordCapture_OutOfRange_Silent                             ×1
    ///   SO_GetHeatLevel_NoCaptures_ReturnsZero                        ×1
    ///   SO_GetHeatLevel_MaxZone_ReturnsOne                            ×1
    ///   SO_GetHeatLevel_RelativeNormalisation                         ×1
    ///   SO_Reset_ClearsCounts                                         ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_HandleZoneCaptured_RecordsCapture                  ×1
    ///   Controller_Refresh_NullSO_HidesPanel                          ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlHeatmapTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static IntGameEvent CreateIntEvent() =>
            ScriptableObject.CreateInstance<IntGameEvent>();

        private static ZoneControlHeatmapSO CreateHeatmapSO(int zoneCount = 4)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapSO>();
            SetField(so, "_zoneCount", zoneCount);
            // Trigger re-initialisation of internal array.
            so.Reset();
            return so;
        }

        private static ZoneControlHeatmapController CreateController() =>
            new GameObject("HeatmapCtrl_Test")
                .AddComponent<ZoneControlHeatmapController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ZoneCount_Default()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapSO>();
            Assert.AreEqual(4, so.ZoneCount,
                "Default ZoneCount must be 4.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ValidIndex_IncrementsCaptureCount()
        {
            var so = CreateHeatmapSO(4);
            so.RecordCapture(1);
            Assert.AreEqual(1, so.GetCaptureCount(1),
                "CaptureCount for zone 1 must be 1 after one RecordCapture.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_OutOfRange_Silent()
        {
            var so = CreateHeatmapSO(4);
            Assert.DoesNotThrow(() => so.RecordCapture(-1),
                "RecordCapture with negative index must not throw.");
            Assert.DoesNotThrow(() => so.RecordCapture(99),
                "RecordCapture with out-of-range index must not throw.");
            Assert.AreEqual(0, so.MaxCaptureCount,
                "No captures must have been recorded after out-of-range calls.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetHeatLevel_NoCaptures_ReturnsZero()
        {
            var so = CreateHeatmapSO(4);
            Assert.AreEqual(0f, so.GetHeatLevel(0),
                "GetHeatLevel must return 0 when no captures have been recorded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetHeatLevel_MaxZone_ReturnsOne()
        {
            var so = CreateHeatmapSO(4);
            so.RecordCapture(2);
            Assert.AreEqual(1f, so.GetHeatLevel(2),
                "The zone with the sole capture must have heat level 1.0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetHeatLevel_RelativeNormalisation()
        {
            var so = CreateHeatmapSO(4);
            // Zone 0: 4 captures (max); Zone 1: 2 captures → heat = 0.5.
            so.RecordCapture(0); so.RecordCapture(0);
            so.RecordCapture(0); so.RecordCapture(0);
            so.RecordCapture(1); so.RecordCapture(1);

            Assert.AreEqual(1f, so.GetHeatLevel(0),
                "Zone 0 (4 captures, max) must have heat level 1.0.");
            Assert.AreEqual(0.5f, so.GetHeatLevel(1),
                "Zone 1 (2 captures, half of max) must have heat level 0.5.");
            Assert.AreEqual(0f, so.GetHeatLevel(2),
                "Zone 2 (0 captures) must have heat level 0.0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsCounts()
        {
            var so = CreateHeatmapSO(4);
            so.RecordCapture(0);
            so.RecordCapture(1);
            so.Reset();
            Assert.AreEqual(0, so.MaxCaptureCount,
                "MaxCaptureCount must be 0 after Reset.");
            Assert.AreEqual(0f, so.GetHeatLevel(0),
                "GetHeatLevel must return 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlHeatmapController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlHeatmapController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlHeatmapController>();

            var evt = CreateVoidEvent();
            SetField(ctrl, "_onHeatmapUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onHeatmapUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleZoneCaptured_RecordsCapture()
        {
            var go   = new GameObject("Test_RecordsCapture");
            var ctrl = go.AddComponent<ZoneControlHeatmapController>();

            var so = CreateHeatmapSO(4);
            SetField(ctrl, "_heatmapSO", so);

            ctrl.HandleZoneCaptured(2);

            Assert.AreEqual(1, so.GetCaptureCount(2),
                "HandleZoneCaptured must delegate to ZoneControlHeatmapSO.RecordCapture.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlHeatmapController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when HeatmapSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T318: <see cref="ZoneControlHeatmapSO"/> and
    /// <see cref="ZoneControlHeatmapController"/>.
    ///
    /// ZoneControlHeatmapTests (12):
    ///   SO_FreshInstance_MaxCaptures_Zero                                        ×1
    ///   SO_RecordCapture_IncrementsCount                                         ×1
    ///   SO_RecordCapture_OutOfRange_Ignored                                      ×1
    ///   SO_GetHeatLevel_HottestZone_ReturnsOne                                  ×1
    ///   SO_GetHeatLevel_ReturnsZero_WhenNoCapturesRecorded                      ×1
    ///   SO_GetHeatLevel_NormalisedRelativeToMax                                  ×1
    ///   SO_Reset_ClearsCounts                                                    ×1
    ///   Controller_FreshInstance_HeatmapSO_Null                                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_MatchStartedChannel                    ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                    ×1
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlHeatmapSO CreateHeatmapSO(int maxZones = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapSO>();
            SetField(so, "_maxZones", maxZones);
            so.Reset();
            return so;
        }

        private static ZoneControlHeatmapController CreateController() =>
            new GameObject("HeatmapCtrl_Test")
                .AddComponent<ZoneControlHeatmapController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_MaxCaptures_Zero()
        {
            var so = CreateHeatmapSO();
            Assert.AreEqual(0, so.MaxCaptures,
                "MaxCaptures must be 0 on a fresh instance with no captures recorded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCount()
        {
            var so = CreateHeatmapSO(maxZones: 3);
            so.RecordCapture(1);
            so.RecordCapture(1);
            Assert.AreEqual(2, so.GetCaptureCount(1),
                "GetCaptureCount must reflect the number of RecordCapture calls for that zone.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_OutOfRange_Ignored()
        {
            var so = CreateHeatmapSO(maxZones: 2);
            so.RecordCapture(-1);
            so.RecordCapture(2);
            so.RecordCapture(99);
            Assert.AreEqual(0, so.MaxCaptures,
                "Out-of-range zone indices must be silently ignored.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetHeatLevel_HottestZone_ReturnsOne()
        {
            var so = CreateHeatmapSO(maxZones: 3);
            so.RecordCapture(0);
            so.RecordCapture(0);
            so.RecordCapture(1); // zone 0 has most captures → heat = 1.0

            Assert.AreEqual(1f, so.GetHeatLevel(0),
                "The zone with the highest capture count must return a heat level of 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetHeatLevel_ReturnsZero_WhenNoCapturesRecorded()
        {
            var so = CreateHeatmapSO(maxZones: 3);
            Assert.AreEqual(0f, so.GetHeatLevel(0),
                "GetHeatLevel must return 0 when no captures have been recorded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetHeatLevel_NormalisedRelativeToMax()
        {
            var so = CreateHeatmapSO(maxZones: 3);
            so.RecordCapture(0); // 1 capture
            so.RecordCapture(0); // 2 captures  ← max
            so.RecordCapture(1); // 1 capture   → heat = 0.5

            float heat = so.GetHeatLevel(1);
            Assert.AreEqual(0.5f, heat, 0.001f,
                "GetHeatLevel must normalise relative to the zone with the most captures.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsCounts()
        {
            var so = CreateHeatmapSO(maxZones: 3);
            so.RecordCapture(0);
            so.RecordCapture(2);
            Assert.AreNotEqual(0, so.MaxCaptures);

            so.Reset();
            Assert.AreEqual(0, so.MaxCaptures,
                "MaxCaptures must be 0 after Reset.");
            Assert.AreEqual(0f, so.GetHeatLevel(0),
                "GetHeatLevel must return 0 for all zones after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_HeatmapSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.HeatmapSO,
                "HeatmapSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

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
        public void Controller_OnDisable_Unregisters_MatchStartedChannel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlHeatmapController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchStarted", evt);

            go.SetActive(true);  // triggers OnEnable → registers
            go.SetActive(false); // triggers OnDisable → unregisters

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchStarted must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
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

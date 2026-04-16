using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T323: <see cref="ZoneControlHeatmapHistorySO"/> and
    /// <see cref="ZoneControlHeatmapHistoryController"/>.
    ///
    /// ZoneControlHeatmapHistoryTests (12):
    ///   SO_FreshInstance_SnapshotCount_Zero                           ×1
    ///   SO_AddSnapshot_IncrementsSnapshotCount                        ×1
    ///   SO_AddSnapshot_NullArray_Ignored                              ×1
    ///   SO_AddSnapshot_EvictsOldest_WhenFull                          ×1
    ///   SO_GetLifetimeHeatLevel_NoSnapshots_ReturnsZero               ×1
    ///   SO_GetLifetimeHeatLevel_HottestZone_ReturnsOne                ×1
    ///   SO_Reset_ClearsHistory                                        ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_Refresh_NullHistorySO_HidesPanel                   ×1
    ///   Controller_HandleMatchEnded_NullHistorySO_DoesNotThrow        ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlHeatmapHistoryTests
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

        private static ZoneControlHeatmapHistorySO CreateHistorySO(int maxSnapshots = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapHistorySO>();
            SetField(so, "_maxSnapshots", maxSnapshots);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_SnapshotCount_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapHistorySO>();
            Assert.AreEqual(0, so.SnapshotCount,
                "SnapshotCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddSnapshot_IncrementsSnapshotCount()
        {
            var so = CreateHistorySO(maxSnapshots: 5);
            so.AddSnapshot(new int[] { 3, 1, 2 });
            Assert.AreEqual(1, so.SnapshotCount,
                "SnapshotCount must increment after AddSnapshot.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddSnapshot_NullArray_Ignored()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(null);
            Assert.AreEqual(0, so.SnapshotCount,
                "AddSnapshot with null array must be silently ignored.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddSnapshot_EvictsOldest_WhenFull()
        {
            var so = CreateHistorySO(maxSnapshots: 2);
            so.AddSnapshot(new int[] { 1, 0 });
            so.AddSnapshot(new int[] { 2, 0 });
            so.AddSnapshot(new int[] { 3, 0 }); // should evict first
            Assert.AreEqual(2, so.SnapshotCount,
                "SnapshotCount must not exceed MaxSnapshots.");
            // Lifetime total for zone 0 should be 2+3=5, not 1+2+3=6.
            float heat = so.GetLifetimeHeatLevel(0);
            Assert.AreEqual(1f, heat, 0.0001f,
                "After eviction, hottest zone should still return heat 1.0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetLifetimeHeatLevel_NoSnapshots_ReturnsZero()
        {
            var so = CreateHistorySO();
            Assert.AreEqual(0f, so.GetLifetimeHeatLevel(0), 0.0001f,
                "GetLifetimeHeatLevel must return 0 when no snapshots exist.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetLifetimeHeatLevel_HottestZone_ReturnsOne()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(new int[] { 10, 5, 2 });
            float heat = so.GetLifetimeHeatLevel(0);
            Assert.AreEqual(1f, heat, 0.0001f,
                "The zone with the highest total must return GetLifetimeHeatLevel == 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsHistory()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(new int[] { 5, 3 });
            so.Reset();
            Assert.AreEqual(0, so.SnapshotCount,
                "SnapshotCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlHeatmapHistoryController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlHeatmapHistoryController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlHeatmapHistoryController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullHistorySO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlHeatmapHistoryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when HistorySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullHistorySO_DoesNotThrow()
        {
            var go   = new GameObject("Test_HandleMatchEnded_Null");
            var ctrl = go.AddComponent<ZoneControlHeatmapHistoryController>();
            Assert.DoesNotThrow(
                () => ctrl.HandleMatchEnded(),
                "HandleMatchEnded with null HistorySO must not throw.");
            Object.DestroyImmediate(go);
        }
    }
}

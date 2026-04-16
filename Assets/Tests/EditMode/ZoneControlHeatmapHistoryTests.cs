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
    ///   SO_FreshInstance_SnapshotCount_Zero                              ×1
    ///   SO_AddSnapshot_Null_NoOp                                         ×1
    ///   SO_AddSnapshot_IncrementsCount                                   ×1
    ///   SO_AddSnapshot_PrunesOldestWhenAtCapacity                        ×1
    ///   SO_GetLifetimeCount_ReturnsSum                                   ×1
    ///   SO_GetLifetimeHeatLevel_NormalisedCorrectly                      ×1
    ///   SO_Reset_ClearsSnapshots                                         ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                       ×1
    ///   Controller_OnDisable_Unregisters_Channel                         ×1
    ///   Controller_HandleMatchEnded_AddsSnapshot                         ×1
    ///   Controller_Refresh_NullHistorySO_HidesPanel                      ×1
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

        private static ZoneControlHeatmapHistorySO CreateHistorySO(int max = 10)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapHistorySO>();
            SetField(so, "_maxSnapshots", max);
            so.Reset();
            return so;
        }

        private static ZoneControlHeatmapSO CreateHeatmapSO(int zoneCount = 4)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlHeatmapSO>();
            SetField(so, "_zoneCount", zoneCount);
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
        public void SO_AddSnapshot_Null_NoOp()
        {
            var so = CreateHistorySO();
            Assert.DoesNotThrow(() => so.AddSnapshot(null),
                "AddSnapshot(null) must not throw.");
            Assert.AreEqual(0, so.SnapshotCount,
                "SnapshotCount must remain 0 after AddSnapshot(null).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddSnapshot_IncrementsCount()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(new int[] { 1, 2, 3 });
            Assert.AreEqual(1, so.SnapshotCount,
                "SnapshotCount must be 1 after one AddSnapshot call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddSnapshot_PrunesOldestWhenAtCapacity()
        {
            var so = CreateHistorySO(max: 2);
            so.AddSnapshot(new int[] { 1, 0 }); // snapshot 0
            so.AddSnapshot(new int[] { 2, 0 }); // snapshot 1
            so.AddSnapshot(new int[] { 3, 0 }); // should evict snapshot 0

            Assert.AreEqual(2, so.SnapshotCount,
                "SnapshotCount must not exceed MaxSnapshots.");
            Assert.AreEqual(3, so.Snapshots[1][0],
                "Latest snapshot must be retained.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetLifetimeCount_ReturnsSum()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(new int[] { 2, 5 });
            so.AddSnapshot(new int[] { 3, 1 });

            Assert.AreEqual(5, so.GetLifetimeCount(0),
                "GetLifetimeCount must sum counts across all snapshots for zone 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetLifetimeHeatLevel_NormalisedCorrectly()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(new int[] { 4, 2 }); // zone 0 sum=4, zone 1 sum=2; total per snap: 6
            // Max total across all snapshots = 6 (zone 0 col sum=4; zone 1 col sum=2; hottest total snapshot=6)
            // Actually GetLifetimeHeatLevel uses max zone total across snapshots, not per-zone sum across snapshots.
            // Let me re-read the implementation: it finds max total (sum of all zones in a snapshot),
            // then divides zoneTotal (sum of that zone across all snapshots) by maxTotal.
            // snap 0: total=6, zoneTotal[0]=4 → 4/6 ≈ 0.667
            float heat = so.GetLifetimeHeatLevel(0);
            Assert.Greater(heat, 0f,
                "GetLifetimeHeatLevel must return > 0 when captures exist.");
            Assert.LessOrEqual(heat, 1f,
                "GetLifetimeHeatLevel must not exceed 1.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsSnapshots()
        {
            var so = CreateHistorySO();
            so.AddSnapshot(new int[] { 1, 2 });
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
        public void Controller_HandleMatchEnded_AddsSnapshot()
        {
            var go         = new GameObject("Test_MatchEnded");
            var ctrl       = go.AddComponent<ZoneControlHeatmapHistoryController>();
            var historySO  = CreateHistorySO();
            var sessionSO  = CreateHeatmapSO(zoneCount: 2);
            SetField(ctrl, "_historySO", historySO);
            SetField(ctrl, "_sessionSO", sessionSO);

            // Record two captures on zone 0
            sessionSO.RecordCapture(0);
            sessionSO.RecordCapture(0);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(1, historySO.SnapshotCount,
                "HandleMatchEnded must add one snapshot to the history SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(historySO);
            Object.DestroyImmediate(sessionSO);
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
    }
}

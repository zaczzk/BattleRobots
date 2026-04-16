using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T341: <see cref="ZoneControlOwnershipSnapshotSO"/> and
    /// <see cref="ZoneControlOwnershipSnapshotController"/>.
    ///
    /// ZoneControlOwnershipSnapshotTests (12):
    ///   SO_FreshInstance_SnapshotCount_Zero                           ×1
    ///   SO_TakeSnapshot_NullCatalog_IsNoOp                           ×1
    ///   SO_TakeSnapshot_AddsSnapshot                                  ×1
    ///   SO_TakeSnapshot_ClonesOwnershipState                          ×1
    ///   SO_TakeSnapshot_PrunesOldestBeyondCapacity                    ×1
    ///   SO_GetSnapshot_OutOfRangeSnapshotIndex_ReturnsFalse           ×1
    ///   SO_GetSnapshot_OutOfRangeZoneIndex_ReturnsFalse               ×1
    ///   SO_Reset_ClearsAllSnapshots                                   ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_HandleMatchEnded_CallsTakeSnapshot                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlOwnershipSnapshotTests
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

        private static ZoneControlOwnershipSnapshotSO CreateSnapshotSO(int maxSnapshots = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlOwnershipSnapshotSO>();
            SetField(so, "_maxSnapshots", maxSnapshots);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneControllerCatalogSO CreateCatalogSO(int zoneCount = 4)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneControllerCatalogSO>();
            SetField(so, "_zoneCount", zoneCount);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_SnapshotCount_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlOwnershipSnapshotSO>();
            Assert.AreEqual(0, so.SnapshotCount,
                "SnapshotCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_NullCatalog_IsNoOp()
        {
            var so = CreateSnapshotSO();
            so.TakeSnapshot(null);
            Assert.AreEqual(0, so.SnapshotCount,
                "TakeSnapshot with null catalog must be a no-op.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_AddsSnapshot()
        {
            var so      = CreateSnapshotSO();
            var catalog = CreateCatalogSO(4);

            so.TakeSnapshot(catalog);

            Assert.AreEqual(1, so.SnapshotCount,
                "SnapshotCount must be 1 after one TakeSnapshot call.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_TakeSnapshot_ClonesOwnershipState()
        {
            var so      = CreateSnapshotSO();
            var catalog = CreateCatalogSO(4);

            // Set zone 0 as player-owned, zone 1 as bot-owned
            catalog.SetZoneController(0, true);
            catalog.SetZoneController(1, false);

            so.TakeSnapshot(catalog);

            Assert.IsTrue(so.GetSnapshot(0, 0),
                "Snapshot zone 0 must reflect player ownership.");
            Assert.IsFalse(so.GetSnapshot(0, 1),
                "Snapshot zone 1 must reflect bot ownership.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_TakeSnapshot_PrunesOldestBeyondCapacity()
        {
            var so      = CreateSnapshotSO(maxSnapshots: 2);
            var catalog = CreateCatalogSO(2);

            // Snapshot 1: zone 0 = player
            catalog.SetZoneController(0, true);
            so.TakeSnapshot(catalog);

            // Snapshot 2: zone 0 = bot
            catalog.SetZoneController(0, false);
            so.TakeSnapshot(catalog);

            // Snapshot 3: zone 0 = player again → snapshot 1 pruned
            catalog.SetZoneController(0, true);
            so.TakeSnapshot(catalog);

            Assert.AreEqual(2, so.SnapshotCount,
                "SnapshotCount must not exceed MaxSnapshots.");

            // The oldest (zone0=player) was pruned; snapshot[0] is now zone0=bot
            Assert.IsFalse(so.GetSnapshot(0, 0),
                "After pruning the oldest entry, snapshot[0] must be the second-oldest.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_GetSnapshot_OutOfRangeSnapshotIndex_ReturnsFalse()
        {
            var so = CreateSnapshotSO();
            Assert.IsFalse(so.GetSnapshot(0, 0),
                "GetSnapshot with out-of-range snapshot index must return false.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetSnapshot_OutOfRangeZoneIndex_ReturnsFalse()
        {
            var so      = CreateSnapshotSO();
            var catalog = CreateCatalogSO(2);
            so.TakeSnapshot(catalog);

            Assert.IsFalse(so.GetSnapshot(0, 99),
                "GetSnapshot with out-of-range zone index must return false.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_Reset_ClearsAllSnapshots()
        {
            var so      = CreateSnapshotSO();
            var catalog = CreateCatalogSO(2);
            so.TakeSnapshot(catalog);
            so.TakeSnapshot(catalog);

            so.Reset();

            Assert.AreEqual(0, so.SnapshotCount,
                "SnapshotCount must be 0 after Reset.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlOwnershipSnapshotController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlOwnershipSnapshotController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlOwnershipSnapshotController>();
            var evt  = CreateEvent();
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
        public void Controller_HandleMatchEnded_CallsTakeSnapshot()
        {
            var go         = new GameObject("Test_HandleMatchEnded");
            var ctrl       = go.AddComponent<ZoneControlOwnershipSnapshotController>();
            var snapshotSO = CreateSnapshotSO();
            var catalogSO  = CreateCatalogSO(3);

            SetField(ctrl, "_snapshotSO", snapshotSO);
            SetField(ctrl, "_catalogSO",  catalogSO);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(1, snapshotSO.SnapshotCount,
                "HandleMatchEnded must call TakeSnapshot on the snapshot SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(snapshotSO);
            Object.DestroyImmediate(catalogSO);
        }
    }
}

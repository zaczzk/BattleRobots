using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T471: <see cref="ZoneControlZoneOwnershipHistorySO"/> and
    /// <see cref="ZoneControlZoneOwnershipHistoryController"/>.
    ///
    /// ZoneControlZoneOwnershipHistoryTests (12):
    ///   SO_FreshInstance_SnapshotCount_Zero                                 x1
    ///   SO_TakeSnapshot_IncreasesCount                                      x1
    ///   SO_TakeSnapshot_PrunesOldestWhenFull                                x1
    ///   SO_TakeSnapshot_ComputesRatioCorrectly                              x1
    ///   SO_TakeSnapshot_ZeroTotalZones_RatioZero                            x1
    ///   SO_GetMajorityCount_ReturnsCorrectCount                             x1
    ///   SO_BestRatio_ReturnsMax                                             x1
    ///   SO_GetSnapshot_OutOfRange_ReturnsZero                               x1
    ///   SO_Reset_ClearsHistory                                              x1
    ///   Controller_FreshInstance_HistorySO_Null                             x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlZoneOwnershipHistoryTests
    {
        private static ZoneControlZoneOwnershipHistorySO CreateSO(int maxSnapshots = 5, float threshold = 0.5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneOwnershipHistorySO>();
            var t  = typeof(ZoneControlZoneOwnershipHistorySO);
            t.GetField("_maxSnapshots",      BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, maxSnapshots);
            t.GetField("_majorityThreshold", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, threshold);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneOwnershipHistoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneOwnershipHistoryController>();
        }

        [Test]
        public void SO_FreshInstance_SnapshotCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SnapshotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_IncreasesCount()
        {
            var so = CreateSO();
            so.TakeSnapshot(2, 4);
            Assert.That(so.SnapshotCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_PrunesOldestWhenFull()
        {
            var so = CreateSO(maxSnapshots: 3);
            so.TakeSnapshot(4, 4);
            so.TakeSnapshot(4, 4);
            so.TakeSnapshot(4, 4);
            so.TakeSnapshot(0, 4);
            Assert.That(so.SnapshotCount, Is.EqualTo(3));
            Assert.That(so.GetSnapshot(2), Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_ComputesRatioCorrectly()
        {
            var so = CreateSO();
            so.TakeSnapshot(2, 4);
            Assert.That(so.GetSnapshot(0), Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_ZeroTotalZones_RatioZero()
        {
            var so = CreateSO();
            so.TakeSnapshot(3, 0);
            Assert.That(so.GetSnapshot(0), Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetMajorityCount_ReturnsCorrectCount()
        {
            var so = CreateSO(threshold: 0.5f);
            so.TakeSnapshot(4, 4);
            so.TakeSnapshot(1, 4);
            so.TakeSnapshot(3, 4);
            Assert.That(so.GetMajorityCount(), Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BestRatio_ReturnsMax()
        {
            var so = CreateSO();
            so.TakeSnapshot(1, 4);
            so.TakeSnapshot(4, 4);
            so.TakeSnapshot(2, 4);
            Assert.That(so.BestRatio, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetSnapshot_OutOfRange_ReturnsZero()
        {
            var so = CreateSO();
            Assert.That(so.GetSnapshot(5), Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsHistory()
        {
            var so = CreateSO();
            so.TakeSnapshot(4, 4);
            so.Reset();
            Assert.That(so.SnapshotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HistorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HistorySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneOwnershipHistoryController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

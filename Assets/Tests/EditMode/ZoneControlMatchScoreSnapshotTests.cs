using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T465: <see cref="ZoneControlMatchScoreSnapshotSO"/> and
    /// <see cref="ZoneControlMatchScoreSnapshotController"/>.
    ///
    /// ZoneControlMatchScoreSnapshotTests (12):
    ///   SO_FreshInstance_SnapshotCount_Zero                                          x1
    ///   SO_FreshInstance_BestScore_Zero                                              x1
    ///   SO_TakeSnapshot_IncrementsCount                                              x1
    ///   SO_TakeSnapshot_StoresScore                                                  x1
    ///   SO_TakeSnapshot_MultipleScores_BestScoreIsMax                               x1
    ///   SO_TakeSnapshot_ExceedsCapacity_PrunesOldest                                x1
    ///   SO_GetSnapshot_ValidIndex_ReturnsScore                                       x1
    ///   SO_GetSnapshot_OutOfBounds_ReturnsZero                                       x1
    ///   SO_BestScore_SingleSnapshot_ReturnsThatScore                                 x1
    ///   SO_Reset_ClearsAll                                                           x1
    ///   Controller_FreshInstance_SnapshotSO_Null                                     x1
    ///   Controller_Refresh_NullSO_HidesPanel                                         x1
    /// </summary>
    public sealed class ZoneControlMatchScoreSnapshotTests
    {
        private static ZoneControlMatchScoreSnapshotSO CreateSO(int maxSnapshots = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchScoreSnapshotSO>();
            typeof(ZoneControlMatchScoreSnapshotSO)
                .GetField("_maxSnapshots", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxSnapshots);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchScoreSnapshotController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchScoreSnapshotController>();
        }

        [Test]
        public void SO_FreshInstance_SnapshotCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SnapshotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BestScore_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BestScore, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_IncrementsCount()
        {
            var so = CreateSO();
            so.TakeSnapshot(100);
            Assert.That(so.SnapshotCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_StoresScore()
        {
            var so = CreateSO();
            so.TakeSnapshot(250);
            Assert.That(so.GetSnapshot(0), Is.EqualTo(250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_MultipleScores_BestScoreIsMax()
        {
            var so = CreateSO();
            so.TakeSnapshot(100);
            so.TakeSnapshot(300);
            so.TakeSnapshot(200);
            Assert.That(so.BestScore, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TakeSnapshot_ExceedsCapacity_PrunesOldest()
        {
            var so = CreateSO(maxSnapshots: 3);
            so.TakeSnapshot(10);
            so.TakeSnapshot(20);
            so.TakeSnapshot(30);
            so.TakeSnapshot(40);
            Assert.That(so.SnapshotCount, Is.EqualTo(3));
            Assert.That(so.GetSnapshot(0), Is.EqualTo(20));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetSnapshot_ValidIndex_ReturnsScore()
        {
            var so = CreateSO();
            so.TakeSnapshot(500);
            so.TakeSnapshot(750);
            Assert.That(so.GetSnapshot(1), Is.EqualTo(750));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetSnapshot_OutOfBounds_ReturnsZero()
        {
            var so = CreateSO();
            Assert.That(so.GetSnapshot(0),  Is.EqualTo(0));
            Assert.That(so.GetSnapshot(-1), Is.EqualTo(0));
            Assert.That(so.GetSnapshot(99), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BestScore_SingleSnapshot_ReturnsThatScore()
        {
            var so = CreateSO();
            so.TakeSnapshot(123);
            Assert.That(so.BestScore, Is.EqualTo(123));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.TakeSnapshot(100);
            so.TakeSnapshot(200);
            so.Reset();
            Assert.That(so.SnapshotCount, Is.EqualTo(0));
            Assert.That(so.BestScore,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SnapshotSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SnapshotSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchScoreSnapshotController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T403: <see cref="ZoneControlCaptureMilestoneSO"/> and
    /// <see cref="ZoneControlCaptureMilestoneController"/>.
    ///
    /// ZoneControlCaptureMilestoneTests (12):
    ///   SO_FreshInstance_CaptureCount_Zero               x1
    ///   SO_FreshInstance_MilestonesReached_Zero          x1
    ///   SO_RecordCapture_IncrementsCaptureCount          x1
    ///   SO_RecordCapture_ReachesMilestone                x1
    ///   SO_RecordCapture_MultiMilestoneSafe              x1
    ///   SO_RecordCapture_AccumulatesTotalBonus           x1
    ///   SO_Reset_ClearsAll                               x1
    ///   SO_NextMilestoneTarget_ReturnsMinus1WhenAllDone  x1
    ///   SO_NextMilestoneTarget_ReturnsCurrentTarget      x1
    ///   Controller_FreshInstance_MilestoneSO_Null        x1
    ///   Controller_Refresh_NullSO_HidesPanel             x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow        x1
    /// </summary>
    public sealed class ZoneControlCaptureMilestoneTests
    {
        private static ZoneControlCaptureMilestoneSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureMilestoneSO>();

        private static ZoneControlCaptureMilestoneController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMilestoneController>();
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MilestonesReached_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MilestonesReached, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptureCount()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ReachesMilestone()
        {
            var so = CreateSO();
            // Default first milestone is 5
            for (int i = 0; i < 5; i++)
                so.RecordCapture();
            Assert.That(so.MilestonesReached, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_MultiMilestoneSafe()
        {
            var so = CreateSO();
            // Default milestones: {5, 10, 20, 35, 50}; 10 captures crosses first two
            for (int i = 0; i < 10; i++)
                so.RecordCapture();
            Assert.That(so.MilestonesReached, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            for (int i = 0; i < 5; i++)
                so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BonusPerMilestone));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < 5; i++)
                so.RecordCapture();
            so.Reset();
            Assert.That(so.CaptureCount,      Is.EqualTo(0));
            Assert.That(so.MilestonesReached, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextMilestoneTarget_ReturnsMinus1WhenAllDone()
        {
            var so = CreateSO();
            typeof(ZoneControlCaptureMilestoneSO)
                .GetField("_milestones", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, new int[] { 1 });
            so.RecordCapture();
            Assert.That(so.NextMilestoneTarget, Is.EqualTo(-1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextMilestoneTarget_ReturnsCurrentTarget()
        {
            var so = CreateSO();
            // Default first milestone is 5
            Assert.That(so.NextMilestoneTarget, Is.EqualTo(5));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MilestoneSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MilestoneSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureMilestoneController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() =>
            {
                ctrl.enabled = false;
                ctrl.enabled = true;
            });
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}

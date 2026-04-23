using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRigTests
    {
        private static ZoneControlCaptureRigSO CreateSO(
            int rigCellsNeeded   = 8,
            int absorbPerBot     = 2,
            int bonusPerDistribute = 3190)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRigSO>();
            typeof(ZoneControlCaptureRigSO)
                .GetField("_rigCellsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rigCellsNeeded);
            typeof(ZoneControlCaptureRigSO)
                .GetField("_absorbPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, absorbPerBot);
            typeof(ZoneControlCaptureRigSO)
                .GetField("_bonusPerDistribute", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDistribute);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRigController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRigController>();
        }

        [Test]
        public void SO_FreshInstance_RigCells_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RigCells, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DistributeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DistributeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRigCells()
        {
            var so = CreateSO(rigCellsNeeded: 8);
            so.RecordPlayerCapture();
            Assert.That(so.RigCells, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(rigCellsNeeded: 3, bonusPerDistribute: 3190);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(3190));
            Assert.That(so.DistributeCount,  Is.EqualTo(1));
            Assert.That(so.RigCells,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(rigCellsNeeded: 8);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesRigCells()
        {
            var so = CreateSO(rigCellsNeeded: 8, absorbPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RigCells, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(rigCellsNeeded: 8, absorbPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RigCells, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RigCellProgress_Clamped()
        {
            var so = CreateSO(rigCellsNeeded: 8);
            so.RecordPlayerCapture();
            Assert.That(so.RigCellProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDistributed_FiresEvent()
        {
            var so    = CreateSO(rigCellsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRigSO)
                .GetField("_onDistributed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(rigCellsNeeded: 2, bonusPerDistribute: 3190);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RigCells,          Is.EqualTo(0));
            Assert.That(so.DistributeCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDistributions_Accumulate()
        {
            var so = CreateSO(rigCellsNeeded: 2, bonusPerDistribute: 3190);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DistributeCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6380));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RigSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RigSO, Is.Null);
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
            typeof(ZoneControlCaptureRigController)
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

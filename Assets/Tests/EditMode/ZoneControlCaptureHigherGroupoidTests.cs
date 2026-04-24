using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHigherGroupoidTests
    {
        private static ZoneControlCaptureHigherGroupoidSO CreateSO(
            int cellsNeeded   = 5,
            int breakPerBot   = 1,
            int bonusPerInvert = 3640)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHigherGroupoidSO>();
            typeof(ZoneControlCaptureHigherGroupoidSO)
                .GetField("_cellsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cellsNeeded);
            typeof(ZoneControlCaptureHigherGroupoidSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureHigherGroupoidSO)
                .GetField("_bonusPerInvert", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInvert);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHigherGroupoidController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHigherGroupoidController>();
        }

        [Test]
        public void SO_FreshInstance_Cells_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cells, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InvertCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InvertCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCells()
        {
            var so = CreateSO(cellsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Cells, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cellsNeeded: 3, bonusPerInvert: 3640);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3640));
            Assert.That(so.InvertCount,  Is.EqualTo(1));
            Assert.That(so.Cells,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cellsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksCells()
        {
            var so = CreateSO(cellsNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cells, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cellsNeeded: 5, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cells, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CellProgress_Clamped()
        {
            var so = CreateSO(cellsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CellProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHigherGroupoidInverted_FiresEvent()
        {
            var so    = CreateSO(cellsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHigherGroupoidSO)
                .GetField("_onHigherGroupoidInverted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cellsNeeded: 2, bonusPerInvert: 3640);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cells,             Is.EqualTo(0));
            Assert.That(so.InvertCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInversions_Accumulate()
        {
            var so = CreateSO(cellsNeeded: 2, bonusPerInvert: 3640);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InvertCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7280));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HigherGroupoidSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HigherGroupoidSO, Is.Null);
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
            typeof(ZoneControlCaptureHigherGroupoidController)
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

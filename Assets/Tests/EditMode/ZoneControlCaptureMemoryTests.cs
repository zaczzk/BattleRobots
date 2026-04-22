using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMemoryTests
    {
        private static ZoneControlCaptureMemorySO CreateSO(
            int cellsNeeded   = 5,
            int corruptPerBot = 1,
            int bonusPerFlush = 1825)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMemorySO>();
            typeof(ZoneControlCaptureMemorySO)
                .GetField("_cellsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cellsNeeded);
            typeof(ZoneControlCaptureMemorySO)
                .GetField("_corruptPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, corruptPerBot);
            typeof(ZoneControlCaptureMemorySO)
                .GetField("_bonusPerFlush", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFlush);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMemoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMemoryController>();
        }

        [Test]
        public void SO_FreshInstance_Cells_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cells, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FlushCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlushCount, Is.EqualTo(0));
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
            var so    = CreateSO(cellsNeeded: 3, bonusPerFlush: 1825);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1825));
            Assert.That(so.FlushCount,  Is.EqualTo(1));
            Assert.That(so.Cells,       Is.EqualTo(0));
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
        public void SO_RecordBotCapture_RemovesCells()
        {
            var so = CreateSO(cellsNeeded: 5, corruptPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cells, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cellsNeeded: 5, corruptPerBot: 10);
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
        public void SO_OnMemoryFlushed_FiresEvent()
        {
            var so    = CreateSO(cellsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMemorySO)
                .GetField("_onMemoryFlushed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cellsNeeded: 2, bonusPerFlush: 1825);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cells,            Is.EqualTo(0));
            Assert.That(so.FlushCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFlushes_Accumulate()
        {
            var so = CreateSO(cellsNeeded: 2, bonusPerFlush: 1825);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FlushCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3650));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MemorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MemorySO, Is.Null);
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
            typeof(ZoneControlCaptureMemoryController)
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

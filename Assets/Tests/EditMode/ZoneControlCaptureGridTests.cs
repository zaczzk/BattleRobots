using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGridTests
    {
        private static ZoneControlCaptureGridSO CreateSO(
            int columns = 3, int rows = 3, int bonusPerRow = 175, int completionBonus = 600)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGridSO>();
            typeof(ZoneControlCaptureGridSO)
                .GetField("_columns", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, columns);
            typeof(ZoneControlCaptureGridSO)
                .GetField("_rows", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rows);
            typeof(ZoneControlCaptureGridSO)
                .GetField("_bonusPerRow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRow);
            typeof(ZoneControlCaptureGridSO)
                .GetField("_completionBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, completionBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGridController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGridController>();
        }

        [Test]
        public void SO_FreshInstance_FilledSlots_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FilledSlots, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RowsCompleted_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RowsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsComplete_False()
        {
            var so = CreateSO();
            Assert.That(so.IsComplete, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsFilledSlots()
        {
            var so = CreateSO(columns: 3, rows: 3);
            so.RecordPlayerCapture();
            Assert.That(so.FilledSlots, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesRow_IncreasesRowsCompleted()
        {
            var so = CreateSO(columns: 2, rows: 3, bonusPerRow: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.RowsCompleted, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesRow_ReturnsRowBonus()
        {
            var so    = CreateSO(columns: 2, rows: 3, bonusPerRow: 100, completionBonus: 0);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesRow_FiresRowEvent()
        {
            var so    = CreateSO(columns: 2, rows: 3, bonusPerRow: 100);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGridSO)
                .GetField("_onRowComplete", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesGrid_SetsIsComplete()
        {
            var so = CreateSO(columns: 1, rows: 2, bonusPerRow: 0, completionBonus: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsComplete, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AfterComplete_NoOp()
        {
            var so = CreateSO(columns: 1, rows: 1, bonusPerRow: 0, completionBonus: 200);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Assert.That(so.FilledSlots, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DecrementsFilledSlots()
        {
            var so = CreateSO(columns: 3, rows: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.FilledSlots, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesRowsCompleted()
        {
            var so = CreateSO(columns: 2, rows: 3, bonusPerRow: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RowsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(columns: 1, rows: 1, bonusPerRow: 100, completionBonus: 200);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.FilledSlots,       Is.EqualTo(0));
            Assert.That(so.RowsCompleted,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.IsComplete,        Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GridSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GridSO, Is.Null);
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
            typeof(ZoneControlCaptureGridController)
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

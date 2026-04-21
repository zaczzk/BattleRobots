using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePistonTests
    {
        private static ZoneControlCapturePistonSO CreateSO(
            int strokesNeeded = 5,
            int missPerBot    = 1,
            int bonusPerCycle = 1195)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePistonSO>();
            typeof(ZoneControlCapturePistonSO)
                .GetField("_strokesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, strokesNeeded);
            typeof(ZoneControlCapturePistonSO)
                .GetField("_missPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, missPerBot);
            typeof(ZoneControlCapturePistonSO)
                .GetField("_bonusPerCycle", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCycle);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePistonController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePistonController>();
        }

        [Test]
        public void SO_FreshInstance_Strokes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Strokes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CycleCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CycleCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStrokes()
        {
            var so = CreateSO(strokesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Strokes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_StrokesAtThreshold()
        {
            var so    = CreateSO(strokesNeeded: 3, bonusPerCycle: 1195);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1195));
            Assert.That(so.CycleCount,  Is.EqualTo(1));
            Assert.That(so.Strokes,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(strokesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesStrokes()
        {
            var so = CreateSO(strokesNeeded: 5, missPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Strokes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(strokesNeeded: 5, missPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Strokes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StrokeProgress_Clamped()
        {
            var so = CreateSO(strokesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StrokeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPistonCycled_FiresEvent()
        {
            var so    = CreateSO(strokesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePistonSO)
                .GetField("_onPistonCycled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(strokesNeeded: 2, bonusPerCycle: 1195);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Strokes,           Is.EqualTo(0));
            Assert.That(so.CycleCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCycles_Accumulate()
        {
            var so = CreateSO(strokesNeeded: 2, bonusPerCycle: 1195);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CycleCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2390));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PistonSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PistonSO, Is.Null);
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
            typeof(ZoneControlCapturePistonController)
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

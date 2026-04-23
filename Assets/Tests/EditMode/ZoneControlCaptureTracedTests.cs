using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTracedTests
    {
        private static ZoneControlCaptureTracedSO CreateSO(
            int loopsNeeded   = 5,
            int unwindPerBot  = 1,
            int bonusPerTrace = 3085)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTracedSO>();
            typeof(ZoneControlCaptureTracedSO)
                .GetField("_loopsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, loopsNeeded);
            typeof(ZoneControlCaptureTracedSO)
                .GetField("_unwindPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unwindPerBot);
            typeof(ZoneControlCaptureTracedSO)
                .GetField("_bonusPerTrace", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTrace);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTracedController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTracedController>();
        }

        [Test]
        public void SO_FreshInstance_Loops_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TraceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TraceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLoops()
        {
            var so = CreateSO(loopsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(loopsNeeded: 3, bonusPerTrace: 3085);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3085));
            Assert.That(so.TraceCount,   Is.EqualTo(1));
            Assert.That(so.Loops,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(loopsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesLoops()
        {
            var so = CreateSO(loopsNeeded: 5, unwindPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(loopsNeeded: 5, unwindPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoopProgress_Clamped()
        {
            var so = CreateSO(loopsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LoopProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTraced_FiresEvent()
        {
            var so    = CreateSO(loopsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTracedSO)
                .GetField("_onTraced", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(loopsNeeded: 2, bonusPerTrace: 3085);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Loops,             Is.EqualTo(0));
            Assert.That(so.TraceCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTraces_Accumulate()
        {
            var so = CreateSO(loopsNeeded: 2, bonusPerTrace: 3085);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TraceCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6170));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TracedSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TracedSO, Is.Null);
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
            typeof(ZoneControlCaptureTracedController)
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

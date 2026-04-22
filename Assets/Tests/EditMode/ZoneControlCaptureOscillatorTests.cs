using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOscillatorTests
    {
        private static ZoneControlCaptureOscillatorSO CreateSO(
            int oscillationsNeeded = 6,
            int dampPerBot         = 2,
            int bonusPerCycle      = 1525)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOscillatorSO>();
            typeof(ZoneControlCaptureOscillatorSO)
                .GetField("_oscillationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, oscillationsNeeded);
            typeof(ZoneControlCaptureOscillatorSO)
                .GetField("_dampPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dampPerBot);
            typeof(ZoneControlCaptureOscillatorSO)
                .GetField("_bonusPerCycle", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCycle);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOscillatorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOscillatorController>();
        }

        [Test]
        public void SO_FreshInstance_Oscillations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Oscillations, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesOscillations()
        {
            var so = CreateSO(oscillationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Oscillations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OscillationsAtThreshold()
        {
            var so    = CreateSO(oscillationsNeeded: 3, bonusPerCycle: 1525);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(1525));
            Assert.That(so.CycleCount,    Is.EqualTo(1));
            Assert.That(so.Oscillations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(oscillationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DampsOscillations()
        {
            var so = CreateSO(oscillationsNeeded: 6, dampPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Oscillations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(oscillationsNeeded: 6, dampPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Oscillations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OscillationProgress_Clamped()
        {
            var so = CreateSO(oscillationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.OscillationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnOscillatorCycled_FiresEvent()
        {
            var so    = CreateSO(oscillationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureOscillatorSO)
                .GetField("_onOscillatorCycled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(oscillationsNeeded: 2, bonusPerCycle: 1525);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Oscillations,      Is.EqualTo(0));
            Assert.That(so.CycleCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCycles_Accumulate()
        {
            var so = CreateSO(oscillationsNeeded: 2, bonusPerCycle: 1525);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CycleCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3050));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OscillatorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OscillatorSO, Is.Null);
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
            typeof(ZoneControlCaptureOscillatorController)
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

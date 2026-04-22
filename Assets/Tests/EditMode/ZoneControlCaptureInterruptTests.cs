using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInterruptTests
    {
        private static ZoneControlCaptureInterruptSO CreateSO(
            int irqsNeeded  = 6,
            int maskPerBot  = 1,
            int bonusPerISR = 1870)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInterruptSO>();
            typeof(ZoneControlCaptureInterruptSO)
                .GetField("_irqsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, irqsNeeded);
            typeof(ZoneControlCaptureInterruptSO)
                .GetField("_maskPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maskPerBot);
            typeof(ZoneControlCaptureInterruptSO)
                .GetField("_bonusPerISR", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerISR);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInterruptController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInterruptController>();
        }

        [Test]
        public void SO_FreshInstance_Irqs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Irqs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsrCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IsrCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesIrqs()
        {
            var so = CreateSO(irqsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Irqs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(irqsNeeded: 3, bonusPerISR: 1870);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(1870));
            Assert.That(so.IsrCount,   Is.EqualTo(1));
            Assert.That(so.Irqs,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(irqsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesIrqs()
        {
            var so = CreateSO(irqsNeeded: 6, maskPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Irqs, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(irqsNeeded: 6, maskPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Irqs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IrqProgress_Clamped()
        {
            var so = CreateSO(irqsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.IrqProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnInterruptHandled_FiresEvent()
        {
            var so    = CreateSO(irqsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInterruptSO)
                .GetField("_onInterruptHandled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(irqsNeeded: 2, bonusPerISR: 1870);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Irqs,             Is.EqualTo(0));
            Assert.That(so.IsrCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleISRs_Accumulate()
        {
            var so = CreateSO(irqsNeeded: 2, bonusPerISR: 1870);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IsrCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3740));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InterruptSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InterruptSO, Is.Null);
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
            typeof(ZoneControlCaptureInterruptController)
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

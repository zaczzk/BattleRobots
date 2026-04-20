using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePortalTests
    {
        private static ZoneControlCapturePortalSO CreateSO(
            int chargesForActivation = 4,
            int drainPerBot          = 1,
            int bonusPerActivation   = 425)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePortalSO>();
            typeof(ZoneControlCapturePortalSO)
                .GetField("_chargesForActivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesForActivation);
            typeof(ZoneControlCapturePortalSO)
                .GetField("_drainPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainPerBot);
            typeof(ZoneControlCapturePortalSO)
                .GetField("_bonusPerActivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerActivation);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePortalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePortalController>();
        }

        [Test]
        public void SO_FreshInstance_Charges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ActivationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ActivationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCharges()
        {
            var so = CreateSO(chargesForActivation: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ActivatesAtThreshold()
        {
            var so    = CreateSO(chargesForActivation: 3, bonusPerActivation: 425);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(425));
            Assert.That(so.ActivationCount,  Is.EqualTo(1));
            Assert.That(so.Charges,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileCharging()
        {
            var so    = CreateSO(chargesForActivation: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsCharges()
        {
            var so = CreateSO(chargesForActivation: 4, drainPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chargesForActivation: 4, drainPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_Clamped()
        {
            var so = CreateSO(chargesForActivation: 4);
            so.RecordPlayerCapture();
            Assert.That(so.ChargeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPortalActivated_FiresEvent()
        {
            var so    = CreateSO(chargesForActivation: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePortalSO)
                .GetField("_onPortalActivated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chargesForActivation: 2, bonusPerActivation: 425);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charges,            Is.EqualTo(0));
            Assert.That(so.ActivationCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleActivations_Accumulate()
        {
            var so = CreateSO(chargesForActivation: 2, bonusPerActivation: 425);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ActivationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(850));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PortalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PortalSO, Is.Null);
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
            typeof(ZoneControlCapturePortalController)
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

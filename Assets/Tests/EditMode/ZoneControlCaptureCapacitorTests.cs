using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCapacitorTests
    {
        private static ZoneControlCaptureCapacitorSO CreateSO(
            int chargesNeeded     = 5,
            int leakPerBot        = 1,
            int bonusPerDischarge = 1495)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCapacitorSO>();
            typeof(ZoneControlCaptureCapacitorSO)
                .GetField("_chargesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesNeeded);
            typeof(ZoneControlCaptureCapacitorSO)
                .GetField("_leakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, leakPerBot);
            typeof(ZoneControlCaptureCapacitorSO)
                .GetField("_bonusPerDischarge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDischarge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCapacitorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCapacitorController>();
        }

        [Test]
        public void SO_FreshInstance_Charge_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Charge, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DischargeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DischargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCharge()
        {
            var so = CreateSO(chargesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Charge, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ChargeAtThreshold()
        {
            var so    = CreateSO(chargesNeeded: 3, bonusPerDischarge: 1495);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(1495));
            Assert.That(so.DischargeCount, Is.EqualTo(1));
            Assert.That(so.Charge,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chargesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_LeaksCharge()
        {
            var so = CreateSO(chargesNeeded: 5, leakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charge, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chargesNeeded: 5, leakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charge, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_Clamped()
        {
            var so = CreateSO(chargesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ChargeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCapacitorDischarged_FiresEvent()
        {
            var so    = CreateSO(chargesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCapacitorSO)
                .GetField("_onCapacitorDischarged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chargesNeeded: 2, bonusPerDischarge: 1495);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charge,            Is.EqualTo(0));
            Assert.That(so.DischargeCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDischarges_Accumulate()
        {
            var so = CreateSO(chargesNeeded: 2, bonusPerDischarge: 1495);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DischargeCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2990));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CapacitorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CapacitorSO, Is.Null);
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
            typeof(ZoneControlCaptureCapacitorController)
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

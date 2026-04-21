using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTalismanTests
    {
        private static ZoneControlCaptureTalismanSO CreateSO(
            int chargesNeeded      = 7,
            int drainPerBot        = 2,
            int bonusPerActivation = 730)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTalismanSO>();
            typeof(ZoneControlCaptureTalismanSO)
                .GetField("_chargesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesNeeded);
            typeof(ZoneControlCaptureTalismanSO)
                .GetField("_drainPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainPerBot);
            typeof(ZoneControlCaptureTalismanSO)
                .GetField("_bonusPerActivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerActivation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTalismanController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTalismanController>();
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
            var so = CreateSO(chargesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ActivatesAtThreshold()
        {
            var so    = CreateSO(chargesNeeded: 3, bonusPerActivation: 730);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(730));
            Assert.That(so.ActivationCount,  Is.EqualTo(1));
            Assert.That(so.Charges,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chargesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsCharges()
        {
            var so = CreateSO(chargesNeeded: 7, drainPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chargesNeeded: 7, drainPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_Clamped()
        {
            var so = CreateSO(chargesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ChargeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTalismanActivated_FiresEvent()
        {
            var so    = CreateSO(chargesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTalismanSO)
                .GetField("_onTalismanActivated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chargesNeeded: 2, bonusPerActivation: 730);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charges,           Is.EqualTo(0));
            Assert.That(so.ActivationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleActivations_Accumulate()
        {
            var so = CreateSO(chargesNeeded: 2, bonusPerActivation: 730);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ActivationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1460));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TalismanSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TalismanSO, Is.Null);
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
            typeof(ZoneControlCaptureTalismanController)
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

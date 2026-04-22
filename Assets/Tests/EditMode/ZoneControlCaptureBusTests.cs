using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBusTests
    {
        private static ZoneControlCaptureBusSO CreateSO(
            int signalsNeeded        = 5,
            int dropPerBot           = 1,
            int bonusPerTransmission = 1810)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBusSO>();
            typeof(ZoneControlCaptureBusSO)
                .GetField("_signalsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, signalsNeeded);
            typeof(ZoneControlCaptureBusSO)
                .GetField("_dropPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dropPerBot);
            typeof(ZoneControlCaptureBusSO)
                .GetField("_bonusPerTransmission", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTransmission);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBusController>();
        }

        [Test]
        public void SO_FreshInstance_Signals_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Signals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TransmissionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TransmissionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSignals()
        {
            var so = CreateSO(signalsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Signals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(signalsNeeded: 3, bonusPerTransmission: 1810);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(1810));
            Assert.That(so.TransmissionCount,   Is.EqualTo(1));
            Assert.That(so.Signals,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(signalsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSignals()
        {
            var so = CreateSO(signalsNeeded: 5, dropPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Signals, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(signalsNeeded: 5, dropPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Signals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SignalProgress_Clamped()
        {
            var so = CreateSO(signalsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SignalProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBusTransmitted_FiresEvent()
        {
            var so    = CreateSO(signalsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBusSO)
                .GetField("_onBusTransmitted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(signalsNeeded: 2, bonusPerTransmission: 1810);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Signals,           Is.EqualTo(0));
            Assert.That(so.TransmissionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTransmissions_Accumulate()
        {
            var so = CreateSO(signalsNeeded: 2, bonusPerTransmission: 1810);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TransmissionCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3620));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BusSO, Is.Null);
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
            typeof(ZoneControlCaptureBusController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTransistorTests
    {
        private static ZoneControlCaptureTransistorSO CreateSO(
            int gatesNeeded   = 5,
            int leakPerBot    = 1,
            int bonusPerSwitch = 1570)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTransistorSO>();
            typeof(ZoneControlCaptureTransistorSO)
                .GetField("_gatesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, gatesNeeded);
            typeof(ZoneControlCaptureTransistorSO)
                .GetField("_leakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, leakPerBot);
            typeof(ZoneControlCaptureTransistorSO)
                .GetField("_bonusPerSwitch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSwitch);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTransistorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTransistorController>();
        }

        [Test]
        public void SO_FreshInstance_Gates_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Gates, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SwitchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SwitchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGates()
        {
            var so = CreateSO(gatesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Gates, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_GatesAtThreshold()
        {
            var so    = CreateSO(gatesNeeded: 3, bonusPerSwitch: 1570);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(1570));
            Assert.That(so.SwitchCount,  Is.EqualTo(1));
            Assert.That(so.Gates,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(gatesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_LeaksGates()
        {
            var so = CreateSO(gatesNeeded: 5, leakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gates, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(gatesNeeded: 5, leakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gates, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GateProgress_Clamped()
        {
            var so = CreateSO(gatesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.GateProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTransistorSwitched_FiresEvent()
        {
            var so    = CreateSO(gatesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTransistorSO)
                .GetField("_onTransistorSwitched", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(gatesNeeded: 2, bonusPerSwitch: 1570);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Gates,             Is.EqualTo(0));
            Assert.That(so.SwitchCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSwitches_Accumulate()
        {
            var so = CreateSO(gatesNeeded: 2, bonusPerSwitch: 1570);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SwitchCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3140));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TransistorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TransistorSO, Is.Null);
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
            typeof(ZoneControlCaptureTransistorController)
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

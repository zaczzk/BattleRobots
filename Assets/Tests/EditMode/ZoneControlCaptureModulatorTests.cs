using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureModulatorTests
    {
        private static ZoneControlCaptureModulatorSO CreateSO(
            int signalsNeeded      = 6,
            int decayPerBot        = 2,
            int bonusPerModulation = 1705)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureModulatorSO>();
            typeof(ZoneControlCaptureModulatorSO)
                .GetField("_signalsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, signalsNeeded);
            typeof(ZoneControlCaptureModulatorSO)
                .GetField("_decayPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decayPerBot);
            typeof(ZoneControlCaptureModulatorSO)
                .GetField("_bonusPerModulation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerModulation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureModulatorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureModulatorController>();
        }

        [Test]
        public void SO_FreshInstance_Signals_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Signals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ModulationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ModulationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSignals()
        {
            var so = CreateSO(signalsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Signals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(signalsNeeded: 3, bonusPerModulation: 1705);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(1705));
            Assert.That(so.ModulationCount,  Is.EqualTo(1));
            Assert.That(so.Signals,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(signalsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSignals()
        {
            var so = CreateSO(signalsNeeded: 6, decayPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Signals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(signalsNeeded: 6, decayPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Signals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SignalProgress_Clamped()
        {
            var so = CreateSO(signalsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SignalProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnModulatorModulated_FiresEvent()
        {
            var so    = CreateSO(signalsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureModulatorSO)
                .GetField("_onModulatorModulated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(signalsNeeded: 2, bonusPerModulation: 1705);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Signals,           Is.EqualTo(0));
            Assert.That(so.ModulationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleModulations_Accumulate()
        {
            var so = CreateSO(signalsNeeded: 2, bonusPerModulation: 1705);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ModulationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3410));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ModulatorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ModulatorSO, Is.Null);
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
            typeof(ZoneControlCaptureModulatorController)
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

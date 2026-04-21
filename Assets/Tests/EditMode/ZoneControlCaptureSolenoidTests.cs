using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSolenoidTests
    {
        private static ZoneControlCaptureSolenoidSO CreateSO(
            int plungersNeeded    = 5,
            int retractPerBot     = 1,
            int bonusPerActuation = 1405)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSolenoidSO>();
            typeof(ZoneControlCaptureSolenoidSO)
                .GetField("_plungersNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, plungersNeeded);
            typeof(ZoneControlCaptureSolenoidSO)
                .GetField("_retractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, retractPerBot);
            typeof(ZoneControlCaptureSolenoidSO)
                .GetField("_bonusPerActuation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerActuation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSolenoidController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSolenoidController>();
        }

        [Test]
        public void SO_FreshInstance_Plungers_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Plungers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ActuationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ActuationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPlungers()
        {
            var so = CreateSO(plungersNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Plungers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PlungersAtThreshold()
        {
            var so    = CreateSO(plungersNeeded: 3, bonusPerActuation: 1405);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(1405));
            Assert.That(so.ActuationCount,  Is.EqualTo(1));
            Assert.That(so.Plungers,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(plungersNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPlungers()
        {
            var so = CreateSO(plungersNeeded: 5, retractPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Plungers, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(plungersNeeded: 5, retractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Plungers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlungerProgress_Clamped()
        {
            var so = CreateSO(plungersNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PlungerProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSolenoidActuated_FiresEvent()
        {
            var so    = CreateSO(plungersNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSolenoidSO)
                .GetField("_onSolenoidActuated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(plungersNeeded: 2, bonusPerActuation: 1405);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Plungers,          Is.EqualTo(0));
            Assert.That(so.ActuationCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleActuations_Accumulate()
        {
            var so = CreateSO(plungersNeeded: 2, bonusPerActuation: 1405);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ActuationCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2810));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SolenoidSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SolenoidSO, Is.Null);
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
            typeof(ZoneControlCaptureSolenoidController)
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

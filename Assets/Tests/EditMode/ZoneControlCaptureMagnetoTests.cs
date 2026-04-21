using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMagnetoTests
    {
        private static ZoneControlCaptureMagnetoSO CreateSO(
            int polesNeeded   = 5,
            int dampPerBot    = 1,
            int bonusPerPulse = 1450)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMagnetoSO>();
            typeof(ZoneControlCaptureMagnetoSO)
                .GetField("_polesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, polesNeeded);
            typeof(ZoneControlCaptureMagnetoSO)
                .GetField("_dampPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dampPerBot);
            typeof(ZoneControlCaptureMagnetoSO)
                .GetField("_bonusPerPulse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPulse);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMagnetoController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMagnetoController>();
        }

        [Test]
        public void SO_FreshInstance_Poles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Poles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PulseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PulseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPoles()
        {
            var so = CreateSO(polesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Poles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PolesAtThreshold()
        {
            var so    = CreateSO(polesNeeded: 3, bonusPerPulse: 1450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1450));
            Assert.That(so.PulseCount,  Is.EqualTo(1));
            Assert.That(so.Poles,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(polesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPoles()
        {
            var so = CreateSO(polesNeeded: 5, dampPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Poles, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(polesNeeded: 5, dampPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Poles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PoleProgress_Clamped()
        {
            var so = CreateSO(polesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PoleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMagnetoCharged_FiresEvent()
        {
            var so    = CreateSO(polesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMagnetoSO)
                .GetField("_onMagnetoCharged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(polesNeeded: 2, bonusPerPulse: 1450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Poles,             Is.EqualTo(0));
            Assert.That(so.PulseCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePulses_Accumulate()
        {
            var so = CreateSO(polesNeeded: 2, bonusPerPulse: 1450);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PulseCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2900));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MagnetoSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MagnetoSO, Is.Null);
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
            typeof(ZoneControlCaptureMagnetoController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePortTests
    {
        private static ZoneControlCapturePortSO CreateSO(
            int portsNeeded  = 5,
            int closePerBot  = 1,
            int bonusPerBind = 1930)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePortSO>();
            typeof(ZoneControlCapturePortSO)
                .GetField("_portsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, portsNeeded);
            typeof(ZoneControlCapturePortSO)
                .GetField("_closePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, closePerBot);
            typeof(ZoneControlCapturePortSO)
                .GetField("_bonusPerBind", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBind);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePortController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePortController>();
        }

        [Test]
        public void SO_FreshInstance_Ports_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ports, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BindCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BindCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPorts()
        {
            var so = CreateSO(portsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Ports, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(portsNeeded: 3, bonusPerBind: 1930);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(1930));
            Assert.That(so.BindCount,  Is.EqualTo(1));
            Assert.That(so.Ports,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(portsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPorts()
        {
            var so = CreateSO(portsNeeded: 5, closePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ports, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(portsNeeded: 5, closePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ports, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PortProgress_Clamped()
        {
            var so = CreateSO(portsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PortProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPortOpened_FiresEvent()
        {
            var so    = CreateSO(portsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePortSO)
                .GetField("_onPortOpened", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(portsNeeded: 2, bonusPerBind: 1930);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ports,            Is.EqualTo(0));
            Assert.That(so.BindCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBinds_Accumulate()
        {
            var so = CreateSO(portsNeeded: 2, bonusPerBind: 1930);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BindCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3860));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PortSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PortSO, Is.Null);
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
            typeof(ZoneControlCapturePortController)
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

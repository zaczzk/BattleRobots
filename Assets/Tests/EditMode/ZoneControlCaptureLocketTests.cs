using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLocketTests
    {
        private static ZoneControlCaptureLocketSO CreateSO(
            int charmsNeeded = 6,
            int removePerBot = 2,
            int bonusPerFill = 700)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLocketSO>();
            typeof(ZoneControlCaptureLocketSO)
                .GetField("_charmsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, charmsNeeded);
            typeof(ZoneControlCaptureLocketSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureLocketSO)
                .GetField("_bonusPerFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFill);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLocketController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLocketController>();
        }

        [Test]
        public void SO_FreshInstance_Charms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Charms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LocketCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LocketCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCharms()
        {
            var so = CreateSO(charmsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Charms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsAtThreshold()
        {
            var so    = CreateSO(charmsNeeded: 3, bonusPerFill: 700);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(700));
            Assert.That(so.LocketCount, Is.EqualTo(1));
            Assert.That(so.Charms,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(charmsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCharms()
        {
            var so = CreateSO(charmsNeeded: 6, removePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charms, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(charmsNeeded: 6, removePerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CharmProgress_Clamped()
        {
            var so = CreateSO(charmsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CharmProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLocketFilled_FiresEvent()
        {
            var so    = CreateSO(charmsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLocketSO)
                .GetField("_onLocketFilled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(charmsNeeded: 2, bonusPerFill: 700);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charms,            Is.EqualTo(0));
            Assert.That(so.LocketCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFills_Accumulate()
        {
            var so = CreateSO(charmsNeeded: 2, bonusPerFill: 700);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LocketCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LocketSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LocketSO, Is.Null);
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
            typeof(ZoneControlCaptureLocketController)
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

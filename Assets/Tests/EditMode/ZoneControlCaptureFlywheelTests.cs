using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFlywheelTests
    {
        private static ZoneControlCaptureFlywheelSO CreateSO(
            int turnsNeeded        = 7,
            int dragPerBot         = 2,
            int bonusPerRevolution = 1210)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFlywheelSO>();
            typeof(ZoneControlCaptureFlywheelSO)
                .GetField("_turnsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, turnsNeeded);
            typeof(ZoneControlCaptureFlywheelSO)
                .GetField("_dragPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dragPerBot);
            typeof(ZoneControlCaptureFlywheelSO)
                .GetField("_bonusPerRevolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRevolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFlywheelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFlywheelController>();
        }

        [Test]
        public void SO_FreshInstance_Turns_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Turns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RevolutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RevolutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTurns()
        {
            var so = CreateSO(turnsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Turns, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TurnsAtThreshold()
        {
            var so    = CreateSO(turnsNeeded: 3, bonusPerRevolution: 1210);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(1210));
            Assert.That(so.RevolutionCount,  Is.EqualTo(1));
            Assert.That(so.Turns,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(turnsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTurns()
        {
            var so = CreateSO(turnsNeeded: 7, dragPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Turns, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(turnsNeeded: 7, dragPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Turns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TurnProgress_Clamped()
        {
            var so = CreateSO(turnsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.TurnProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFlywheelRevolved_FiresEvent()
        {
            var so    = CreateSO(turnsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFlywheelSO)
                .GetField("_onFlywheelRevolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(turnsNeeded: 2, bonusPerRevolution: 1210);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Turns,             Is.EqualTo(0));
            Assert.That(so.RevolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRevolutions_Accumulate()
        {
            var so = CreateSO(turnsNeeded: 2, bonusPerRevolution: 1210);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RevolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2420));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FlywheelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FlywheelSO, Is.Null);
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
            typeof(ZoneControlCaptureFlywheelController)
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

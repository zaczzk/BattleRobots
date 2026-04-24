using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOpcartTests
    {
        private static ZoneControlCaptureOpcartSO CreateSO(
            int liftsNeeded    = 7,
            int obstructPerBot = 2,
            int bonusPerLift   = 3580)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOpcartSO>();
            typeof(ZoneControlCaptureOpcartSO)
                .GetField("_liftsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, liftsNeeded);
            typeof(ZoneControlCaptureOpcartSO)
                .GetField("_obstructPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, obstructPerBot);
            typeof(ZoneControlCaptureOpcartSO)
                .GetField("_bonusPerLift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLift);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOpcartController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOpcartController>();
        }

        [Test]
        public void SO_FreshInstance_Lifts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Lifts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LiftCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLifts()
        {
            var so = CreateSO(liftsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Lifts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(liftsNeeded: 3, bonusPerLift: 3580);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(3580));
            Assert.That(so.LiftCount, Is.EqualTo(1));
            Assert.That(so.Lifts,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(liftsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ObstructsLifts()
        {
            var so = CreateSO(liftsNeeded: 7, obstructPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lifts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(liftsNeeded: 7, obstructPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lifts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LiftProgress_Clamped()
        {
            var so = CreateSO(liftsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.LiftProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnOpcartLifted_FiresEvent()
        {
            var so    = CreateSO(liftsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureOpcartSO)
                .GetField("_onOpcartLifted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(liftsNeeded: 2, bonusPerLift: 3580);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Lifts,             Is.EqualTo(0));
            Assert.That(so.LiftCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLifts_Accumulate()
        {
            var so = CreateSO(liftsNeeded: 2, bonusPerLift: 3580);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LiftCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7160));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OpcartSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OpcartSO, Is.Null);
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
            typeof(ZoneControlCaptureOpcartController)
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

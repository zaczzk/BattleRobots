using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFibrationTests
    {
        private static ZoneControlCaptureFibrationSO CreateSO(
            int fibersNeeded  = 6,
            int unravelPerBot = 2,
            int bonusPerLift  = 2545)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFibrationSO>();
            typeof(ZoneControlCaptureFibrationSO)
                .GetField("_fibersNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fibersNeeded);
            typeof(ZoneControlCaptureFibrationSO)
                .GetField("_unravelPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unravelPerBot);
            typeof(ZoneControlCaptureFibrationSO)
                .GetField("_bonusPerLift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLift);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFibrationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFibrationController>();
        }

        [Test]
        public void SO_FreshInstance_Fibers_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Fibers, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesFibers()
        {
            var so = CreateSO(fibersNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Fibers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(fibersNeeded: 3, bonusPerLift: 2545);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(2545));
            Assert.That(so.LiftCount,  Is.EqualTo(1));
            Assert.That(so.Fibers,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(fibersNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFibers()
        {
            var so = CreateSO(fibersNeeded: 6, unravelPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fibers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fibersNeeded: 6, unravelPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fibers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FiberProgress_Clamped()
        {
            var so = CreateSO(fibersNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.FiberProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFibrationLifted_FiresEvent()
        {
            var so    = CreateSO(fibersNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFibrationSO)
                .GetField("_onFibrationLifted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fibersNeeded: 2, bonusPerLift: 2545);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Fibers,            Is.EqualTo(0));
            Assert.That(so.LiftCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLifts_Accumulate()
        {
            var so = CreateSO(fibersNeeded: 2, bonusPerLift: 2545);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LiftCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5090));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FibrationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FibrationSO, Is.Null);
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
            typeof(ZoneControlCaptureFibrationController)
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

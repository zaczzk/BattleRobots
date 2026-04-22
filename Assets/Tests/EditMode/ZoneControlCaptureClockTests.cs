using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureClockTests
    {
        private static ZoneControlCaptureClockSO CreateSO(
            int ticksNeeded   = 7,
            int jitterPerBot  = 2,
            int bonusPerClock = 1795)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureClockSO>();
            typeof(ZoneControlCaptureClockSO)
                .GetField("_ticksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ticksNeeded);
            typeof(ZoneControlCaptureClockSO)
                .GetField("_jitterPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, jitterPerBot);
            typeof(ZoneControlCaptureClockSO)
                .GetField("_bonusPerClock", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClock);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureClockController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureClockController>();
        }

        [Test]
        public void SO_FreshInstance_Ticks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ticks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClockCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClockCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTicks()
        {
            var so = CreateSO(ticksNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Ticks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(ticksNeeded: 3, bonusPerClock: 1795);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1795));
            Assert.That(so.ClockCount,  Is.EqualTo(1));
            Assert.That(so.Ticks,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ticksNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTicks()
        {
            var so = CreateSO(ticksNeeded: 7, jitterPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ticks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ticksNeeded: 7, jitterPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ticks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TickProgress_Clamped()
        {
            var so = CreateSO(ticksNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.TickProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnClockRun_FiresEvent()
        {
            var so    = CreateSO(ticksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureClockSO)
                .GetField("_onClockRun", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ticksNeeded: 2, bonusPerClock: 1795);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ticks,             Is.EqualTo(0));
            Assert.That(so.ClockCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClockRuns_Accumulate()
        {
            var so = CreateSO(ticksNeeded: 2, bonusPerClock: 1795);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClockCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3590));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ClockSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ClockSO, Is.Null);
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
            typeof(ZoneControlCaptureClockController)
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

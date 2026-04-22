using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCPUTests
    {
        private static ZoneControlCaptureCPUSO CreateSO(
            int cyclesNeeded  = 6,
            int stallPerBot   = 2,
            int bonusPerCycle = 1765)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCPUSO>();
            typeof(ZoneControlCaptureCPUSO)
                .GetField("_cyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cyclesNeeded);
            typeof(ZoneControlCaptureCPUSO)
                .GetField("_stallPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stallPerBot);
            typeof(ZoneControlCaptureCPUSO)
                .GetField("_bonusPerCycle", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCycle);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCPUController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCPUController>();
        }

        [Test]
        public void SO_FreshInstance_Cycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CycleCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CycleCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCycles()
        {
            var so = CreateSO(cyclesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cyclesNeeded: 3, bonusPerCycle: 1765);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1765));
            Assert.That(so.CycleCount,   Is.EqualTo(1));
            Assert.That(so.Cycles,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cyclesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCycles()
        {
            var so = CreateSO(cyclesNeeded: 6, stallPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cyclesNeeded: 6, stallPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CycleProgress_Clamped()
        {
            var so = CreateSO(cyclesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCPUCycled_FiresEvent()
        {
            var so    = CreateSO(cyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCPUSO)
                .GetField("_onCPUCycled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cyclesNeeded: 2, bonusPerCycle: 1765);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cycles,            Is.EqualTo(0));
            Assert.That(so.CycleCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCycles_Accumulate()
        {
            var so = CreateSO(cyclesNeeded: 2, bonusPerCycle: 1765);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CycleCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3530));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CPUSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CPUSO, Is.Null);
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
            typeof(ZoneControlCaptureCPUController)
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

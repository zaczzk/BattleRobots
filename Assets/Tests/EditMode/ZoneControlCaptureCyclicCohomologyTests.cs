using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCyclicCohomologyTests
    {
        private static ZoneControlCaptureCyclicCohomologySO CreateSO(
            int cyclesNeeded   = 5,
            int degeneracyPerBot = 1,
            int bonusPerTrace  = 3925)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCyclicCohomologySO>();
            typeof(ZoneControlCaptureCyclicCohomologySO)
                .GetField("_cyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cyclesNeeded);
            typeof(ZoneControlCaptureCyclicCohomologySO)
                .GetField("_degeneracyPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, degeneracyPerBot);
            typeof(ZoneControlCaptureCyclicCohomologySO)
                .GetField("_bonusPerTrace", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTrace);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCyclicCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCyclicCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Cycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TraceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TraceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCycles()
        {
            var so = CreateSO(cyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cyclesNeeded: 3, bonusPerTrace: 3925);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3925));
            Assert.That(so.TraceCount,   Is.EqualTo(1));
            Assert.That(so.Cycles,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cyclesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDegeneracies()
        {
            var so = CreateSO(cyclesNeeded: 5, degeneracyPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cyclesNeeded: 5, degeneracyPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CycleProgress_Clamped()
        {
            var so = CreateSO(cyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCyclicCohomologyTraced_FiresEvent()
        {
            var so    = CreateSO(cyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCyclicCohomologySO)
                .GetField("_onCyclicCohomologyTraced", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cyclesNeeded: 2, bonusPerTrace: 3925);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cycles,           Is.EqualTo(0));
            Assert.That(so.TraceCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTraces_Accumulate()
        {
            var so = CreateSO(cyclesNeeded: 2, bonusPerTrace: 3925);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TraceCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7850));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CyclicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CyclicSO, Is.Null);
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
            typeof(ZoneControlCaptureCyclicCohomologyController)
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

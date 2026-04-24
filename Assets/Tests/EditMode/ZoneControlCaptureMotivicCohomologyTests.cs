using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMotivicCohomologyTests
    {
        private static ZoneControlCaptureMotivicCohomologySO CreateSO(
            int cyclesNeeded       = 5,
            int breakPerBot        = 1,
            int bonusPerMotivation = 3805)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMotivicCohomologySO>();
            typeof(ZoneControlCaptureMotivicCohomologySO)
                .GetField("_cyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cyclesNeeded);
            typeof(ZoneControlCaptureMotivicCohomologySO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureMotivicCohomologySO)
                .GetField("_bonusPerMotivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMotivation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMotivicCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMotivicCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Cycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MotivateCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MotivateCount, Is.EqualTo(0));
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
            var so    = CreateSO(cyclesNeeded: 3, bonusPerMotivation: 3805);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(3805));
            Assert.That(so.MotivateCount,  Is.EqualTo(1));
            Assert.That(so.Cycles,         Is.EqualTo(0));
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
        public void SO_RecordBotCapture_BreaksAlgebraicRelations()
        {
            var so = CreateSO(cyclesNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cyclesNeeded: 5, breakPerBot: 10);
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
        public void SO_OnMotivicCohomologyMotivated_FiresEvent()
        {
            var so    = CreateSO(cyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMotivicCohomologySO)
                .GetField("_onMotivicCohomologyMotivated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cyclesNeeded: 2, bonusPerMotivation: 3805);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cycles,            Is.EqualTo(0));
            Assert.That(so.MotivateCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMotivations_Accumulate()
        {
            var so = CreateSO(cyclesNeeded: 2, bonusPerMotivation: 3805);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MotivateCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7610));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MotivicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MotivicSO, Is.Null);
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
            typeof(ZoneControlCaptureMotivicCohomologyController)
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

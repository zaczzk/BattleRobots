using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHaltingProblemTests
    {
        private static ZoneControlCaptureHaltingProblemSO CreateSO(
            int computationStepsNeeded = 5,
            int infiniteLoopsPerBot    = 1,
            int bonusPerHalt           = 4645)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHaltingProblemSO>();
            typeof(ZoneControlCaptureHaltingProblemSO)
                .GetField("_computationStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, computationStepsNeeded);
            typeof(ZoneControlCaptureHaltingProblemSO)
                .GetField("_infiniteLoopsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, infiniteLoopsPerBot);
            typeof(ZoneControlCaptureHaltingProblemSO)
                .GetField("_bonusPerHalt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHalt);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHaltingProblemController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHaltingProblemController>();
        }

        [Test]
        public void SO_FreshInstance_ComputationSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComputationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DecisionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DecisionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSteps()
        {
            var so = CreateSO(computationStepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ComputationSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(computationStepsNeeded: 3, bonusPerHalt: 4645);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4645));
            Assert.That(so.DecisionCount,     Is.EqualTo(1));
            Assert.That(so.ComputationSteps,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(computationStepsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesInfiniteLoops()
        {
            var so = CreateSO(computationStepsNeeded: 5, infiniteLoopsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ComputationSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(computationStepsNeeded: 5, infiniteLoopsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ComputationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputationStepProgress_Clamped()
        {
            var so = CreateSO(computationStepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ComputationStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHaltingProblemDecided_FiresEvent()
        {
            var so    = CreateSO(computationStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHaltingProblemSO)
                .GetField("_onHaltingProblemDecided", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(computationStepsNeeded: 2, bonusPerHalt: 4645);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ComputationSteps,  Is.EqualTo(0));
            Assert.That(so.DecisionCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDecisions_Accumulate()
        {
            var so = CreateSO(computationStepsNeeded: 2, bonusPerHalt: 4645);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DecisionCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9290));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HaltingProblemSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HaltingProblemSO, Is.Null);
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
            typeof(ZoneControlCaptureHaltingProblemController)
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

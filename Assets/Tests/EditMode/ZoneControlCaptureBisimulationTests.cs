using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBisimulationTests
    {
        private static ZoneControlCaptureBisimulationSO CreateSO(
            int bisimulationStepsNeeded        = 6,
            int observationalDivergencesPerBot = 1,
            int bonusPerBisimulation           = 5200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBisimulationSO>();
            typeof(ZoneControlCaptureBisimulationSO)
                .GetField("_bisimulationStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bisimulationStepsNeeded);
            typeof(ZoneControlCaptureBisimulationSO)
                .GetField("_observationalDivergencesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, observationalDivergencesPerBot);
            typeof(ZoneControlCaptureBisimulationSO)
                .GetField("_bonusPerBisimulation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBisimulation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBisimulationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBisimulationController>();
        }

        [Test]
        public void SO_FreshInstance_BisimulationSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BisimulationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BisimulationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BisimulationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBisimulationSteps()
        {
            var so = CreateSO(bisimulationStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BisimulationSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bisimulationStepsNeeded: 3, bonusPerBisimulation: 5200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(5200));
            Assert.That(so.BisimulationCount, Is.EqualTo(1));
            Assert.That(so.BisimulationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bisimulationStepsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesObservationalDivergences()
        {
            var so = CreateSO(bisimulationStepsNeeded: 6, observationalDivergencesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BisimulationSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bisimulationStepsNeeded: 6, observationalDivergencesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BisimulationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BisimulationStepProgress_Clamped()
        {
            var so = CreateSO(bisimulationStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BisimulationStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBisimulationCompleted_FiresEvent()
        {
            var so    = CreateSO(bisimulationStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBisimulationSO)
                .GetField("_onBisimulationCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bisimulationStepsNeeded: 2, bonusPerBisimulation: 5200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.BisimulationSteps, Is.EqualTo(0));
            Assert.That(so.BisimulationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBisimulations_Accumulate()
        {
            var so = CreateSO(bisimulationStepsNeeded: 2, bonusPerBisimulation: 5200);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BisimulationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BisimulationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BisimulationSO, Is.Null);
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
            typeof(ZoneControlCaptureBisimulationController)
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

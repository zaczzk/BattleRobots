using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTuringCompletenessTests
    {
        private static ZoneControlCaptureTuringCompletenessSO CreateSO(
            int simulationStepsNeeded   = 5,
            int memoryConstraintsPerBot = 1,
            int bonusPerSimulation      = 4660)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTuringCompletenessSO>();
            typeof(ZoneControlCaptureTuringCompletenessSO)
                .GetField("_simulationStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, simulationStepsNeeded);
            typeof(ZoneControlCaptureTuringCompletenessSO)
                .GetField("_memoryConstraintsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, memoryConstraintsPerBot);
            typeof(ZoneControlCaptureTuringCompletenessSO)
                .GetField("_bonusPerSimulation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSimulation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTuringCompletenessController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTuringCompletenessController>();
        }

        [Test]
        public void SO_FreshInstance_SimulationSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SimulationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SimulationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SimulationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSteps()
        {
            var so = CreateSO(simulationStepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SimulationSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(simulationStepsNeeded: 3, bonusPerSimulation: 4660);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4660));
            Assert.That(so.SimulationCount,  Is.EqualTo(1));
            Assert.That(so.SimulationSteps,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(simulationStepsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesMemoryConstraints()
        {
            var so = CreateSO(simulationStepsNeeded: 5, memoryConstraintsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SimulationSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(simulationStepsNeeded: 5, memoryConstraintsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SimulationSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SimulationStepProgress_Clamped()
        {
            var so = CreateSO(simulationStepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SimulationStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTuringCompletenessSimulated_FiresEvent()
        {
            var so    = CreateSO(simulationStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTuringCompletenessSO)
                .GetField("_onTuringCompletenessSimulated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(simulationStepsNeeded: 2, bonusPerSimulation: 4660);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SimulationSteps,    Is.EqualTo(0));
            Assert.That(so.SimulationCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSimulations_Accumulate()
        {
            var so = CreateSO(simulationStepsNeeded: 2, bonusPerSimulation: 4660);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SimulationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9320));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TuringCompletenessSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TuringCompletenessSO, Is.Null);
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
            typeof(ZoneControlCaptureTuringCompletenessController)
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

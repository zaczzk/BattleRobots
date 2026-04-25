using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCollatzConjectureTests
    {
        private static ZoneControlCaptureCollatzConjectureSO CreateSO(
            int convergenceStepsNeeded   = 7,
            int divergentSequencesPerBot = 2,
            int bonusPerConvergence      = 4735)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCollatzConjectureSO>();
            typeof(ZoneControlCaptureCollatzConjectureSO)
                .GetField("_convergenceStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, convergenceStepsNeeded);
            typeof(ZoneControlCaptureCollatzConjectureSO)
                .GetField("_divergentSequencesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, divergentSequencesPerBot);
            typeof(ZoneControlCaptureCollatzConjectureSO)
                .GetField("_bonusPerConvergence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConvergence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCollatzConjectureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCollatzConjectureController>();
        }

        [Test]
        public void SO_FreshInstance_ConvergenceSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConvergenceSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConvergenceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConvergenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesConvergenceSteps()
        {
            var so = CreateSO(convergenceStepsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ConvergenceSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(convergenceStepsNeeded: 3, bonusPerConvergence: 4735);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4735));
            Assert.That(so.ConvergenceCount,  Is.EqualTo(1));
            Assert.That(so.ConvergenceSteps,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(convergenceStepsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDivergentSequences()
        {
            var so = CreateSO(convergenceStepsNeeded: 7, divergentSequencesPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConvergenceSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(convergenceStepsNeeded: 7, divergentSequencesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConvergenceSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ConvergenceStepProgress_Clamped()
        {
            var so = CreateSO(convergenceStepsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ConvergenceStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCollatzConjectureConverged_FiresEvent()
        {
            var so    = CreateSO(convergenceStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCollatzConjectureSO)
                .GetField("_onCollatzConjectureConverged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(convergenceStepsNeeded: 2, bonusPerConvergence: 4735);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ConvergenceSteps,  Is.EqualTo(0));
            Assert.That(so.ConvergenceCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConvergences_Accumulate()
        {
            var so = CreateSO(convergenceStepsNeeded: 2, bonusPerConvergence: 4735);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConvergenceCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9470));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CollatzSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CollatzSO, Is.Null);
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
            typeof(ZoneControlCaptureCollatzConjectureController)
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

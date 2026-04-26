using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCorecursionTests
    {
        private static ZoneControlCaptureCorecursionSO CreateSO(
            int corecursiveStepsNeeded         = 6,
            int nonProductiveDivergencesPerBot = 1,
            int bonusPerCorecursiveStep        = 5140)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCorecursionSO>();
            typeof(ZoneControlCaptureCorecursionSO)
                .GetField("_corecursiveStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, corecursiveStepsNeeded);
            typeof(ZoneControlCaptureCorecursionSO)
                .GetField("_nonProductiveDivergencesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nonProductiveDivergencesPerBot);
            typeof(ZoneControlCaptureCorecursionSO)
                .GetField("_bonusPerCorecursiveStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCorecursiveStep);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCorecursionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCorecursionController>();
        }

        [Test]
        public void SO_FreshInstance_CorecursiveSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CorecursiveSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CorecursionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CorecursionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCorecursiveSteps()
        {
            var so = CreateSO(corecursiveStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CorecursiveSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(corecursiveStepsNeeded: 3, bonusPerCorecursiveStep: 5140);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(5140));
            Assert.That(so.CorecursionCount,  Is.EqualTo(1));
            Assert.That(so.CorecursiveSteps,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(corecursiveStepsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNonProductiveDivergences()
        {
            var so = CreateSO(corecursiveStepsNeeded: 6, nonProductiveDivergencesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CorecursiveSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(corecursiveStepsNeeded: 6, nonProductiveDivergencesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CorecursiveSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CorecursiveStepProgress_Clamped()
        {
            var so = CreateSO(corecursiveStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CorecursiveStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCorecursionCompleted_FiresEvent()
        {
            var so    = CreateSO(corecursiveStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCorecursionSO)
                .GetField("_onCorecursionCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(corecursiveStepsNeeded: 2, bonusPerCorecursiveStep: 5140);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CorecursiveSteps,   Is.EqualTo(0));
            Assert.That(so.CorecursionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCorecursions_Accumulate()
        {
            var so = CreateSO(corecursiveStepsNeeded: 2, bonusPerCorecursiveStep: 5140);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CorecursionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10280));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CorecursionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CorecursionSO, Is.Null);
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
            typeof(ZoneControlCaptureCorecursionController)
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

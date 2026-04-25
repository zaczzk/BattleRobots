using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHodgeConjectureTests
    {
        private static ZoneControlCaptureHodgeConjectureSO CreateSO(
            int hodgeCyclesNeeded             = 5,
            int nonAlgebraicObstructionsPerBot = 1,
            int bonusPerClassification         = 4540)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHodgeConjectureSO>();
            typeof(ZoneControlCaptureHodgeConjectureSO)
                .GetField("_hodgeCyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, hodgeCyclesNeeded);
            typeof(ZoneControlCaptureHodgeConjectureSO)
                .GetField("_nonAlgebraicObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nonAlgebraicObstructionsPerBot);
            typeof(ZoneControlCaptureHodgeConjectureSO)
                .GetField("_bonusPerClassification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClassification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHodgeConjectureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHodgeConjectureController>();
        }

        [Test]
        public void SO_FreshInstance_HodgeCycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HodgeCycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClassificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClassificationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHodgeCycles()
        {
            var so = CreateSO(hodgeCyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.HodgeCycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(hodgeCyclesNeeded: 3, bonusPerClassification: 4540);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4540));
            Assert.That(so.ClassificationCount, Is.EqualTo(1));
            Assert.That(so.HodgeCycles,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(hodgeCyclesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNonAlgebraicObstructions()
        {
            var so = CreateSO(hodgeCyclesNeeded: 5, nonAlgebraicObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HodgeCycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(hodgeCyclesNeeded: 5, nonAlgebraicObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HodgeCycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HodgeCycleProgress_Clamped()
        {
            var so = CreateSO(hodgeCyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.HodgeCycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHodgeConjectureClassified_FiresEvent()
        {
            var so    = CreateSO(hodgeCyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHodgeConjectureSO)
                .GetField("_onHodgeConjectureClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(hodgeCyclesNeeded: 2, bonusPerClassification: 4540);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.HodgeCycles,        Is.EqualTo(0));
            Assert.That(so.ClassificationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClassifications_Accumulate()
        {
            var so = CreateSO(hodgeCyclesNeeded: 2, bonusPerClassification: 4540);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClassificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(9080));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HodgeConjectureSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HodgeConjectureSO, Is.Null);
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
            typeof(ZoneControlCaptureHodgeConjectureController)
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

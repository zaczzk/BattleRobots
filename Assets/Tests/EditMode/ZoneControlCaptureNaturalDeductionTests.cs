using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNaturalDeductionTests
    {
        private static ZoneControlCaptureNaturalDeductionSO CreateSO(
            int dischargeStepsNeeded          = 6,
            int undischargedAssumptionsPerBot = 1,
            int bonusPerDischarge             = 5005)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNaturalDeductionSO>();
            typeof(ZoneControlCaptureNaturalDeductionSO)
                .GetField("_dischargeStepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dischargeStepsNeeded);
            typeof(ZoneControlCaptureNaturalDeductionSO)
                .GetField("_undischargedAssumptionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, undischargedAssumptionsPerBot);
            typeof(ZoneControlCaptureNaturalDeductionSO)
                .GetField("_bonusPerDischarge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDischarge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNaturalDeductionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNaturalDeductionController>();
        }

        [Test]
        public void SO_FreshInstance_DischargeSteps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DischargeSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DischargeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DischargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSteps()
        {
            var so = CreateSO(dischargeStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DischargeSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(dischargeStepsNeeded: 3, bonusPerDischarge: 5005);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(5005));
            Assert.That(so.DischargeCount,  Is.EqualTo(1));
            Assert.That(so.DischargeSteps,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(dischargeStepsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesUndischargedAssumptions()
        {
            var so = CreateSO(dischargeStepsNeeded: 6, undischargedAssumptionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DischargeSteps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(dischargeStepsNeeded: 6, undischargedAssumptionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DischargeSteps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DischargeStepProgress_Clamped()
        {
            var so = CreateSO(dischargeStepsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DischargeStepProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNaturalDeductionCompleted_FiresEvent()
        {
            var so    = CreateSO(dischargeStepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNaturalDeductionSO)
                .GetField("_onNaturalDeductionCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(dischargeStepsNeeded: 2, bonusPerDischarge: 5005);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DischargeSteps,    Is.EqualTo(0));
            Assert.That(so.DischargeCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDischarges_Accumulate()
        {
            var so = CreateSO(dischargeStepsNeeded: 2, bonusPerDischarge: 5005);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DischargeCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10010));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NaturalDeductionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NaturalDeductionSO, Is.Null);
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
            typeof(ZoneControlCaptureNaturalDeductionController)
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

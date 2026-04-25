using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureChoiceAxiomTests
    {
        private static ZoneControlCaptureChoiceAxiomSO CreateSO(
            int wellOrderingsNeeded         = 6,
            int undecidableSelectionsPerBot = 1,
            int bonusPerChoiceFunction      = 4810)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureChoiceAxiomSO>();
            typeof(ZoneControlCaptureChoiceAxiomSO)
                .GetField("_wellOrderingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wellOrderingsNeeded);
            typeof(ZoneControlCaptureChoiceAxiomSO)
                .GetField("_undecidableSelectionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, undecidableSelectionsPerBot);
            typeof(ZoneControlCaptureChoiceAxiomSO)
                .GetField("_bonusPerChoiceFunction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerChoiceFunction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureChoiceAxiomController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChoiceAxiomController>();
        }

        [Test]
        public void SO_FreshInstance_WellOrderings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WellOrderings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChoiceFunctionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChoiceFunctionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWellOrderings()
        {
            var so = CreateSO(wellOrderingsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.WellOrderings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(wellOrderingsNeeded: 3, bonusPerChoiceFunction: 4810);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4810));
            Assert.That(so.ChoiceFunctionCount, Is.EqualTo(1));
            Assert.That(so.WellOrderings,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(wellOrderingsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesUndecidableSelections()
        {
            var so = CreateSO(wellOrderingsNeeded: 6, undecidableSelectionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.WellOrderings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(wellOrderingsNeeded: 6, undecidableSelectionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.WellOrderings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WellOrderingProgress_Clamped()
        {
            var so = CreateSO(wellOrderingsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.WellOrderingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnChoiceAxiomApplied_FiresEvent()
        {
            var so    = CreateSO(wellOrderingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChoiceAxiomSO)
                .GetField("_onChoiceAxiomApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(wellOrderingsNeeded: 2, bonusPerChoiceFunction: 4810);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.WellOrderings,       Is.EqualTo(0));
            Assert.That(so.ChoiceFunctionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleChoiceFunctions_Accumulate()
        {
            var so = CreateSO(wellOrderingsNeeded: 2, bonusPerChoiceFunction: 4810);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ChoiceFunctionCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(9620));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChoiceAxiomSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChoiceAxiomSO, Is.Null);
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
            typeof(ZoneControlCaptureChoiceAxiomController)
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

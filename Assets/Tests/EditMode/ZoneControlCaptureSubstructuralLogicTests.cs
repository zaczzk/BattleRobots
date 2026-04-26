using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSubstructuralLogicTests
    {
        private static ZoneControlCaptureSubstructuralLogicSO CreateSO(
            int structuralRulesNeeded  = 6,
            int ruleViolationsPerBot   = 1,
            int bonusPerRuleApplication = 5080)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSubstructuralLogicSO>();
            typeof(ZoneControlCaptureSubstructuralLogicSO)
                .GetField("_structuralRulesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, structuralRulesNeeded);
            typeof(ZoneControlCaptureSubstructuralLogicSO)
                .GetField("_ruleViolationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ruleViolationsPerBot);
            typeof(ZoneControlCaptureSubstructuralLogicSO)
                .GetField("_bonusPerRuleApplication", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRuleApplication);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSubstructuralLogicController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSubstructuralLogicController>();
        }

        [Test]
        public void SO_FreshInstance_StructuralRules_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StructuralRules, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RuleApplicationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RuleApplicationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStructuralRules()
        {
            var so = CreateSO(structuralRulesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.StructuralRules, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(structuralRulesNeeded: 3, bonusPerRuleApplication: 5080);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                      Is.EqualTo(5080));
            Assert.That(so.RuleApplicationCount,    Is.EqualTo(1));
            Assert.That(so.StructuralRules,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(structuralRulesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesRuleViolations()
        {
            var so = CreateSO(structuralRulesNeeded: 6, ruleViolationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StructuralRules, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(structuralRulesNeeded: 6, ruleViolationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StructuralRules, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StructuralRuleProgress_Clamped()
        {
            var so = CreateSO(structuralRulesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.StructuralRuleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSubstructuralLogicCompleted_FiresEvent()
        {
            var so    = CreateSO(structuralRulesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSubstructuralLogicSO)
                .GetField("_onSubstructuralLogicCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(structuralRulesNeeded: 2, bonusPerRuleApplication: 5080);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.StructuralRules,      Is.EqualTo(0));
            Assert.That(so.RuleApplicationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRuleApplications_Accumulate()
        {
            var so = CreateSO(structuralRulesNeeded: 2, bonusPerRuleApplication: 5080);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RuleApplicationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(10160));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SubstructuralLogicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SubstructuralLogicSO, Is.Null);
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
            typeof(ZoneControlCaptureSubstructuralLogicController)
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

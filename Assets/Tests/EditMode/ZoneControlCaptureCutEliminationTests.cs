using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCutEliminationTests
    {
        private static ZoneControlCaptureCutEliminationSO CreateSO(
            int cutFreeDerivationsNeeded = 6,
            int cutRulesPerBot           = 1,
            int bonusPerElimination      = 4930)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCutEliminationSO>();
            typeof(ZoneControlCaptureCutEliminationSO)
                .GetField("_cutFreeDerivationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cutFreeDerivationsNeeded);
            typeof(ZoneControlCaptureCutEliminationSO)
                .GetField("_cutRulesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cutRulesPerBot);
            typeof(ZoneControlCaptureCutEliminationSO)
                .GetField("_bonusPerElimination", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerElimination);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCutEliminationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCutEliminationController>();
        }

        [Test]
        public void SO_FreshInstance_CutFreeDerivations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CutFreeDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EliminationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EliminationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDerivations()
        {
            var so = CreateSO(cutFreeDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CutFreeDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cutFreeDerivationsNeeded: 3, bonusPerElimination: 4930);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4930));
            Assert.That(so.EliminationCount, Is.EqualTo(1));
            Assert.That(so.CutFreeDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cutFreeDerivationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCutRules()
        {
            var so = CreateSO(cutFreeDerivationsNeeded: 6, cutRulesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CutFreeDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cutFreeDerivationsNeeded: 6, cutRulesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CutFreeDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CutFreeDerivationProgress_Clamped()
        {
            var so = CreateSO(cutFreeDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CutFreeDerivationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCutEliminationAchieved_FiresEvent()
        {
            var so    = CreateSO(cutFreeDerivationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCutEliminationSO)
                .GetField("_onCutEliminationAchieved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cutFreeDerivationsNeeded: 2, bonusPerElimination: 4930);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CutFreeDerivations, Is.EqualTo(0));
            Assert.That(so.EliminationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEliminations_Accumulate()
        {
            var so = CreateSO(cutFreeDerivationsNeeded: 2, bonusPerElimination: 4930);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EliminationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9860));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CutEliminationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CutEliminationSO, Is.Null);
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
            typeof(ZoneControlCaptureCutEliminationController)
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

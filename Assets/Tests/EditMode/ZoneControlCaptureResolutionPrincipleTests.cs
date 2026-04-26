using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureResolutionPrincipleTests
    {
        private static ZoneControlCaptureResolutionPrincipleSO CreateSO(
            int resolvedClausesNeeded = 6,
            int tautologiesPerBot     = 1,
            int bonusPerResolution    = 4975)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureResolutionPrincipleSO>();
            typeof(ZoneControlCaptureResolutionPrincipleSO)
                .GetField("_resolvedClausesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, resolvedClausesNeeded);
            typeof(ZoneControlCaptureResolutionPrincipleSO)
                .GetField("_tautologiesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tautologiesPerBot);
            typeof(ZoneControlCaptureResolutionPrincipleSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureResolutionPrincipleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureResolutionPrincipleController>();
        }

        [Test]
        public void SO_FreshInstance_ResolvedClauses_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResolvedClauses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResolutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResolutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesClauses()
        {
            var so = CreateSO(resolvedClausesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ResolvedClauses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(resolvedClausesNeeded: 3, bonusPerResolution: 4975);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4975));
            Assert.That(so.ResolutionCount,   Is.EqualTo(1));
            Assert.That(so.ResolvedClauses,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(resolvedClausesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTautologies()
        {
            var so = CreateSO(resolvedClausesNeeded: 6, tautologiesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ResolvedClauses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(resolvedClausesNeeded: 6, tautologiesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ResolvedClauses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResolvedClauseProgress_Clamped()
        {
            var so = CreateSO(resolvedClausesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ResolvedClauseProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnResolutionPrincipleApplied_FiresEvent()
        {
            var so    = CreateSO(resolvedClausesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureResolutionPrincipleSO)
                .GetField("_onResolutionPrincipleApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(resolvedClausesNeeded: 2, bonusPerResolution: 4975);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ResolvedClauses,   Is.EqualTo(0));
            Assert.That(so.ResolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(resolvedClausesNeeded: 2, bonusPerResolution: 4975);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9950));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ResolutionPrincipleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ResolutionPrincipleSO, Is.Null);
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
            typeof(ZoneControlCaptureResolutionPrincipleController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRefinementTypesTests
    {
        private static ZoneControlCaptureRefinementTypesSO CreateSO(
            int verifiedRefinementsNeeded     = 6,
            int predicateFalsificationsPerBot = 1,
            int bonusPerRefinement            = 5170)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRefinementTypesSO>();
            typeof(ZoneControlCaptureRefinementTypesSO)
                .GetField("_verifiedRefinementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, verifiedRefinementsNeeded);
            typeof(ZoneControlCaptureRefinementTypesSO)
                .GetField("_predicateFalsificationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, predicateFalsificationsPerBot);
            typeof(ZoneControlCaptureRefinementTypesSO)
                .GetField("_bonusPerRefinement", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRefinement);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRefinementTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRefinementTypesController>();
        }

        [Test]
        public void SO_FreshInstance_VerifiedRefinements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VerifiedRefinements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RefinementCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RefinementCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesVerifiedRefinements()
        {
            var so = CreateSO(verifiedRefinementsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.VerifiedRefinements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(verifiedRefinementsNeeded: 3, bonusPerRefinement: 5170);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(5170));
            Assert.That(so.RefinementCount,     Is.EqualTo(1));
            Assert.That(so.VerifiedRefinements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(verifiedRefinementsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesPredicateFalsifications()
        {
            var so = CreateSO(verifiedRefinementsNeeded: 6, predicateFalsificationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VerifiedRefinements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(verifiedRefinementsNeeded: 6, predicateFalsificationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VerifiedRefinements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VerifiedRefinementProgress_Clamped()
        {
            var so = CreateSO(verifiedRefinementsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.VerifiedRefinementProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRefinementTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(verifiedRefinementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRefinementTypesSO)
                .GetField("_onRefinementTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(verifiedRefinementsNeeded: 2, bonusPerRefinement: 5170);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.VerifiedRefinements, Is.EqualTo(0));
            Assert.That(so.RefinementCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRefinements_Accumulate()
        {
            var so = CreateSO(verifiedRefinementsNeeded: 2, bonusPerRefinement: 5170);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RefinementCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10340));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RefinementTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RefinementTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureRefinementTypesController)
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

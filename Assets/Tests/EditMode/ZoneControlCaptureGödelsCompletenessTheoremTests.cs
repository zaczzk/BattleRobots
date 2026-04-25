using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGödelsCompletenessTheoremTests
    {
        private static ZoneControlCaptureGödelsCompletenessTheoremSO CreateSO(
            int consistentExtensionsNeeded      = 6,
            int incompleteAxiomatizationsPerBot = 1,
            int bonusPerCompletion              = 4885)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGödelsCompletenessTheoremSO>();
            typeof(ZoneControlCaptureGödelsCompletenessTheoremSO)
                .GetField("_consistentExtensionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, consistentExtensionsNeeded);
            typeof(ZoneControlCaptureGödelsCompletenessTheoremSO)
                .GetField("_incompleteAxiomatizationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, incompleteAxiomatizationsPerBot);
            typeof(ZoneControlCaptureGödelsCompletenessTheoremSO)
                .GetField("_bonusPerCompletion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompletion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGödelsCompletenessTheoremController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGödelsCompletenessTheoremController>();
        }

        [Test]
        public void SO_FreshInstance_ConsistentExtensions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompletionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompletionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesExtensions()
        {
            var so = CreateSO(consistentExtensionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(consistentExtensionsNeeded: 3, bonusPerCompletion: 4885);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4885));
            Assert.That(so.CompletionCount,   Is.EqualTo(1));
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(consistentExtensionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesIncompleteAxiomatizations()
        {
            var so = CreateSO(consistentExtensionsNeeded: 6, incompleteAxiomatizationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(consistentExtensionsNeeded: 6, incompleteAxiomatizationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ConsistentExtensionProgress_Clamped()
        {
            var so = CreateSO(consistentExtensionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ConsistentExtensionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGödelsCompletenessTheoremCompleted_FiresEvent()
        {
            var so    = CreateSO(consistentExtensionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGödelsCompletenessTheoremSO)
                .GetField("_onGödelsCompletenessTheoremCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(consistentExtensionsNeeded: 2, bonusPerCompletion: 4885);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Assert.That(so.CompletionCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompletions_Accumulate()
        {
            var so = CreateSO(consistentExtensionsNeeded: 2, bonusPerCompletion: 4885);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompletionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9770));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GödelsCompletenessSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GödelsCompletenessSO, Is.Null);
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
            typeof(ZoneControlCaptureGödelsCompletenessTheoremController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureWeilConjecturesTests
    {
        private static ZoneControlCaptureWeilConjecturesSO CreateSO(
            int zetaTermsNeeded        = 6,
            int counterexamplesPerBot  = 1,
            int bonusPerVerification   = 4510)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureWeilConjecturesSO>();
            typeof(ZoneControlCaptureWeilConjecturesSO)
                .GetField("_zetaTermsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, zetaTermsNeeded);
            typeof(ZoneControlCaptureWeilConjecturesSO)
                .GetField("_counterexamplesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, counterexamplesPerBot);
            typeof(ZoneControlCaptureWeilConjecturesSO)
                .GetField("_bonusPerVerification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerVerification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureWeilConjecturesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureWeilConjecturesController>();
        }

        [Test]
        public void SO_FreshInstance_ZetaTerms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ZetaTerms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_VerificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VerificationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesZetaTerms()
        {
            var so = CreateSO(zetaTermsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ZetaTerms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(zetaTermsNeeded: 3, bonusPerVerification: 4510);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4510));
            Assert.That(so.VerificationCount, Is.EqualTo(1));
            Assert.That(so.ZetaTerms,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(zetaTermsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesZetaTerms()
        {
            var so = CreateSO(zetaTermsNeeded: 6, counterexamplesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ZetaTerms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(zetaTermsNeeded: 6, counterexamplesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ZetaTerms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ZetaTermProgress_Clamped()
        {
            var so = CreateSO(zetaTermsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ZetaTermProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnWeilConjecturesVerified_FiresEvent()
        {
            var so    = CreateSO(zetaTermsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureWeilConjecturesSO)
                .GetField("_onWeilConjecturesVerified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(zetaTermsNeeded: 2, bonusPerVerification: 4510);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ZetaTerms,         Is.EqualTo(0));
            Assert.That(so.VerificationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleVerifications_Accumulate()
        {
            var so = CreateSO(zetaTermsNeeded: 2, bonusPerVerification: 4510);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.VerificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9020));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_WeilConjecturesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.WeilConjecturesSO, Is.Null);
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
            typeof(ZoneControlCaptureWeilConjecturesController)
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

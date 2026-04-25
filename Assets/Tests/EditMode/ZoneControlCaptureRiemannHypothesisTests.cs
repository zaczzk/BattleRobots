using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRiemannHypothesisTests
    {
        private static ZoneControlCaptureRiemannHypothesisSO CreateSO(
            int verifiedZerosNeeded    = 5,
            int offLineDeviationsPerBot = 2,
            int bonusPerVerification   = 4555)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRiemannHypothesisSO>();
            typeof(ZoneControlCaptureRiemannHypothesisSO)
                .GetField("_verifiedZerosNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, verifiedZerosNeeded);
            typeof(ZoneControlCaptureRiemannHypothesisSO)
                .GetField("_offLineDeviationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, offLineDeviationsPerBot);
            typeof(ZoneControlCaptureRiemannHypothesisSO)
                .GetField("_bonusPerVerification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerVerification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRiemannHypothesisController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRiemannHypothesisController>();
        }

        [Test]
        public void SO_FreshInstance_VerifiedZeros_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VerifiedZeros, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesVerifiedZeros()
        {
            var so = CreateSO(verifiedZerosNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.VerifiedZeros, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(verifiedZerosNeeded: 3, bonusPerVerification: 4555);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4555));
            Assert.That(so.VerificationCount, Is.EqualTo(1));
            Assert.That(so.VerifiedZeros,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(verifiedZerosNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesOffLineDeviations()
        {
            var so = CreateSO(verifiedZerosNeeded: 5, offLineDeviationsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VerifiedZeros, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(verifiedZerosNeeded: 5, offLineDeviationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VerifiedZeros, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VerifiedZeroProgress_Clamped()
        {
            var so = CreateSO(verifiedZerosNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.VerifiedZeroProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRiemannHypothesisVerified_FiresEvent()
        {
            var so    = CreateSO(verifiedZerosNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRiemannHypothesisSO)
                .GetField("_onRiemannHypothesisVerified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(verifiedZerosNeeded: 2, bonusPerVerification: 4555);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.VerifiedZeros,     Is.EqualTo(0));
            Assert.That(so.VerificationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleVerifications_Accumulate()
        {
            var so = CreateSO(verifiedZerosNeeded: 2, bonusPerVerification: 4555);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.VerificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9110));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RiemannHypothesisSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RiemannHypothesisSO, Is.Null);
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
            typeof(ZoneControlCaptureRiemannHypothesisController)
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

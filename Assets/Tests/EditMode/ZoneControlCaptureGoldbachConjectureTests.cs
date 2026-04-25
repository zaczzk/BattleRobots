using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGoldbachConjectureTests
    {
        private static ZoneControlCaptureGoldbachConjectureSO CreateSO(
            int primePairsNeeded            = 6,
            int compositeObstructionsPerBot = 1,
            int bonusPerVerification        = 4705)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGoldbachConjectureSO>();
            typeof(ZoneControlCaptureGoldbachConjectureSO)
                .GetField("_primePairsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, primePairsNeeded);
            typeof(ZoneControlCaptureGoldbachConjectureSO)
                .GetField("_compositeObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, compositeObstructionsPerBot);
            typeof(ZoneControlCaptureGoldbachConjectureSO)
                .GetField("_bonusPerVerification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerVerification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGoldbachConjectureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGoldbachConjectureController>();
        }

        [Test]
        public void SO_FreshInstance_PrimePairs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PrimePairs, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesPrimePairs()
        {
            var so = CreateSO(primePairsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PrimePairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(primePairsNeeded: 3, bonusPerVerification: 4705);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4705));
            Assert.That(so.VerificationCount,   Is.EqualTo(1));
            Assert.That(so.PrimePairs,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(primePairsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesPrimePairs()
        {
            var so = CreateSO(primePairsNeeded: 6, compositeObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PrimePairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(primePairsNeeded: 6, compositeObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PrimePairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PrimePairProgress_Clamped()
        {
            var so = CreateSO(primePairsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PrimePairProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGoldbachConjectureVerified_FiresEvent()
        {
            var so    = CreateSO(primePairsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGoldbachConjectureSO)
                .GetField("_onGoldbachConjectureVerified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(primePairsNeeded: 2, bonusPerVerification: 4705);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PrimePairs,          Is.EqualTo(0));
            Assert.That(so.VerificationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleVerifications_Accumulate()
        {
            var so = CreateSO(primePairsNeeded: 2, bonusPerVerification: 4705);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.VerificationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(9410));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GoldbachSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GoldbachSO, Is.Null);
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
            typeof(ZoneControlCaptureGoldbachConjectureController)
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

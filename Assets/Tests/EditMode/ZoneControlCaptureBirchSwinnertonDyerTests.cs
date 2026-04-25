using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBirchSwinnertonDyerTests
    {
        private static ZoneControlCaptureBirchSwinnertonDyerSO CreateSO(
            int rationalPointsNeeded  = 6,
            int tshObstructionsPerBot  = 1,
            int bonusPerVerification  = 4450)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBirchSwinnertonDyerSO>();
            typeof(ZoneControlCaptureBirchSwinnertonDyerSO)
                .GetField("_rationalPointsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rationalPointsNeeded);
            typeof(ZoneControlCaptureBirchSwinnertonDyerSO)
                .GetField("_tshObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tshObstructionsPerBot);
            typeof(ZoneControlCaptureBirchSwinnertonDyerSO)
                .GetField("_bonusPerVerification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerVerification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBirchSwinnertonDyerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBirchSwinnertonDyerController>();
        }

        [Test]
        public void SO_FreshInstance_RationalPoints_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RationalPoints, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesRationalPoints()
        {
            var so = CreateSO(rationalPointsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.RationalPoints, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(rationalPointsNeeded: 3, bonusPerVerification: 4450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4450));
            Assert.That(so.VerificationCount, Is.EqualTo(1));
            Assert.That(so.RationalPoints,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(rationalPointsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTshObstructions()
        {
            var so = CreateSO(rationalPointsNeeded: 6, tshObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RationalPoints, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(rationalPointsNeeded: 6, tshObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RationalPoints, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RationalPointProgress_Clamped()
        {
            var so = CreateSO(rationalPointsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.RationalPointProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBirchSwinnertonDyerVerified_FiresEvent()
        {
            var so    = CreateSO(rationalPointsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBirchSwinnertonDyerSO)
                .GetField("_onBirchSwinnertonDyerVerified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(rationalPointsNeeded: 2, bonusPerVerification: 4450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RationalPoints,    Is.EqualTo(0));
            Assert.That(so.VerificationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleVerifications_Accumulate()
        {
            var so = CreateSO(rationalPointsNeeded: 2, bonusPerVerification: 4450);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.VerificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8900));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BirchSwinnertonDyerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BirchSwinnertonDyerSO, Is.Null);
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
            typeof(ZoneControlCaptureBirchSwinnertonDyerController)
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

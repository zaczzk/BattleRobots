using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEllipticCohomologyTests
    {
        private static ZoneControlCaptureEllipticCohomologySO CreateSO(
            int modularFormsNeeded  = 6,
            int cuspsPerBot         = 1,
            int bonusPerComputation = 4090)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEllipticCohomologySO>();
            typeof(ZoneControlCaptureEllipticCohomologySO)
                .GetField("_modularFormsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, modularFormsNeeded);
            typeof(ZoneControlCaptureEllipticCohomologySO)
                .GetField("_cuspsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cuspsPerBot);
            typeof(ZoneControlCaptureEllipticCohomologySO)
                .GetField("_bonusPerComputation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerComputation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEllipticCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEllipticCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_ModularForms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ModularForms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComputationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComputationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesModularForms()
        {
            var so = CreateSO(modularFormsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ModularForms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(modularFormsNeeded: 3, bonusPerComputation: 4090);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                 Is.EqualTo(4090));
            Assert.That(so.ComputationCount,   Is.EqualTo(1));
            Assert.That(so.ModularForms,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(modularFormsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCusps()
        {
            var so = CreateSO(modularFormsNeeded: 6, cuspsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ModularForms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(modularFormsNeeded: 6, cuspsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ModularForms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ModularFormProgress_Clamped()
        {
            var so = CreateSO(modularFormsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ModularFormProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEllipticCohomologyComputed_FiresEvent()
        {
            var so    = CreateSO(modularFormsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEllipticCohomologySO)
                .GetField("_onEllipticCohomologyComputed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(modularFormsNeeded: 2, bonusPerComputation: 4090);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ModularForms,       Is.EqualTo(0));
            Assert.That(so.ComputationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleComputations_Accumulate()
        {
            var so = CreateSO(modularFormsNeeded: 2, bonusPerComputation: 4090);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComputationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8180));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EllipticCohomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EllipticCohomologySO, Is.Null);
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
            typeof(ZoneControlCaptureEllipticCohomologyController)
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

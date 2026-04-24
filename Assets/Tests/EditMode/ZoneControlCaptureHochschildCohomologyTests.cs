using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHochschildCohomologyTests
    {
        private static ZoneControlCaptureHochschildCohomologySO CreateSO(
            int deformationsNeeded  = 5,
            int obstructionPerBot   = 1,
            int bonusPerDeformation = 3895)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHochschildCohomologySO>();
            typeof(ZoneControlCaptureHochschildCohomologySO)
                .GetField("_deformationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, deformationsNeeded);
            typeof(ZoneControlCaptureHochschildCohomologySO)
                .GetField("_obstructionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, obstructionPerBot);
            typeof(ZoneControlCaptureHochschildCohomologySO)
                .GetField("_bonusPerDeformation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDeformation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHochschildCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHochschildCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Deformations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Deformations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DeformCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DeformCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDeformations()
        {
            var so = CreateSO(deformationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Deformations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(deformationsNeeded: 3, bonusPerDeformation: 3895);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3895));
            Assert.That(so.DeformCount,  Is.EqualTo(1));
            Assert.That(so.Deformations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(deformationsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesObstructions()
        {
            var so = CreateSO(deformationsNeeded: 5, obstructionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Deformations, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(deformationsNeeded: 5, obstructionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Deformations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DeformationProgress_Clamped()
        {
            var so = CreateSO(deformationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.DeformationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHochschildCohomologyDeformed_FiresEvent()
        {
            var so    = CreateSO(deformationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHochschildCohomologySO)
                .GetField("_onHochschildCohomologyDeformed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(deformationsNeeded: 2, bonusPerDeformation: 3895);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Deformations,      Is.EqualTo(0));
            Assert.That(so.DeformCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDeformations_Accumulate()
        {
            var so = CreateSO(deformationsNeeded: 2, bonusPerDeformation: 3895);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DeformCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7790));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HochschildSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HochschildSO, Is.Null);
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
            typeof(ZoneControlCaptureHochschildCohomologyController)
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

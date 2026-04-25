using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFormalModuliTests
    {
        private static ZoneControlCaptureFormalModuliSO CreateSO(
            int deformationsNeeded    = 5,
            int obstructionsPerBot    = 1,
            int bonusPerClassification = 4195)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFormalModuliSO>();
            typeof(ZoneControlCaptureFormalModuliSO)
                .GetField("_deformationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, deformationsNeeded);
            typeof(ZoneControlCaptureFormalModuliSO)
                .GetField("_obstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, obstructionsPerBot);
            typeof(ZoneControlCaptureFormalModuliSO)
                .GetField("_bonusPerClassification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClassification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFormalModuliController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFormalModuliController>();
        }

        [Test]
        public void SO_FreshInstance_Deformations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Deformations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClassificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClassificationCount, Is.EqualTo(0));
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
            var so    = CreateSO(deformationsNeeded: 3, bonusPerClassification: 4195);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(4195));
            Assert.That(so.ClassificationCount, Is.EqualTo(1));
            Assert.That(so.Deformations,        Is.EqualTo(0));
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
            var so = CreateSO(deformationsNeeded: 5, obstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Deformations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(deformationsNeeded: 5, obstructionsPerBot: 10);
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
        public void SO_OnFormalModuliClassified_FiresEvent()
        {
            var so    = CreateSO(deformationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFormalModuliSO)
                .GetField("_onFormalModuliClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(deformationsNeeded: 2, bonusPerClassification: 4195);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Deformations,        Is.EqualTo(0));
            Assert.That(so.ClassificationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClassifications_Accumulate()
        {
            var so = CreateSO(deformationsNeeded: 2, bonusPerClassification: 4195);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClassificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(8390));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FormalModuliSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FormalModuliSO, Is.Null);
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
            typeof(ZoneControlCaptureFormalModuliController)
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

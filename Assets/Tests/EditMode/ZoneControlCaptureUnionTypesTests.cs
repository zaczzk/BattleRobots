using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureUnionTypesTests
    {
        private static ZoneControlCaptureUnionTypesSO CreateSO(
            int variantsNeeded             = 6,
            int discriminantErasuresPerBot = 1,
            int bonusPerUnionElimination   = 5215)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureUnionTypesSO>();
            typeof(ZoneControlCaptureUnionTypesSO)
                .GetField("_variantsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, variantsNeeded);
            typeof(ZoneControlCaptureUnionTypesSO)
                .GetField("_discriminantErasuresPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, discriminantErasuresPerBot);
            typeof(ZoneControlCaptureUnionTypesSO)
                .GetField("_bonusPerUnionElimination", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerUnionElimination);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureUnionTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureUnionTypesController>();
        }

        [Test]
        public void SO_FreshInstance_Variants_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Variants, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_UnionEliminationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UnionEliminationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesVariants()
        {
            var so = CreateSO(variantsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Variants, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(variantsNeeded: 3, bonusPerUnionElimination: 5215);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(5215));
            Assert.That(so.UnionEliminationCount, Is.EqualTo(1));
            Assert.That(so.Variants,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(variantsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDiscriminantErasures()
        {
            var so = CreateSO(variantsNeeded: 6, discriminantErasuresPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Variants, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(variantsNeeded: 6, discriminantErasuresPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Variants, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VariantProgress_Clamped()
        {
            var so = CreateSO(variantsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.VariantProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnUnionTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(variantsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureUnionTypesSO)
                .GetField("_onUnionTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(variantsNeeded: 2, bonusPerUnionElimination: 5215);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Variants,             Is.EqualTo(0));
            Assert.That(so.UnionEliminationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEliminations_Accumulate()
        {
            var so = CreateSO(variantsNeeded: 2, bonusPerUnionElimination: 5215);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.UnionEliminationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(10430));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_UnionTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.UnionTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureUnionTypesController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBorderingGroupTests
    {
        private static ZoneControlCaptureBorderingGroupSO CreateSO(
            int manifoldsNeeded        = 7,
            int boundaryPerBot         = 2,
            int bonusPerClassification = 4030)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBorderingGroupSO>();
            typeof(ZoneControlCaptureBorderingGroupSO)
                .GetField("_manifoldsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, manifoldsNeeded);
            typeof(ZoneControlCaptureBorderingGroupSO)
                .GetField("_boundaryPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, boundaryPerBot);
            typeof(ZoneControlCaptureBorderingGroupSO)
                .GetField("_bonusPerClassification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClassification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBorderingGroupController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBorderingGroupController>();
        }

        [Test]
        public void SO_FreshInstance_Manifolds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Manifolds, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesManifolds()
        {
            var so = CreateSO(manifoldsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Manifolds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(manifoldsNeeded: 3, bonusPerClassification: 4030);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(4030));
            Assert.That(so.ClassificationCount,  Is.EqualTo(1));
            Assert.That(so.Manifolds,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(manifoldsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesBoundaries()
        {
            var so = CreateSO(manifoldsNeeded: 7, boundaryPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Manifolds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(manifoldsNeeded: 7, boundaryPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Manifolds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ManifoldProgress_Clamped()
        {
            var so = CreateSO(manifoldsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ManifoldProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBorderingGroupClassified_FiresEvent()
        {
            var so    = CreateSO(manifoldsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBorderingGroupSO)
                .GetField("_onBorderingGroupClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(manifoldsNeeded: 2, bonusPerClassification: 4030);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Manifolds,            Is.EqualTo(0));
            Assert.That(so.ClassificationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClassifications_Accumulate()
        {
            var so = CreateSO(manifoldsNeeded: 2, bonusPerClassification: 4030);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClassificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(8060));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BorderingGroupSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BorderingGroupSO, Is.Null);
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
            typeof(ZoneControlCaptureBorderingGroupController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGroupCohomologyTests
    {
        private static ZoneControlCaptureGroupCohomologySO CreateSO(
            int cocyclesNeeded         = 5,
            int coboundaryPerBot       = 1,
            int bonusPerClassification = 3865)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGroupCohomologySO>();
            typeof(ZoneControlCaptureGroupCohomologySO)
                .GetField("_cocyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cocyclesNeeded);
            typeof(ZoneControlCaptureGroupCohomologySO)
                .GetField("_coboundaryPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coboundaryPerBot);
            typeof(ZoneControlCaptureGroupCohomologySO)
                .GetField("_bonusPerClassification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClassification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGroupCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGroupCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Cocycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cocycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClassifyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClassifyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCocycles()
        {
            var so = CreateSO(cocyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Cocycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cocyclesNeeded: 3, bonusPerClassification: 3865);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(3865));
            Assert.That(so.ClassifyCount,  Is.EqualTo(1));
            Assert.That(so.Cocycles,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cocyclesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCoboundaries()
        {
            var so = CreateSO(cocyclesNeeded: 5, coboundaryPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cocycles, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cocyclesNeeded: 5, coboundaryPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cocycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CocycleProgress_Clamped()
        {
            var so = CreateSO(cocyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CocycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGroupCohomologyClassified_FiresEvent()
        {
            var so    = CreateSO(cocyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGroupCohomologySO)
                .GetField("_onGroupCohomologyClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cocyclesNeeded: 2, bonusPerClassification: 3865);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cocycles,          Is.EqualTo(0));
            Assert.That(so.ClassifyCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClassifications_Accumulate()
        {
            var so = CreateSO(cocyclesNeeded: 2, bonusPerClassification: 3865);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClassifyCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7730));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GroupCohomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GroupCohomologySO, Is.Null);
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
            typeof(ZoneControlCaptureGroupCohomologyController)
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

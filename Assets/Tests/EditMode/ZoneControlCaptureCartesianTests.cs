using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCartesianTests
    {
        private static ZoneControlCaptureCartesianSO CreateSO(
            int projectionsNeeded   = 7,
            int deletePerBot        = 2,
            int bonusPerDiagonalize = 3130)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCartesianSO>();
            typeof(ZoneControlCaptureCartesianSO)
                .GetField("_projectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, projectionsNeeded);
            typeof(ZoneControlCaptureCartesianSO)
                .GetField("_deletePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, deletePerBot);
            typeof(ZoneControlCaptureCartesianSO)
                .GetField("_bonusPerDiagonalize", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDiagonalize);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCartesianController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCartesianController>();
        }

        [Test]
        public void SO_FreshInstance_Projections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Projections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DiagonalizeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DiagonalizeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesProjections()
        {
            var so = CreateSO(projectionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Projections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(projectionsNeeded: 3, bonusPerDiagonalize: 3130);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(3130));
            Assert.That(so.DiagonalizeCount,  Is.EqualTo(1));
            Assert.That(so.Projections,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(projectionsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesProjections()
        {
            var so = CreateSO(projectionsNeeded: 7, deletePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Projections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(projectionsNeeded: 7, deletePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Projections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ProjectionProgress_Clamped()
        {
            var so = CreateSO(projectionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ProjectionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDiagonalized_FiresEvent()
        {
            var so    = CreateSO(projectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCartesianSO)
                .GetField("_onDiagonalized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(projectionsNeeded: 2, bonusPerDiagonalize: 3130);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Projections,       Is.EqualTo(0));
            Assert.That(so.DiagonalizeCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDiagonalizations_Accumulate()
        {
            var so = CreateSO(projectionsNeeded: 2, bonusPerDiagonalize: 3130);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DiagonalizeCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6260));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CartesianSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CartesianSO, Is.Null);
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
            typeof(ZoneControlCaptureCartesianController)
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

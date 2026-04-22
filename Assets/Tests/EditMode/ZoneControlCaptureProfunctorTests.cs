using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureProfunctorTests
    {
        private static ZoneControlCaptureProfunctorSO CreateSO(
            int projectionsNeeded = 6,
            int invertPerBot      = 2,
            int bonusPerDimap     = 2320)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureProfunctorSO>();
            typeof(ZoneControlCaptureProfunctorSO)
                .GetField("_projectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, projectionsNeeded);
            typeof(ZoneControlCaptureProfunctorSO)
                .GetField("_invertPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, invertPerBot);
            typeof(ZoneControlCaptureProfunctorSO)
                .GetField("_bonusPerDimap", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDimap);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureProfunctorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureProfunctorController>();
        }

        [Test]
        public void SO_FreshInstance_Projections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Projections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DimapCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DimapCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesProjections()
        {
            var so = CreateSO(projectionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Projections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(projectionsNeeded: 3, bonusPerDimap: 2320);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2320));
            Assert.That(so.DimapCount,  Is.EqualTo(1));
            Assert.That(so.Projections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(projectionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesProjections()
        {
            var so = CreateSO(projectionsNeeded: 6, invertPerBot: 2);
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
            var so = CreateSO(projectionsNeeded: 6, invertPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Projections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ProjectionProgress_Clamped()
        {
            var so = CreateSO(projectionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ProjectionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnProfunctorDimapped_FiresEvent()
        {
            var so    = CreateSO(projectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureProfunctorSO)
                .GetField("_onProfunctorDimapped", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(projectionsNeeded: 2, bonusPerDimap: 2320);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Projections,       Is.EqualTo(0));
            Assert.That(so.DimapCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDimaps_Accumulate()
        {
            var so = CreateSO(projectionsNeeded: 2, bonusPerDimap: 2320);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DimapCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4640));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ProfunctorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ProfunctorSO, Is.Null);
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
            typeof(ZoneControlCaptureProfunctorController)
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

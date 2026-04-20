using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePillarTests
    {
        private static ZoneControlCapturePillarSO CreateSO(
            int capturesPerPillar = 4,
            int maxPillars        = 4,
            int bonusPerCapture   = 80)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePillarSO>();
            typeof(ZoneControlCapturePillarSO)
                .GetField("_capturesPerPillar", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerPillar);
            typeof(ZoneControlCapturePillarSO)
                .GetField("_maxPillars",        BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxPillars);
            typeof(ZoneControlCapturePillarSO)
                .GetField("_bonusPerCapture",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePillarController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePillarController>();
        }

        [Test]
        public void SO_FreshInstance_PillarCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PillarCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BuildCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BuildCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BuildsTowardPillar()
        {
            var so = CreateSO(capturesPerPillar: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BuildCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ErectsPillarAtThreshold()
        {
            var so = CreateSO(capturesPerPillar: 3, maxPillars: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PillarCount, Is.EqualTo(1));
            Assert.That(so.BuildCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenComplete_ReturnsBonus()
        {
            var so = CreateSO(capturesPerPillar: 1, maxPillars: 2, bonusPerCapture: 80);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(80));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenBuilding_ReturnsZero()
        {
            var so    = CreateSO(capturesPerPillar: 4, maxPillars: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenPillarsUp_TopplesPillar()
        {
            var so = CreateSO(capturesPerPillar: 1, maxPillars: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PillarCount, Is.EqualTo(1));
            Assert.That(so.BuildCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenBuilding_ReducesBuild()
        {
            var so = CreateSO(capturesPerPillar: 4, maxPillars: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BuildProgress_Clamped()
        {
            var so = CreateSO(capturesPerPillar: 4, maxPillars: 4);
            Assert.That(so.BuildProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.BuildProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnStructureComplete_FiresEvent()
        {
            var so    = CreateSO(capturesPerPillar: 1, maxPillars: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePillarSO)
                .GetField("_onStructureComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(capturesPerPillar: 1, maxPillars: 2, bonusPerCapture: 80);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.BuildCount,        Is.EqualTo(0));
            Assert.That(so.PillarCount,       Is.EqualTo(0));
            Assert.That(so.BonusCaptureCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCaptures_WhileComplete_AccumulateBonus()
        {
            var so = CreateSO(capturesPerPillar: 1, maxPillars: 2, bonusPerCapture: 80);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BonusCaptureCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(160));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PillarSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PillarSO, Is.Null);
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
            typeof(ZoneControlCapturePillarController)
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

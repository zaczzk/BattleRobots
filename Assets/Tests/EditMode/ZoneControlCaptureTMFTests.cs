using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTMFTests
    {
        private static ZoneControlCaptureTMFSO CreateSO(
            int levelStructuresNeeded = 6,
            int cuspsPerBot           = 1,
            int bonusPerResolution    = 4105)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTMFSO>();
            typeof(ZoneControlCaptureTMFSO)
                .GetField("_levelStructuresNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, levelStructuresNeeded);
            typeof(ZoneControlCaptureTMFSO)
                .GetField("_cuspsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cuspsPerBot);
            typeof(ZoneControlCaptureTMFSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTMFController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTMFController>();
        }

        [Test]
        public void SO_FreshInstance_LevelStructures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LevelStructures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResolutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResolutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLevelStructures()
        {
            var so = CreateSO(levelStructuresNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LevelStructures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(levelStructuresNeeded: 3, bonusPerResolution: 4105);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4105));
            Assert.That(so.ResolutionCount,  Is.EqualTo(1));
            Assert.That(so.LevelStructures,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(levelStructuresNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCusps()
        {
            var so = CreateSO(levelStructuresNeeded: 6, cuspsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LevelStructures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(levelStructuresNeeded: 6, cuspsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LevelStructures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LevelStructureProgress_Clamped()
        {
            var so = CreateSO(levelStructuresNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LevelStructureProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTMFResolved_FiresEvent()
        {
            var so    = CreateSO(levelStructuresNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTMFSO)
                .GetField("_onTMFResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(levelStructuresNeeded: 2, bonusPerResolution: 4105);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.LevelStructures,   Is.EqualTo(0));
            Assert.That(so.ResolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(levelStructuresNeeded: 2, bonusPerResolution: 4105);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8210));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TMFSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TMFSO, Is.Null);
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
            typeof(ZoneControlCaptureTMFController)
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

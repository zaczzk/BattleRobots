using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCatapultTests
    {
        private static ZoneControlCaptureCatapultSO CreateSO(
            int projectilesPerLaunch = 6,
            int unloadPerBot         = 2,
            int bonusPerLaunch       = 420)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCatapultSO>();
            typeof(ZoneControlCaptureCatapultSO)
                .GetField("_projectilesPerLaunch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, projectilesPerLaunch);
            typeof(ZoneControlCaptureCatapultSO)
                .GetField("_unloadPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unloadPerBot);
            typeof(ZoneControlCaptureCatapultSO)
                .GetField("_bonusPerLaunch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLaunch);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCatapultController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCatapultController>();
        }

        [Test]
        public void SO_FreshInstance_LoadedProjectiles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LoadedProjectiles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LaunchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LaunchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_LoadsProjectile()
        {
            var so = CreateSO(projectilesPerLaunch: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LoadedProjectiles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_LaunchesAtThreshold()
        {
            var so    = CreateSO(projectilesPerLaunch: 3, bonusPerLaunch: 420);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(420));
            Assert.That(so.LaunchCount,      Is.EqualTo(1));
            Assert.That(so.LoadedProjectiles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileLoading()
        {
            var so    = CreateSO(projectilesPerLaunch: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_UnloadsProjectiles()
        {
            var so = CreateSO(projectilesPerLaunch: 6, unloadPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LoadedProjectiles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(projectilesPerLaunch: 6, unloadPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LoadedProjectiles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoadProgress_Clamped()
        {
            var so = CreateSO(projectilesPerLaunch: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LoadProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLaunch_FiresEvent()
        {
            var so    = CreateSO(projectilesPerLaunch: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCatapultSO)
                .GetField("_onLaunch", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(projectilesPerLaunch: 2, bonusPerLaunch: 420);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.LoadedProjectiles, Is.EqualTo(0));
            Assert.That(so.LaunchCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLaunches_Accumulate()
        {
            var so = CreateSO(projectilesPerLaunch: 2, bonusPerLaunch: 420);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LaunchCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(840));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CatapultSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CatapultSO, Is.Null);
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
            typeof(ZoneControlCaptureCatapultController)
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

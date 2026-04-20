using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLockboxTests
    {
        private static ZoneControlCaptureLockboxSO CreateSO(
            int startingLocks = 5,
            int maxLocks      = 8,
            int bonusPerOpen  = 350)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLockboxSO>();
            typeof(ZoneControlCaptureLockboxSO)
                .GetField("_startingLocks", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, startingLocks);
            typeof(ZoneControlCaptureLockboxSO)
                .GetField("_maxLocks",      BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxLocks);
            typeof(ZoneControlCaptureLockboxSO)
                .GetField("_bonusPerOpen",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerOpen);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLockboxController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLockboxController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentLocks_EqualsStartingLocks()
        {
            var so = CreateSO(startingLocks: 5);
            Assert.That(so.CurrentLocks, Is.EqualTo(5));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_OpenCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OpenCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PicksOneLock()
        {
            var so = CreateSO(startingLocks: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentLocks, Is.EqualTo(4));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OpensAtZeroLocks()
        {
            var so    = CreateSO(startingLocks: 2, bonusPerOpen: 350);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(350));
            Assert.That(so.OpenCount,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsLocksAfterOpen()
        {
            var so = CreateSO(startingLocks: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentLocks, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhenBuilding()
        {
            var so    = CreateSO(startingLocks: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AddsOneLock()
        {
            var so = CreateSO(startingLocks: 3, maxLocks: 8);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentLocks, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToMaxLocks()
        {
            var so = CreateSO(startingLocks: 3, maxLocks: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentLocks, Is.EqualTo(4));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LockProgress_Clamped()
        {
            var so = CreateSO(startingLocks: 5);
            Assert.That(so.LockProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.LockProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLockboxOpened_FiresEvent()
        {
            var so    = CreateSO(startingLocks: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLockboxSO)
                .GetField("_onLockboxOpened", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(startingLocks: 3, bonusPerOpen: 350);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentLocks,      Is.EqualTo(3));
            Assert.That(so.OpenCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleOpenings_Accumulate()
        {
            var so = CreateSO(startingLocks: 2, bonusPerOpen: 350);
            for (int i = 0; i < 4; i++) so.RecordPlayerCapture();
            Assert.That(so.OpenCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(700));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LockboxSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LockboxSO, Is.Null);
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
            typeof(ZoneControlCaptureLockboxController)
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

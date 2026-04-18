using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T415: <see cref="ZoneControlLoseStreakTrackerSO"/> and
    /// <see cref="ZoneControlLoseStreakTrackerController"/>.
    ///
    /// ZoneControlLoseStreakTrackerTests (12):
    ///   SO_FreshInstance_LoseStreak_Zero                    x1
    ///   SO_FreshInstance_IsWarning_False                    x1
    ///   SO_RecordBotCapture_BelowThreshold_NoFire           x1
    ///   SO_RecordBotCapture_MeetsThreshold_FiresWarning     x1
    ///   SO_RecordBotCapture_MeetsThreshold_SetsWarning      x1
    ///   SO_RecordBotCapture_AfterWarning_Idempotent         x1
    ///   SO_RecordPlayerCapture_ResetsStreak                 x1
    ///   SO_RecordPlayerCapture_WhenZero_NoFire              x1
    ///   SO_Reset_ClearsAll                                  x1
    ///   Controller_FreshInstance_TrackerSO_Null             x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow           x1
    ///   Controller_Refresh_NullSO_HidesPanel                x1
    /// </summary>
    public sealed class ZoneControlLoseStreakTrackerTests
    {
        private static ZoneControlLoseStreakTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlLoseStreakTrackerSO>();

        private static ZoneControlLoseStreakTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlLoseStreakTrackerController>();
        }

        [Test]
        public void SO_FreshInstance_LoseStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LoseStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsWarning_False()
        {
            var so = CreateSO();
            Assert.That(so.IsWarning, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BelowThreshold_NoFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlLoseStreakTrackerSO)
                .GetField("_onLoseStreakWarning", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int below = so.WarningThreshold - 1;
            for (int i = 0; i < below; i++)
                so.RecordBotCapture();

            Assert.That(fired,        Is.EqualTo(0));
            Assert.That(so.IsWarning, Is.False);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotCapture_MeetsThreshold_FiresWarning()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlLoseStreakTrackerSO)
                .GetField("_onLoseStreakWarning", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.WarningThreshold; i++)
                so.RecordBotCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotCapture_MeetsThreshold_SetsWarning()
        {
            var so = CreateSO();
            for (int i = 0; i < so.WarningThreshold; i++)
                so.RecordBotCapture();
            Assert.That(so.IsWarning, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AfterWarning_Idempotent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlLoseStreakTrackerSO)
                .GetField("_onLoseStreakWarning", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.WarningThreshold + 3; i++)
                so.RecordBotCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsStreak()
        {
            var so = CreateSO();
            for (int i = 0; i < so.WarningThreshold; i++)
                so.RecordBotCapture();

            so.RecordPlayerCapture();

            Assert.That(so.LoseStreak, Is.EqualTo(0));
            Assert.That(so.IsWarning,  Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenZero_NoFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlLoseStreakTrackerSO)
                .GetField("_onLoseStreakReset", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture();

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.WarningThreshold; i++)
                so.RecordBotCapture();
            so.Reset();
            Assert.That(so.LoseStreak, Is.EqualTo(0));
            Assert.That(so.IsWarning,  Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrackerSO, Is.Null);
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
            typeof(ZoneControlLoseStreakTrackerController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T411: <see cref="ZoneControlMatchLongevityBonusSO"/> and
    /// <see cref="ZoneControlMatchLongevityBonusController"/>.
    ///
    /// ZoneControlMatchLongevityBonusTests (12):
    ///   SO_FreshInstance_IsRunning_False                x1
    ///   SO_FreshInstance_ElapsedTime_Zero               x1
    ///   SO_FreshInstance_IntervalsCompleted_Zero        x1
    ///   SO_Tick_WhenNotRunning_DoesNotAdvance           x1
    ///   SO_StartTracking_Idempotent                     x1
    ///   SO_Tick_CompletesInterval                       x1
    ///   SO_Tick_MultiInterval                           x1
    ///   SO_Tick_FiresEvent                              x1
    ///   SO_StopTracking_DisarmsTimer                    x1
    ///   SO_Reset_ClearsAll                              x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow       x1
    ///   Controller_Refresh_NullSO_HidesPanel            x1
    /// </summary>
    public sealed class ZoneControlMatchLongevityBonusTests
    {
        private static ZoneControlMatchLongevityBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchLongevityBonusSO>();

        private static ZoneControlMatchLongevityBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchLongevityBonusController>();
        }

        [Test]
        public void SO_FreshInstance_IsRunning_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRunning, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ElapsedTime_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ElapsedTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IntervalsCompleted_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhenNotRunning_DoesNotAdvance()
        {
            var so = CreateSO();
            so.Tick(100f);
            Assert.That(so.ElapsedTime,        Is.EqualTo(0f));
            Assert.That(so.IntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartTracking_Idempotent()
        {
            var so = CreateSO();
            so.StartTracking();
            so.StartTracking();
            Assert.That(so.IsRunning, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_CompletesInterval()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.IntervalSeconds);
            Assert.That(so.IntervalsCompleted, Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(so.BonusPerInterval));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_MultiInterval()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.IntervalSeconds * 3f);
            Assert.That(so.IntervalsCompleted, Is.EqualTo(3));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(so.BonusPerInterval * 3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchLongevityBonusSO)
                .GetField("_onLongevityBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.StartTracking();
            so.Tick(so.IntervalSeconds);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_StopTracking_DisarmsTimer()
        {
            var so = CreateSO();
            so.StartTracking();
            so.StopTracking();
            so.Tick(so.IntervalSeconds * 5f);
            Assert.That(so.IntervalsCompleted, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartTracking();
            so.Tick(so.IntervalSeconds * 2f);
            so.Reset();
            Assert.That(so.ElapsedTime,        Is.EqualTo(0f));
            Assert.That(so.IntervalsCompleted, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Assert.That(so.IsRunning,          Is.False);
            Object.DestroyImmediate(so);
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
            typeof(ZoneControlMatchLongevityBonusController)
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

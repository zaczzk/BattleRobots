using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T424: <see cref="ZoneControlDominanceTimerSO"/> and
    /// <see cref="ZoneControlDominanceTimerController"/>.
    ///
    /// ZoneControlDominanceTimerTests (12):
    ///   SO_FreshInstance_IsDominating_False                         x1
    ///   SO_FreshInstance_TotalDominanceTime_Zero                    x1
    ///   SO_StartDominance_SetsIsDominating_True                     x1
    ///   SO_EndDominance_ClearsIsDominating                          x1
    ///   SO_Tick_WhenNotDominating_DoesNotAccumulate                 x1
    ///   SO_Tick_WhenDominating_AccumulatesTime                      x1
    ///   SO_Tick_FiresIntervalEvent                                  x1
    ///   SO_Tick_MultipleIntervals_MultipleEvents                    x1
    ///   SO_DominanceProgress_AfterHalfInterval                      x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_DominanceTimerSO_Null              x1
    ///   Controller_Refresh_NullSO_HidesPanel                        x1
    /// </summary>
    public sealed class ZoneControlDominanceTimerTests
    {
        private static ZoneControlDominanceTimerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlDominanceTimerSO>();

        private static ZoneControlDominanceTimerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlDominanceTimerController>();
        }

        [Test]
        public void SO_FreshInstance_IsDominating_False()
        {
            var so = CreateSO();
            Assert.That(so.IsDominating, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalDominanceTime_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalDominanceTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartDominance_SetsIsDominating_True()
        {
            var so = CreateSO();
            so.StartDominance();
            Assert.That(so.IsDominating, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndDominance_ClearsIsDominating()
        {
            var so = CreateSO();
            so.StartDominance();
            so.EndDominance();
            Assert.That(so.IsDominating, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhenNotDominating_DoesNotAccumulate()
        {
            var so = CreateSO();
            so.Tick(5f);
            Assert.That(so.TotalDominanceTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhenDominating_AccumulatesTime()
        {
            var so = CreateSO();
            so.StartDominance();
            so.Tick(3f);
            so.Tick(4f);
            Assert.That(so.TotalDominanceTime, Is.EqualTo(7f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresIntervalEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlDominanceTimerSO)
                .GetField("_onDominanceInterval", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlDominanceTimerSO)
                .GetField("_bonusInterval", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 10f);
            so.Reset(); // re-init _nextMilestone with new value

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.StartDominance();
            so.Tick(11f); // crosses one 10s milestone

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_MultipleIntervals_MultipleEvents()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlDominanceTimerSO)
                .GetField("_onDominanceInterval", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlDominanceTimerSO)
                .GetField("_bonusInterval", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 5f);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.StartDominance();
            so.Tick(12f); // crosses 2 intervals (5s, 10s)

            Assert.That(fired, Is.EqualTo(2));
            Assert.That(so.IntervalsCompleted, Is.EqualTo(2));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_DominanceProgress_AfterHalfInterval()
        {
            var so = CreateSO();
            typeof(ZoneControlDominanceTimerSO)
                .GetField("_bonusInterval", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 10f);
            so.Reset();

            so.StartDominance();
            so.Tick(5f); // halfway through first 10s interval

            Assert.That(so.DominanceProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartDominance();
            so.Tick(20f);
            so.Reset();
            Assert.That(so.IsDominating,       Is.False);
            Assert.That(so.TotalDominanceTime, Is.EqualTo(0f));
            Assert.That(so.IntervalsCompleted, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DominanceTimerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DominanceTimerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlDominanceTimerController)
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

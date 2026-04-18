using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T425: <see cref="ZoneControlZoneActivityTrackerSO"/> and
    /// <see cref="ZoneControlZoneActivityTrackerController"/>.
    ///
    /// ZoneControlZoneActivityTrackerTests (12):
    ///   SO_FreshInstance_TotalActivity_Zero                         x1
    ///   SO_FreshInstance_MilestonesReached_Zero                     x1
    ///   SO_RecordActivity_IncrementsTotalActivity                   x1
    ///   SO_RecordActivity_ReachesFirstMilestone                     x1
    ///   SO_RecordActivity_MultiMilestone_Safe                       x1
    ///   SO_RecordActivity_FiresMilestoneEvent                       x1
    ///   SO_NextMilestone_IsStepAheadOfReached                       x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   SO_MilestoneStep_DefaultIsPositive                          x1
    ///   SO_RecordActivity_BelowMilestone_NoEvent                    x1
    ///   Controller_FreshInstance_ActivityTrackerSO_Null             x1
    ///   Controller_Refresh_NullSO_HidesPanel                        x1
    /// </summary>
    public sealed class ZoneControlZoneActivityTrackerTests
    {
        private static ZoneControlZoneActivityTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneActivityTrackerSO>();

        private static ZoneControlZoneActivityTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneActivityTrackerController>();
        }

        [Test]
        public void SO_FreshInstance_TotalActivity_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalActivity, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MilestonesReached_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MilestonesReached, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordActivity_IncrementsTotalActivity()
        {
            var so = CreateSO();
            so.RecordActivity();
            so.RecordActivity();
            Assert.That(so.TotalActivity, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordActivity_ReachesFirstMilestone()
        {
            var so = CreateSO();
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_milestoneStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 3);

            so.RecordActivity();
            so.RecordActivity();
            so.RecordActivity(); // 3 = first milestone

            Assert.That(so.MilestonesReached, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordActivity_MultiMilestone_Safe()
        {
            var so = CreateSO();
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_milestoneStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 2);

            // Simulate a large delta jump: record 6 activities → crosses milestones at 2, 4, 6
            for (int i = 0; i < 6; i++) so.RecordActivity();

            Assert.That(so.MilestonesReached, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordActivity_FiresMilestoneEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_onActivityMilestone", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_milestoneStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 2);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordActivity();
            so.RecordActivity(); // crosses milestone at 2

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_NextMilestone_IsStepAheadOfReached()
        {
            var so = CreateSO();
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_milestoneStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 4);

            // Before any activity, next milestone = 1 * 4 = 4
            Assert.That(so.NextMilestone, Is.EqualTo(4));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_milestoneStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 1);

            so.RecordActivity();
            so.RecordActivity();
            so.Reset();
            Assert.That(so.TotalActivity,     Is.EqualTo(0));
            Assert.That(so.MilestonesReached, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MilestoneStep_DefaultIsPositive()
        {
            var so = CreateSO();
            Assert.That(so.MilestoneStep, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordActivity_BelowMilestone_NoEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_onActivityMilestone", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlZoneActivityTrackerSO)
                .GetField("_milestoneStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 5);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordActivity(); // only 1 of 5 needed
            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_FreshInstance_ActivityTrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ActivityTrackerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneActivityTrackerController)
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

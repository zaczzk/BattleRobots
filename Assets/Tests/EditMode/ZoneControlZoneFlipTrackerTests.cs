using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T382: <see cref="ZoneControlZoneFlipTrackerSO"/> and
    /// <see cref="ZoneControlZoneFlipTrackerController"/>.
    ///
    /// ZoneControlZoneFlipTrackerTests (12):
    ///   SO_FreshInstance_TotalFlips_Zero                         ×1
    ///   SO_FreshInstance_MilestonesReached_Zero                  ×1
    ///   SO_RecordFlip_IncrementsTotalFlips                       ×1
    ///   SO_RecordFlip_AtMilestone_IncrementsMilestonesReached    ×1
    ///   SO_RecordFlip_AtMilestone_FiresEvent                     ×1
    ///   SO_RecordFlip_MultiMilestone_HandledCorrectly            ×1
    ///   SO_NextMilestone_AdvancesAfterMilestoneReached           ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_FlipTrackerSO_Null              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_Refresh_NullFlipTrackerSO_HidesPanel          ×1
    /// </summary>
    public sealed class ZoneControlZoneFlipTrackerTests
    {
        private static ZoneControlZoneFlipTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneFlipTrackerSO>();

        private static ZoneControlZoneFlipTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneFlipTrackerController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalFlips_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalFlips, Is.EqualTo(0));
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
        public void SO_RecordFlip_IncrementsTotalFlips()
        {
            var so = CreateSO();
            so.RecordFlip();
            so.RecordFlip();
            Assert.That(so.TotalFlips, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFlip_AtMilestone_IncrementsMilestonesReached()
        {
            var so = CreateSO();
            for (int i = 0; i < so.MilestoneInterval; i++)
                so.RecordFlip();

            Assert.That(so.MilestonesReached, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFlip_AtMilestone_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneFlipTrackerSO)
                .GetField("_onFlipMilestoneReached", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.MilestoneInterval; i++)
                so.RecordFlip();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordFlip_MultiMilestone_HandledCorrectly()
        {
            var so = CreateSO();
            for (int i = 0; i < so.MilestoneInterval * 3; i++)
                so.RecordFlip();

            Assert.That(so.MilestonesReached, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NextMilestone_AdvancesAfterMilestoneReached()
        {
            var so             = CreateSO();
            int firstMilestone = so.NextMilestone;

            for (int i = 0; i < so.MilestoneInterval; i++)
                so.RecordFlip();

            Assert.That(so.NextMilestone, Is.GreaterThan(firstMilestone));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.MilestoneInterval + 1; i++)
                so.RecordFlip();

            so.Reset();

            Assert.That(so.TotalFlips,      Is.EqualTo(0));
            Assert.That(so.MilestonesReached, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_FlipTrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FlipTrackerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullFlipTrackerSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneFlipTrackerController)
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

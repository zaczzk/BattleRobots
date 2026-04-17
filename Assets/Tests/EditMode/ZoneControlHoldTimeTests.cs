using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T370: <see cref="ZoneControlHoldTimeSO"/> and
    /// <see cref="ZoneControlHoldTimeController"/>.
    ///
    /// ZoneControlHoldTimeTests (12):
    ///   SO_FreshInstance_IsHolding_False                   ×1
    ///   SO_FreshInstance_MilestoneCount_Zero               ×1
    ///   SO_StartHolding_SetsIsHolding                      ×1
    ///   SO_StopHolding_ClearsIsHolding                     ×1
    ///   SO_Tick_NotHolding_NoMilestone                     ×1
    ///   SO_Tick_MilestoneInterval_TriggersMilestone        ×1
    ///   SO_Tick_TwiceInterval_TriggersTwoMilestones        ×1
    ///   SO_Milestone_FiresEvent                            ×1
    ///   SO_Reset_ClearsAll                                 ×1
    ///   Controller_FreshInstance_HoldSO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow          ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow         ×1
    /// </summary>
    public sealed class ZoneControlHoldTimeTests
    {
        private static ZoneControlHoldTimeSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlHoldTimeSO>();

        private static ZoneControlHoldTimeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlHoldTimeController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsHolding_False()
        {
            var so = CreateSO();
            Assert.That(so.IsHolding, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MilestoneCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MilestoneCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartHolding_SetsIsHolding()
        {
            var so = CreateSO();
            so.StartHolding();
            Assert.That(so.IsHolding, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StopHolding_ClearsIsHolding()
        {
            var so = CreateSO();
            so.StartHolding();
            so.StopHolding();
            Assert.That(so.IsHolding, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NotHolding_NoMilestone()
        {
            var so = CreateSO();
            so.Tick(so.MilestoneInterval + 1f);
            Assert.That(so.MilestoneCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_MilestoneInterval_TriggersMilestone()
        {
            var so = CreateSO();
            so.StartHolding();
            so.Tick(so.MilestoneInterval + 0.1f);
            Assert.That(so.MilestoneCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_TwiceInterval_TriggersTwoMilestones()
        {
            var so = CreateSO();
            so.StartHolding();
            so.Tick(so.MilestoneInterval * 2f + 0.1f);
            Assert.That(so.MilestoneCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Milestone_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlHoldTimeSO)
                .GetField("_onMilestoneReached", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.StartHolding();
            so.Tick(so.MilestoneInterval + 0.1f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartHolding();
            so.Tick(so.MilestoneInterval + 0.1f);
            so.Reset();
            Assert.That(so.IsHolding, Is.False);
            Assert.That(so.MilestoneCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.TotalHoldTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_HoldSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HoldSO, Is.Null);
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
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}

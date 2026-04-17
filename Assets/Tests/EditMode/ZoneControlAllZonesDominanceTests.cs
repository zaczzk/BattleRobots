using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T395: <see cref="ZoneControlAllZonesDominanceSO"/> and
    /// <see cref="ZoneControlAllZonesDominanceController"/>.
    ///
    /// ZoneControlAllZonesDominanceTests (12):
    ///   SO_FreshInstance_IsDominating_False                      x1
    ///   SO_FreshInstance_AccumulatedTime_Zero                    x1
    ///   SO_StartDominating_SetsDominating                        x1
    ///   SO_StartDominating_Idempotent                            x1
    ///   SO_StopDominating_ClearsDominating                       x1
    ///   SO_Tick_NotDominating_NoAccumulate                       x1
    ///   SO_Tick_Dominating_AccumulatesTime                       x1
    ///   SO_Tick_ReachesMilestone_FiresEvent                      x1
    ///   SO_Tick_MultiMilestone_Safe                              x1
    ///   SO_Reset_ClearsAll                                       x1
    ///   Controller_FreshInstance_DominanceSO_Null                x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                x1
    /// </summary>
    public sealed class ZoneControlAllZonesDominanceTests
    {
        private static ZoneControlAllZonesDominanceSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlAllZonesDominanceSO>();

        private static ZoneControlAllZonesDominanceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlAllZonesDominanceController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsDominating_False()
        {
            var so = CreateSO();
            Assert.That(so.IsDominating, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AccumulatedTime_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AccumulatedTime, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartDominating_SetsDominating()
        {
            var so = CreateSO();
            so.StartDominating();
            Assert.That(so.IsDominating, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartDominating_Idempotent()
        {
            var so = CreateSO();
            so.StartDominating();
            so.StartDominating();
            Assert.That(so.IsDominating, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StopDominating_ClearsDominating()
        {
            var so = CreateSO();
            so.StartDominating();
            so.StopDominating();
            Assert.That(so.IsDominating, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NotDominating_NoAccumulate()
        {
            var so = CreateSO();
            so.Tick(10f);
            Assert.That(so.AccumulatedTime, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_Dominating_AccumulatesTime()
        {
            var so = CreateSO();
            so.StartDominating();
            so.Tick(5f);
            Assert.That(so.AccumulatedTime, Is.EqualTo(5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ReachesMilestone_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlAllZonesDominanceSO)
                .GetField("_onMilestoneReached", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.StartDominating();
            so.Tick(so.MilestoneInterval);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.MilestonesReached, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_MultiMilestone_Safe()
        {
            var so = CreateSO();
            so.StartDominating();
            so.Tick(so.MilestoneInterval * 3.5f);
            Assert.That(so.MilestonesReached, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartDominating();
            so.Tick(so.MilestoneInterval * 2f);
            so.Reset();
            Assert.That(so.IsDominating, Is.False);
            Assert.That(so.AccumulatedTime, Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.MilestonesReached, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_DominanceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DominanceSO, Is.Null);
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
    }
}

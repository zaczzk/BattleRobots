using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T409: <see cref="ZoneControlZoneControlRatioSO"/> and
    /// <see cref="ZoneControlZoneControlRatioController"/>.
    ///
    /// ZoneControlZoneControlRatioTests (13):
    ///   SO_FreshInstance_PlayerZones_Zero                        x1
    ///   SO_FreshInstance_HasMajority_False                       x1
    ///   SO_FreshInstance_HoldRatio_Zero                          x1
    ///   SO_SetZoneCounts_ComputesHoldRatio                       x1
    ///   SO_SetZoneCounts_MajorityAboveThreshold_SetsMajority     x1
    ///   SO_SetZoneCounts_MajorityBelowThreshold_ClearsMajority   x1
    ///   SO_SetZoneCounts_FiresRatioUpdated                       x1
    ///   SO_SetZoneCounts_MajorityTransition_FiresMajorityChanged x1
    ///   SO_SetZoneCounts_NoTransition_NoMajorityEvent            x1
    ///   SO_Reset_ClearsAll                                       x1
    ///   Controller_FreshInstance_RatioSO_Null                    x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                x1
    ///   Controller_Refresh_NullSO_HidesPanel                     x1
    /// </summary>
    public sealed class ZoneControlZoneControlRatioTests
    {
        private static ZoneControlZoneControlRatioSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneControlRatioSO>();

        private static ZoneControlZoneControlRatioController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneControlRatioController>();
        }

        [Test]
        public void SO_FreshInstance_PlayerZones_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PlayerZones, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HasMajority_False()
        {
            var so = CreateSO();
            Assert.That(so.HasMajority, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HoldRatio_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HoldRatio, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneCounts_ComputesHoldRatio()
        {
            var so = CreateSO();
            so.SetZoneCounts(2, 4);
            Assert.That(so.HoldRatio, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneCounts_MajorityAboveThreshold_SetsMajority()
        {
            var so = CreateSO();
            so.SetZoneCounts(3, 4);
            Assert.That(so.HasMajority, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneCounts_MajorityBelowThreshold_ClearsMajority()
        {
            var so = CreateSO();
            so.SetZoneCounts(3, 4);
            so.SetZoneCounts(1, 4);
            Assert.That(so.HasMajority, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneCounts_FiresRatioUpdated()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneControlRatioSO)
                .GetField("_onRatioUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetZoneCounts(2, 4);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_SetZoneCounts_MajorityTransition_FiresMajorityChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneControlRatioSO)
                .GetField("_onMajorityChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetZoneCounts(3, 4);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_SetZoneCounts_NoTransition_NoMajorityEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneControlRatioSO)
                .GetField("_onMajorityChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetZoneCounts(3, 4);
            fired = 0;
            so.SetZoneCounts(4, 4);
            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.SetZoneCounts(3, 4);
            so.Reset();
            Assert.That(so.PlayerZones,  Is.EqualTo(0));
            Assert.That(so.TotalZones,   Is.EqualTo(0));
            Assert.That(so.HasMajority,  Is.False);
            Assert.That(so.HoldRatio,    Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RatioSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RatioSO, Is.Null);
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
            typeof(ZoneControlZoneControlRatioController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T446: <see cref="ZoneControlCaptureHeatSO"/> and
    /// <see cref="ZoneControlCaptureHeatController"/>.
    ///
    /// ZoneControlCaptureHeatTests (12):
    ///   SO_FreshInstance_CurrentHeat_Zero                                x1
    ///   SO_FreshInstance_IsHot_False                                     x1
    ///   SO_RecordCapture_IncreasesHeat                                   x1
    ///   SO_RecordCapture_ClampsToMaxHeat                                 x1
    ///   SO_RecordCapture_FiresOnHeatHigh_AtThreshold                     x1
    ///   SO_Tick_DecaysHeat                                               x1
    ///   SO_Tick_FiresOnHeatCooled_WhenDropsBelowThreshold                x1
    ///   SO_Reset_ClearsAll                                               x1
    ///   SO_HeatProgress_Normalised                                       x1
    ///   Controller_FreshInstance_HeatSO_Null                             x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                        x1
    ///   Controller_Refresh_NullSO_HidesPanel                             x1
    /// </summary>
    public sealed class ZoneControlCaptureHeatTests
    {
        private static ZoneControlCaptureHeatSO CreateSO(
            float heatPerCapture = 20f,
            float decayRate      = 5f,
            float threshold      = 60f,
            float maxHeat        = 100f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHeatSO>();
            typeof(ZoneControlCaptureHeatSO)
                .GetField("_heatPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, heatPerCapture);
            typeof(ZoneControlCaptureHeatSO)
                .GetField("_decayRate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decayRate);
            typeof(ZoneControlCaptureHeatSO)
                .GetField("_highHeatThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            typeof(ZoneControlCaptureHeatSO)
                .GetField("_maxHeat", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxHeat);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHeatController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHeatController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentHeat_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentHeat, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsHot_False()
        {
            var so = CreateSO();
            Assert.That(so.IsHot, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncreasesHeat()
        {
            var so = CreateSO(heatPerCapture: 20f);
            so.RecordCapture();
            Assert.That(so.CurrentHeat, Is.EqualTo(20f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampsToMaxHeat()
        {
            var so = CreateSO(heatPerCapture: 200f, maxHeat: 100f);
            so.RecordCapture();
            Assert.That(so.CurrentHeat, Is.EqualTo(100f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnHeatHigh_AtThreshold()
        {
            var so      = CreateSO(heatPerCapture: 70f, threshold: 60f, maxHeat: 100f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHeatSO)
                .GetField("_onHeatHigh", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCapture();

            Assert.That(so.IsHot,  Is.True);
            Assert.That(fired,     Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_DecaysHeat()
        {
            var so = CreateSO(heatPerCapture: 50f, decayRate: 10f, threshold: 100f);
            so.RecordCapture();
            so.Tick(1f);
            Assert.That(so.CurrentHeat, Is.EqualTo(40f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresOnHeatCooled_WhenDropsBelowThreshold()
        {
            var so      = CreateSO(heatPerCapture: 70f, decayRate: 50f, threshold: 60f, maxHeat: 100f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHeatSO)
                .GetField("_onHeatCooled", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(); // heat = 70 → isHot
            so.Tick(1f);        // heat = 20 → cools below 60

            Assert.That(so.IsHot, Is.False);
            Assert.That(fired,    Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(heatPerCapture: 70f, threshold: 60f);
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentHeat, Is.EqualTo(0f));
            Assert.That(so.IsHot,       Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HeatProgress_Normalised()
        {
            var so = CreateSO(heatPerCapture: 50f, maxHeat: 100f);
            so.RecordCapture();
            Assert.That(so.HeatProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HeatSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HeatSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureHeatController)
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

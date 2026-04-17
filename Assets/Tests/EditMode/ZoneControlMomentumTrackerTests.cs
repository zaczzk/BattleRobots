using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlMomentumTrackerTests
    {
        private static ZoneControlMomentumTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMomentumTrackerSO>();

        private static ZoneControlMomentumTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMomentumTrackerController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsBurst_False()
        {
            var so = CreateSO();
            Assert.That(so.IsBurst, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptureCount()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BurstThresholdMet_SetsBurst()
        {
            var so = CreateSO();
            int threshold = so.BurstThreshold;
            for (int i = 0; i < threshold; i++) so.RecordCapture(0f);
            Assert.That(so.IsBurst, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresBurstDetected_OnTransition()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMomentumTrackerSO)
                .GetField("_onBurstDetected", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int threshold = so.BurstThreshold;
            for (int i = 0; i < threshold + 2; i++) so.RecordCapture(0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_PrunesExpiredCaptures()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.Tick(so.BurstWindow + 5f);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BurstEnded_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMomentumTrackerSO)
                .GetField("_onBurstEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int threshold = so.BurstThreshold;
            for (int i = 0; i < threshold; i++) so.RecordCapture(0f);
            Assert.That(so.IsBurst, Is.True);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.BurstWindow + 5f);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.IsBurst, Is.False);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            int threshold = so.BurstThreshold;
            for (int i = 0; i < threshold; i++) so.RecordCapture(0f);
            so.Reset();
            Assert.That(so.IsBurst, Is.False);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_MomentumSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MomentumSO, Is.Null);
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

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMomentumTrackerController)
                .GetField("_onBurstDetected", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullMomentumSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlMomentumTrackerController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

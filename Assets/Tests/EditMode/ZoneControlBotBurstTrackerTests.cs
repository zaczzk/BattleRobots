using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T391: <see cref="ZoneControlBotBurstTrackerSO"/> and
    /// <see cref="ZoneControlBotBurstTrackerController"/>.
    ///
    /// ZoneControlBotBurstTrackerTests (12):
    ///   SO_FreshInstance_IsBotBursting_False                    ×1
    ///   SO_FreshInstance_BotCaptureCount_Zero                   ×1
    ///   SO_RecordBotCapture_IncrementsBotCaptureCount           ×1
    ///   SO_RecordBotCapture_BurstThresholdMet_SetsBursting      ×1
    ///   SO_RecordBotCapture_FiresBotBurstStarted_OnTransition   ×1
    ///   SO_Tick_PrunesExpiredCaptures                           ×1
    ///   SO_Tick_BurstEnded_FiresEvent                           ×1
    ///   SO_Reset_ClearsAll                                      ×1
    ///   Controller_FreshInstance_BotBurstSO_Null                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow              ×1
    ///   Controller_OnDisable_Unregisters_Channel                ×1
    /// </summary>
    public sealed class ZoneControlBotBurstTrackerTests
    {
        private static ZoneControlBotBurstTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlBotBurstTrackerSO>();

        private static ZoneControlBotBurstTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlBotBurstTrackerController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsBotBursting_False()
        {
            var so = CreateSO();
            Assert.That(so.IsBotBursting, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BotCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BotCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotCaptureCount()
        {
            var so = CreateSO();
            so.RecordBotCapture(0f);
            Assert.That(so.BotCaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BurstThresholdMet_SetsBursting()
        {
            var so = CreateSO();
            int threshold = so.BotBurstThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture(0f);
            Assert.That(so.IsBotBursting, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresBotBurstStarted_OnTransition()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlBotBurstTrackerSO)
                .GetField("_onBotBurstStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int threshold = so.BotBurstThreshold;
            for (int i = 0; i < threshold + 2; i++) so.RecordBotCapture(0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_PrunesExpiredCaptures()
        {
            var so = CreateSO();
            so.RecordBotCapture(0f);
            so.RecordBotCapture(1f);
            so.Tick(so.BotBurstWindow + 5f);
            Assert.That(so.BotCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BurstEnded_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlBotBurstTrackerSO)
                .GetField("_onBotBurstEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int threshold = so.BotBurstThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture(0f);
            Assert.That(so.IsBotBursting, Is.True);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.BotBurstWindow + 5f);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.IsBotBursting, Is.False);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            int threshold = so.BotBurstThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture(0f);
            so.Reset();
            Assert.That(so.IsBotBursting, Is.False);
            Assert.That(so.BotCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BotBurstSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BotBurstSO, Is.Null);
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
            typeof(ZoneControlBotBurstTrackerController)
                .GetField("_onBotBurstStarted", BindingFlags.NonPublic | BindingFlags.Instance)
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
    }
}

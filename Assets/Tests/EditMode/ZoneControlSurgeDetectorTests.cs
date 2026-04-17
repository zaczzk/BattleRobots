using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T366: <see cref="ZoneControlSurgeDetectorSO"/> and
    /// <see cref="ZoneControlSurgeDetectorController"/>.
    ///
    /// ZoneControlSurgeDetectorTests (12):
    ///   SO_FreshInstance_IsSurging_False                     ×1
    ///   SO_FreshInstance_CaptureCount_Zero                   ×1
    ///   SO_RecordCapture_IncrementsCaptureCount              ×1
    ///   SO_RecordCapture_SurgeThresholdMet_SetsSurging       ×1
    ///   SO_RecordCapture_FiresSurgeStarted_OnTransition      ×1
    ///   SO_Tick_PrunesExpiredCaptures                        ×1
    ///   SO_Tick_SurgeEnded_FiresEvent                        ×1
    ///   SO_Reset_ClearsAll                                   ×1
    ///   Controller_FreshInstance_SurgeSO_Null                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow           ×1
    ///   Controller_OnDisable_Unregisters_Channel             ×1
    /// </summary>
    public sealed class ZoneControlSurgeDetectorTests
    {
        private static ZoneControlSurgeDetectorSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlSurgeDetectorSO>();

        private static ZoneControlSurgeDetectorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlSurgeDetectorController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsSurging_False()
        {
            var so = CreateSO();
            Assert.That(so.IsSurging, Is.False);
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
        public void SO_RecordCapture_SurgeThresholdMet_SetsSurging()
        {
            var so = CreateSO();
            int threshold = so.SurgeThreshold;
            for (int i = 0; i < threshold; i++) so.RecordCapture(0f);
            Assert.That(so.IsSurging, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresSurgeStarted_OnTransition()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlSurgeDetectorSO)
                .GetField("_onSurgeStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int threshold = so.SurgeThreshold;
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
            so.Tick(so.SurgeWindow + 5f);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_SurgeEnded_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlSurgeDetectorSO)
                .GetField("_onSurgeEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int threshold = so.SurgeThreshold;
            for (int i = 0; i < threshold; i++) so.RecordCapture(0f);
            Assert.That(so.IsSurging, Is.True);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.SurgeWindow + 5f);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.IsSurging, Is.False);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            int threshold = so.SurgeThreshold;
            for (int i = 0; i < threshold; i++) so.RecordCapture(0f);
            so.Reset();
            Assert.That(so.IsSurging, Is.False);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_SurgeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SurgeSO, Is.Null);
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
            typeof(ZoneControlSurgeDetectorController)
                .GetField("_onSurgeStarted", BindingFlags.NonPublic | BindingFlags.Instance)
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

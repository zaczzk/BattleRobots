using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T367: <see cref="ZoneControlCaptureEnduranceSO"/> and
    /// <see cref="ZoneControlCaptureEnduranceController"/>.
    ///
    /// ZoneControlCaptureEnduranceTests (12):
    ///   SO_FreshInstance_IsEnduring_False                    ×1
    ///   SO_FreshInstance_Progress_Zero                       ×1
    ///   SO_RecordCapture_IncrementsCaptureCount              ×1
    ///   SO_RecordCapture_RequiredMet_SetsEnduring            ×1
    ///   SO_RecordCapture_FiresEnduranceAchieved_OnTransition ×1
    ///   SO_Tick_PrunesExpiredCaptures                        ×1
    ///   SO_Tick_EnduranceLost_FiresEvent                     ×1
    ///   SO_Reset_ClearsAll                                   ×1
    ///   Controller_FreshInstance_EnduranceSO_Null            ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow           ×1
    ///   Controller_OnDisable_Unregisters_Channel             ×1
    /// </summary>
    public sealed class ZoneControlCaptureEnduranceTests
    {
        private static ZoneControlCaptureEnduranceSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureEnduranceSO>();

        private static ZoneControlCaptureEnduranceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEnduranceController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsEnduring_False()
        {
            var so = CreateSO();
            Assert.That(so.IsEnduring, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_Progress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Progress, Is.EqualTo(0f));
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
        public void SO_RecordCapture_RequiredMet_SetsEnduring()
        {
            var so = CreateSO();
            int required = so.RequiredCaptures;
            for (int i = 0; i < required; i++) so.RecordCapture(0f);
            Assert.That(so.IsEnduring, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresEnduranceAchieved_OnTransition()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEnduranceSO)
                .GetField("_onEnduranceAchieved", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int required = so.RequiredCaptures;
            for (int i = 0; i < required + 1; i++) so.RecordCapture(0f);

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
            so.Tick(so.EnduranceWindow + 5f);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_EnduranceLost_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEnduranceSO)
                .GetField("_onEnduranceLost", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int required = so.RequiredCaptures;
            for (int i = 0; i < required; i++) so.RecordCapture(0f);
            Assert.That(so.IsEnduring, Is.True);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.EnduranceWindow + 5f);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.IsEnduring, Is.False);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            int required = so.RequiredCaptures;
            for (int i = 0; i < required; i++) so.RecordCapture(0f);
            so.Reset();
            Assert.That(so.IsEnduring, Is.False);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Assert.That(so.Progress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_EnduranceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EnduranceSO, Is.Null);
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
            typeof(ZoneControlCaptureEnduranceController)
                .GetField("_onEnduranceAchieved", BindingFlags.NonPublic | BindingFlags.Instance)
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

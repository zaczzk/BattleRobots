using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T414: <see cref="ZoneControlCaptureQuotaSO"/> and
    /// <see cref="ZoneControlCaptureQuotaController"/>.
    ///
    /// ZoneControlCaptureQuotaTests (12):
    ///   SO_FreshInstance_CaptureCount_Zero              x1
    ///   SO_FreshInstance_QuotaMet_False                 x1
    ///   SO_RecordCapture_BelowTarget_NoFire             x1
    ///   SO_RecordCapture_MeetsTarget_FiresEvent         x1
    ///   SO_RecordCapture_MeetsTarget_SetsQuotaMet       x1
    ///   SO_RecordCapture_AfterQuotaMet_Idempotent       x1
    ///   SO_QuotaProgress_ReflectsCount                  x1
    ///   SO_Reset_ClearsAll                              x1
    ///   Controller_FreshInstance_QuotaSO_Null           x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow       x1
    ///   Controller_Refresh_NullSO_HidesPanel            x1
    ///   Controller_Refresh_ShowsPanel_WhenSOSet         x1
    /// </summary>
    public sealed class ZoneControlCaptureQuotaTests
    {
        private static ZoneControlCaptureQuotaSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureQuotaSO>();

        private static ZoneControlCaptureQuotaController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureQuotaController>();
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_QuotaMet_False()
        {
            var so = CreateSO();
            Assert.That(so.QuotaMet, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowTarget_NoFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQuotaSO)
                .GetField("_onQuotaMet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            int below = so.QuotaTarget - 1;
            for (int i = 0; i < below; i++)
                so.RecordCapture();

            Assert.That(fired,          Is.EqualTo(0));
            Assert.That(so.QuotaMet,    Is.False);
            Assert.That(so.CaptureCount, Is.EqualTo(below));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_MeetsTarget_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQuotaSO)
                .GetField("_onQuotaMet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.QuotaTarget; i++)
                so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_MeetsTarget_SetsQuotaMet()
        {
            var so = CreateSO();
            for (int i = 0; i < so.QuotaTarget; i++)
                so.RecordCapture();
            Assert.That(so.QuotaMet, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AfterQuotaMet_Idempotent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQuotaSO)
                .GetField("_onQuotaMet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.QuotaTarget + 5; i++)
                so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_QuotaProgress_ReflectsCount()
        {
            var so = CreateSO();
            int half = so.QuotaTarget / 2;
            for (int i = 0; i < half; i++)
                so.RecordCapture();
            float expected = Mathf.Clamp01((float)half / so.QuotaTarget);
            Assert.That(so.QuotaProgress, Is.EqualTo(expected).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.QuotaTarget; i++)
                so.RecordCapture();
            so.Reset();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Assert.That(so.QuotaMet,     Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_QuotaSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.QuotaSO, Is.Null);
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
            typeof(ZoneControlCaptureQuotaController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_ShowsPanel_WhenSOSet()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();

            typeof(ZoneControlCaptureQuotaController)
                .GetField("_quotaSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureQuotaController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(false);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}

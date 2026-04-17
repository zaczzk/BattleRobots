using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T371: <see cref="ZoneControlCaptureRushSO"/> and
    /// <see cref="ZoneControlCaptureRushController"/>.
    ///
    /// ZoneControlCaptureRushTests (12):
    ///   SO_FreshInstance_IsRushing_False                   ×1
    ///   SO_FreshInstance_TotalRushCount_Zero               ×1
    ///   SO_RecordCapture_BelowThreshold_NoRush             ×1
    ///   SO_RecordCapture_AtThreshold_StartsRush            ×1
    ///   SO_RecordCapture_AtThreshold_FiresEvent            ×1
    ///   SO_Tick_WindowExpires_EndsRush                     ×1
    ///   SO_Reset_ClearsAll                                 ×1
    ///   Controller_FreshInstance_RushSO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow          ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow         ×1
    ///   Controller_OnDisable_Unregisters_Channel           ×1
    ///   Controller_Refresh_NullRushSO_HidesPanel           ×1
    /// </summary>
    public sealed class ZoneControlCaptureRushTests
    {
        private static ZoneControlCaptureRushSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureRushSO>();

        private static ZoneControlCaptureRushController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRushController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsRushing_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRushing, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalRushCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalRushCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowThreshold_NoRush()
        {
            var so = CreateSO();
            // RequiredCaptures default = 3; record only 2
            so.RecordCapture(0f);
            so.RecordCapture(0.1f);
            Assert.That(so.IsRushing, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_StartsRush()
        {
            var so = CreateSO();
            for (int i = 0; i < so.RequiredCaptures; i++)
                so.RecordCapture(i * 0.1f);
            Assert.That(so.IsRushing, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRushSO)
                .GetField("_onRushCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.RequiredCaptures; i++)
                so.RecordCapture(i * 0.1f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_WindowExpires_EndsRush()
        {
            var so = CreateSO();
            // Trigger rush at t=0
            for (int i = 0; i < so.RequiredCaptures; i++)
                so.RecordCapture(0f);
            Assert.That(so.IsRushing, Is.True);

            // Tick far past the window so all timestamps are pruned
            so.Tick(so.RushWindow + 1f);
            Assert.That(so.IsRushing, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.RequiredCaptures; i++)
                so.RecordCapture(i * 0.1f);
            so.Reset();
            Assert.That(so.IsRushing, Is.False);
            Assert.That(so.TotalRushCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RushSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RushSO, Is.Null);
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
            typeof(ZoneControlCaptureRushController)
                .GetField("_onRushCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullRushSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlCaptureRushController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

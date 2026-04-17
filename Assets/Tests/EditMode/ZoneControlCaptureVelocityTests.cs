using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T383: <see cref="ZoneControlCaptureVelocitySO"/> and
    /// <see cref="ZoneControlCaptureVelocityController"/>.
    ///
    /// ZoneControlCaptureVelocityTests (12):
    ///   SO_FreshInstance_CaptureCount_Zero                       ×1
    ///   SO_FreshInstance_IsHighVelocity_False                    ×1
    ///   SO_RecordCapture_IncrementsCaptureCount                  ×1
    ///   SO_RecordCapture_AboveHighThreshold_SetsIsHighVelocity   ×1
    ///   SO_RecordCapture_AboveHighThreshold_FiresOnHighVelocity  ×1
    ///   SO_GetVelocity_ReturnsCorrectRate                        ×1
    ///   SO_Tick_PrunesStaleTimestamps                            ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_VelocitySO_Null                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_Refresh_NullVelocitySO_HidesPanel             ×1
    /// </summary>
    public sealed class ZoneControlCaptureVelocityTests
    {
        private static ZoneControlCaptureVelocitySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureVelocitySO>();

        private static ZoneControlCaptureVelocityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureVelocityController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsHighVelocity_False()
        {
            var so = CreateSO();
            Assert.That(so.IsHighVelocity, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptureCount()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(0.5f);
            Assert.That(so.CaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AboveHighThreshold_SetsIsHighVelocity()
        {
            var so = CreateSO();
            // Window = 5s, threshold = 1.0 cap/s → need ≥5 caps in 5s
            // Record 6 captures at t=0 to trigger high velocity
            for (int i = 0; i < 6; i++)
                so.RecordCapture(0f);

            Assert.That(so.IsHighVelocity, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AboveHighThreshold_FiresOnHighVelocity()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureVelocitySO)
                .GetField("_onHighVelocity", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < 6; i++)
                so.RecordCapture(0f);

            Assert.That(fired, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetVelocity_ReturnsCorrectRate()
        {
            var so = CreateSO();
            // Record 5 captures all at t=0; window=5s → velocity = 5/5 = 1.0
            for (int i = 0; i < 5; i++)
                so.RecordCapture(0f);

            float velocity = so.GetVelocity(0f);
            Assert.That(velocity, Is.EqualTo(1.0f).Within(0.01f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_PrunesStaleTimestamps()
        {
            var so = CreateSO();
            so.RecordCapture(0f);   // stale after window passes
            so.RecordCapture(0.5f); // also stale

            // Tick well beyond the window duration (default 5s)
            so.Tick(100f);

            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < 6; i++)
                so.RecordCapture(0f);

            so.Reset();

            Assert.That(so.CaptureCount,    Is.EqualTo(0));
            Assert.That(so.IsHighVelocity,  Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_VelocitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VelocitySO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullVelocitySO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureVelocityController)
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

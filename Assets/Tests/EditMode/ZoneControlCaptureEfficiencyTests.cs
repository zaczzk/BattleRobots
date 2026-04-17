using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T397: <see cref="ZoneControlCaptureEfficiencySO"/> and
    /// <see cref="ZoneControlCaptureEfficiencyController"/>.
    ///
    /// ZoneControlCaptureEfficiencyTests (13):
    ///   SO_FreshInstance_PlayerCaptures_Zero                     x1
    ///   SO_FreshInstance_GetEfficiency_Zero                      x1
    ///   SO_RecordPlayerCapture_IncrementsPlayerAndTotal          x1
    ///   SO_RecordBotCapture_IncrementsTotalOnly                  x1
    ///   SO_GetEfficiency_HalfAndHalf                             x1
    ///   SO_EvaluateEfficiency_HighThreshold_FiresHighEfficiency  x1
    ///   SO_EvaluateEfficiency_LowThreshold_FiresLowEfficiency    x1
    ///   SO_EvaluateEfficiency_Idempotent_HighAlreadySet          x1
    ///   SO_Reset_ClearsAll                                       x1
    ///   Controller_FreshInstance_EfficiencySO_Null               x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               x1
    ///   Controller_Refresh_NullSO_HidesPanel                     x1
    /// </summary>
    public sealed class ZoneControlCaptureEfficiencyTests
    {
        private static ZoneControlCaptureEfficiencySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureEfficiencySO>();

        private static ZoneControlCaptureEfficiencyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEfficiencyController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GetEfficiency_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Efficiency, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsPlayerAndTotal()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(2));
            Assert.That(so.TotalCaptures,  Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsTotalOnly()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.TotalCaptures,  Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetEfficiency_HalfAndHalf()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Efficiency, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateEfficiency_HighThreshold_FiresHighEfficiency()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEfficiencySO)
                .GetField("_onHighEfficiency", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            // All player captures → efficiency = 1.0 >= 0.7 threshold
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();

            Assert.That(so.IsHighEfficiency, Is.True);
            Assert.That(fired, Is.GreaterThanOrEqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluateEfficiency_LowThreshold_FiresLowEfficiency()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEfficiencySO)
                .GetField("_onLowEfficiency", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            // All bot captures → efficiency = 0.0 <= 0.3 threshold
            so.RecordBotCapture();
            so.RecordBotCapture();

            Assert.That(so.IsLowEfficiency, Is.True);
            Assert.That(fired, Is.GreaterThanOrEqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluateEfficiency_Idempotent_HighAlreadySet()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEfficiencySO)
                .GetField("_onHighEfficiency", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(); // fires high once
            so.RecordPlayerCapture(); // already high — no re-fire

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.PlayerCaptures,   Is.EqualTo(0));
            Assert.That(so.TotalCaptures,    Is.EqualTo(0));
            Assert.That(so.IsHighEfficiency, Is.False);
            Assert.That(so.IsLowEfficiency,  Is.False);
            Assert.That(so.Efficiency,       Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_EfficiencySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EfficiencySO, Is.Null);
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureEfficiencyController)
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

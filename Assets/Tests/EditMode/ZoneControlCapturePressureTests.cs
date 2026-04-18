using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T422: <see cref="ZoneControlCapturePressureSO"/> and
    /// <see cref="ZoneControlCapturePressureController"/>.
    ///
    /// ZoneControlCapturePressureTests (12):
    ///   SO_FreshInstance_PressureRatio_Zero                         x1
    ///   SO_FreshInstance_IsHighPressure_False                       x1
    ///   SO_RecordBotCapture_IncrementsBotCount                      x1
    ///   SO_RecordPlayerCapture_IncrementsPlayerCount                x1
    ///   SO_PressureRatio_OnlyBotCaptures_ReturnsOne                 x1
    ///   SO_PressureRatio_EqualCaptures_ReturnsHalf                  x1
    ///   SO_EvaluatePressure_FiresHighPressureEvent                  x1
    ///   SO_EvaluatePressure_FiresNormalEvent_OnRecovery             x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   SO_Tick_PrunesStaleEntries                                  x1
    ///   Controller_FreshInstance_PressureSO_Null                    x1
    ///   Controller_Refresh_NullSO_HidesPanel                        x1
    /// </summary>
    public sealed class ZoneControlCapturePressureTests
    {
        private static ZoneControlCapturePressureSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCapturePressureSO>();

        private static ZoneControlCapturePressureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePressureController>();
        }

        [Test]
        public void SO_FreshInstance_PressureRatio_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PressureRatio, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsHighPressure_False()
        {
            var so = CreateSO();
            Assert.That(so.IsHighPressure, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotCount()
        {
            var so = CreateSO();
            so.RecordBotCapture(0f);
            so.RecordBotCapture(1f);
            Assert.That(so.BotCaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsPlayerCount()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            Assert.That(so.PlayerCaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PressureRatio_OnlyBotCaptures_ReturnsOne()
        {
            var so = CreateSO();
            so.RecordBotCapture(0f);
            so.RecordBotCapture(1f);
            Assert.That(so.PressureRatio, Is.EqualTo(1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PressureRatio_EqualCaptures_ReturnsHalf()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            so.RecordBotCapture(1f);
            Assert.That(so.PressureRatio, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePressure_FiresHighPressureEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePressureSO)
                .GetField("_onHighPressure", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            // Set low threshold so any bot capture triggers it
            typeof(ZoneControlCapturePressureSO)
                .GetField("_highPressureThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0.1f);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordBotCapture(0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluatePressure_FiresNormalEvent_OnRecovery()
        {
            var so      = CreateSO();
            var highCh  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var normCh  = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePressureSO)
                .GetField("_onHighPressure",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, highCh);
            typeof(ZoneControlCapturePressureSO)
                .GetField("_onPressureNormal", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, normCh);
            typeof(ZoneControlCapturePressureSO)
                .GetField("_highPressureThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0.1f);

            int normFired = 0;
            normCh.RegisterCallback(() => normFired++);

            so.RecordBotCapture(0f);  // triggers high pressure
            so.RecordPlayerCapture(1f);
            so.RecordPlayerCapture(2f);
            so.RecordPlayerCapture(3f);
            so.RecordPlayerCapture(4f);
            so.RecordPlayerCapture(5f);
            so.RecordPlayerCapture(6f);
            so.RecordPlayerCapture(7f);
            so.RecordPlayerCapture(8f);
            so.RecordPlayerCapture(9f); // ratio now well below 0.1 → normal

            Assert.That(normFired, Is.GreaterThanOrEqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(highCh);
            Object.DestroyImmediate(normCh);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBotCapture(0f);
            so.RecordPlayerCapture(1f);
            so.Reset();
            Assert.That(so.BotCaptureCount,    Is.EqualTo(0));
            Assert.That(so.PlayerCaptureCount, Is.EqualTo(0));
            Assert.That(so.IsHighPressure,     Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_PrunesStaleEntries()
        {
            var so = CreateSO();
            typeof(ZoneControlCapturePressureSO)
                .GetField("_windowDuration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 5f);

            so.RecordBotCapture(0f);
            Assert.That(so.BotCaptureCount, Is.EqualTo(1));

            // Tick at t=10 with 5s window — entry at 0 should be pruned
            so.Tick(10f);
            Assert.That(so.BotCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PressureSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PressureSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCapturePressureController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T406: <see cref="ZoneControlCapturePenaltySO"/> and
    /// <see cref="ZoneControlCapturePenaltyController"/>.
    ///
    /// ZoneControlCapturePenaltyTests (12):
    ///   SO_FreshInstance_BotCaptureCount_Zero              x1
    ///   SO_FreshInstance_TotalPenaltyApplied_Zero          x1
    ///   SO_RecordBotCapture_IncrementsBotCaptureCount      x1
    ///   SO_RecordBotCapture_AccumulatesTotalPenalty        x1
    ///   SO_RecordBotCapture_FiresEvent                     x1
    ///   SO_PenaltyPerBotCapture_DefaultValue               x1
    ///   SO_Reset_ClearsAll                                 x1
    ///   Controller_FreshInstance_PenaltySO_Null            x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow          x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow         x1
    ///   Controller_Refresh_NullSO_HidesPanel               x1
    ///   Controller_HandleBotZoneCaptured_NullSO_NoThrow    x1
    /// </summary>
    public sealed class ZoneControlCapturePenaltyTests
    {
        private static ZoneControlCapturePenaltySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCapturePenaltySO>();

        private static ZoneControlCapturePenaltyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePenaltyController>();
        }

        [Test]
        public void SO_FreshInstance_BotCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BotCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalPenaltyApplied_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalPenaltyApplied, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotCaptureCount()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.BotCaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AccumulatesTotalPenalty()
        {
            var so      = CreateSO();
            int penalty = so.PenaltyPerBotCapture;
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalPenaltyApplied, Is.EqualTo(penalty * 2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresEvent()
        {
            var so       = CreateSO();
            var channel  = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePenaltySO)
                .GetField("_onPenaltyApplied", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_PenaltyPerBotCapture_DefaultValue()
        {
            var so = CreateSO();
            Assert.That(so.PenaltyPerBotCapture, Is.EqualTo(25));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.BotCaptureCount,     Is.EqualTo(0));
            Assert.That(so.TotalPenaltyApplied, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PenaltySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PenaltySO, Is.Null);
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
            typeof(ZoneControlCapturePenaltyController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleBotZoneCaptured_NullSO_NoThrow()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePenaltyController)
                .GetField("_onBotZoneCaptured", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => channel.Raise());
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }
    }
}

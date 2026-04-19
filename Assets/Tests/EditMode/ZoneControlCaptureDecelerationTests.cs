using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T470: <see cref="ZoneControlCaptureDecelerationSO"/> and
    /// <see cref="ZoneControlCaptureDecelerationController"/>.
    ///
    /// ZoneControlCaptureDecelerationTests (12):
    ///   SO_FreshInstance_CurrentDeceleration_Zero                           x1
    ///   SO_FreshInstance_DecelerationProgress_Zero                          x1
    ///   SO_RecordBotCapture_IncreasesDeceleration                           x1
    ///   SO_RecordBotCapture_ClampsAtMax                                     x1
    ///   SO_RecordBotCapture_FiresPeakEvent                                  x1
    ///   SO_RecordBotCapture_PeakEventIdempotent                             x1
    ///   SO_RecordPlayerCapture_ReducesDeceleration                          x1
    ///   SO_Tick_DecaysDeceleration                                          x1
    ///   SO_Reset_ClearsState                                                x1
    ///   Controller_FreshInstance_DecelerationSO_Null                        x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlCaptureDecelerationTests
    {
        private static ZoneControlCaptureDecelerationSO CreateSO(
            float decPerBot = 20f, float redPerPlayer = 10f, float decayRate = 5f, float maxDecel = 100f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDecelerationSO>();
            var t  = typeof(ZoneControlCaptureDecelerationSO);
            t.GetField("_decelerationPerBotCapture",  BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, decPerBot);
            t.GetField("_reductionPerPlayerCapture",  BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, redPerPlayer);
            t.GetField("_decayRate",                  BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, decayRate);
            t.GetField("_maxDeceleration",            BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, maxDecel);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDecelerationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDecelerationController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentDeceleration_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentDeceleration, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DecelerationProgress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DecelerationProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncreasesDeceleration()
        {
            var so = CreateSO(decPerBot: 20f, maxDecel: 100f);
            so.RecordBotCapture();
            Assert.That(so.CurrentDeceleration, Is.EqualTo(20f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtMax()
        {
            var so = CreateSO(decPerBot: 60f, maxDecel: 100f);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentDeceleration, Is.EqualTo(100f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresPeakEvent()
        {
            var so        = CreateSO(decPerBot: 100f, maxDecel: 100f);
            int fireCount = 0;
            var evt       = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDecelerationSO)
                .GetField("_onDecelerationPeak", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.RecordBotCapture();
            Assert.That(fireCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_PeakEventIdempotent()
        {
            var so        = CreateSO(decPerBot: 100f, maxDecel: 100f);
            int fireCount = 0;
            var evt       = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDecelerationSO)
                .GetField("_onDecelerationPeak", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(fireCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReducesDeceleration()
        {
            var so = CreateSO(decPerBot: 50f, redPerPlayer: 20f, maxDecel: 100f);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentDeceleration, Is.EqualTo(30f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DecaysDeceleration()
        {
            var so = CreateSO(decPerBot: 50f, decayRate: 10f, maxDecel: 100f);
            so.RecordBotCapture();
            so.Tick(2f);
            Assert.That(so.CurrentDeceleration, Is.EqualTo(30f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO(decPerBot: 50f, maxDecel: 100f);
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.CurrentDeceleration,  Is.EqualTo(0f));
            Assert.That(so.DecelerationProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DecelerationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DecelerationSO, Is.Null);
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
            typeof(ZoneControlCaptureDecelerationController)
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

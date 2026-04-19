using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T466: <see cref="ZoneControlCaptureAccelerationSO"/> and
    /// <see cref="ZoneControlCaptureAccelerationController"/>.
    ///
    /// ZoneControlCaptureAccelerationTests (12):
    ///   SO_FreshInstance_CurrentAcceleration_Zero                         x1
    ///   SO_FreshInstance_AccelerationProgress_Zero                        x1
    ///   SO_RecordCapture_IncreasesAcceleration                            x1
    ///   SO_RecordCapture_ClampsAtMax                                      x1
    ///   SO_RecordCapture_FiresMaxAccelerationEvent                        x1
    ///   SO_RecordCapture_MaxEventIdempotent                               x1
    ///   SO_Tick_DecaysAcceleration                                        x1
    ///   SO_Tick_ClampsAtZero                                              x1
    ///   SO_Reset_ClearsState                                              x1
    ///   Controller_FreshInstance_AccelerationSO_Null                      x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                         x1
    ///   Controller_Refresh_NullSO_HidesPanel                              x1
    /// </summary>
    public sealed class ZoneControlCaptureAccelerationTests
    {
        private static ZoneControlCaptureAccelerationSO CreateSO(
            float accelPerCapture = 15f, float decayRate = 10f, float maxAcceleration = 100f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAccelerationSO>();
            var t = typeof(ZoneControlCaptureAccelerationSO);
            t.GetField("_accelerationPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, accelPerCapture);
            t.GetField("_decayRate",              BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, decayRate);
            t.GetField("_maxAcceleration",        BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, maxAcceleration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAccelerationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAccelerationController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentAcceleration_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentAcceleration, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AccelerationProgress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AccelerationProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncreasesAcceleration()
        {
            var so = CreateSO(accelPerCapture: 15f, maxAcceleration: 100f);
            so.RecordCapture();
            Assert.That(so.CurrentAcceleration, Is.EqualTo(15f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampsAtMax()
        {
            var so = CreateSO(accelPerCapture: 60f, maxAcceleration: 100f);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CurrentAcceleration, Is.EqualTo(100f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresMaxAccelerationEvent()
        {
            var so      = CreateSO(accelPerCapture: 100f, maxAcceleration: 100f);
            int fireCount = 0;
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAccelerationSO)
                .GetField("_onMaxAcceleration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.RecordCapture();
            Assert.That(fireCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_MaxEventIdempotent()
        {
            var so      = CreateSO(accelPerCapture: 100f, maxAcceleration: 100f);
            int fireCount = 0;
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAccelerationSO)
                .GetField("_onMaxAcceleration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(fireCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_DecaysAcceleration()
        {
            var so = CreateSO(accelPerCapture: 50f, decayRate: 10f, maxAcceleration: 100f);
            so.RecordCapture();
            so.Tick(2f);
            Assert.That(so.CurrentAcceleration, Is.EqualTo(30f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ClampsAtZero()
        {
            var so = CreateSO(accelPerCapture: 10f, decayRate: 100f, maxAcceleration: 100f);
            so.RecordCapture();
            so.Tick(10f);
            Assert.That(so.CurrentAcceleration, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO(accelPerCapture: 50f, maxAcceleration: 100f);
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentAcceleration,  Is.EqualTo(0f));
            Assert.That(so.AccelerationProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AccelerationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AccelerationSO, Is.Null);
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
            typeof(ZoneControlCaptureAccelerationController)
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

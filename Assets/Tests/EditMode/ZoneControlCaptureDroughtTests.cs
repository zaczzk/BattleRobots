using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T396: <see cref="ZoneControlCaptureDroughtSO"/> and
    /// <see cref="ZoneControlCaptureDroughtController"/>.
    ///
    /// ZoneControlCaptureDroughtTests (12):
    ///   SO_FreshInstance_IsDrought_False                         x1
    ///   SO_FreshInstance_TimeSinceCapture_Zero                   x1
    ///   SO_RecordCapture_ResetsTimer                             x1
    ///   SO_RecordCapture_EndsDrought                             x1
    ///   SO_Tick_BelowThreshold_NoDrought                         x1
    ///   SO_Tick_AtThreshold_StartsDrought                        x1
    ///   SO_Tick_FiresOnDroughtStarted                            x1
    ///   SO_RecordCapture_DuringDrought_FiresDroughtEnded         x1
    ///   SO_Reset_ClearsAll                                       x1
    ///   Controller_FreshInstance_DroughtSO_Null                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                x1
    ///   Controller_Refresh_NullSO_HidesPanel                     x1
    /// </summary>
    public sealed class ZoneControlCaptureDroughtTests
    {
        private static ZoneControlCaptureDroughtSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureDroughtSO>();

        private static ZoneControlCaptureDroughtController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDroughtController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsDrought_False()
        {
            var so = CreateSO();
            Assert.That(so.IsDrought, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TimeSinceCapture_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TimeSinceCapture, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ResetsTimer()
        {
            var so = CreateSO();
            so.Tick(5f);
            so.RecordCapture();
            Assert.That(so.TimeSinceCapture, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_EndsDrought()
        {
            var so = CreateSO();
            so.Tick(so.DroughtThreshold + 1f);
            Assert.That(so.IsDrought, Is.True);
            so.RecordCapture();
            Assert.That(so.IsDrought, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowThreshold_NoDrought()
        {
            var so = CreateSO();
            so.Tick(so.DroughtThreshold * 0.5f);
            Assert.That(so.IsDrought, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AtThreshold_StartsDrought()
        {
            var so = CreateSO();
            so.Tick(so.DroughtThreshold);
            Assert.That(so.IsDrought, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresOnDroughtStarted()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDroughtSO)
                .GetField("_onDroughtStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.DroughtThreshold);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_DuringDrought_FiresDroughtEnded()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDroughtSO)
                .GetField("_onDroughtEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.DroughtThreshold);
            so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.Tick(so.DroughtThreshold + 5f);
            so.Reset();
            Assert.That(so.IsDrought, Is.False);
            Assert.That(so.TimeSinceCapture, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_DroughtSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DroughtSO, Is.Null);
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureDroughtController)
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

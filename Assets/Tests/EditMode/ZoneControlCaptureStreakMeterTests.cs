using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T450: <see cref="ZoneControlCaptureStreakMeterSO"/> and
    /// <see cref="ZoneControlCaptureStreakMeterController"/>.
    ///
    /// ZoneControlCaptureStreakMeterTests (12):
    ///   SO_FreshInstance_MeterValue_Zero                                    x1
    ///   SO_FreshInstance_FillCount_Zero                                     x1
    ///   SO_RecordPlayerCapture_IncreasesMeterValue                          x1
    ///   SO_RecordPlayerCapture_TriggersFill_AtFull                          x1
    ///   SO_RecordPlayerCapture_FiresOnMeterFull                             x1
    ///   SO_RecordBotCapture_DrainsMeterValue                                x1
    ///   SO_RecordBotCapture_ClampsToZero                                    x1
    ///   SO_Reset_ClearsAll                                                  x1
    ///   SO_MeterProgress_Normalised                                         x1
    ///   Controller_FreshInstance_MeterSO_Null                               x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlCaptureStreakMeterTests
    {
        private static ZoneControlCaptureStreakMeterSO CreateSO(
            float fillPerCapture    = 20f,
            float drainOnBotCapture = 15f,
            int   bonusOnFill       = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureStreakMeterSO>();
            typeof(ZoneControlCaptureStreakMeterSO)
                .GetField("_fillPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fillPerCapture);
            typeof(ZoneControlCaptureStreakMeterSO)
                .GetField("_drainOnBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainOnBotCapture);
            typeof(ZoneControlCaptureStreakMeterSO)
                .GetField("_bonusOnFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusOnFill);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureStreakMeterController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureStreakMeterController>();
        }

        [Test]
        public void SO_FreshInstance_MeterValue_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MeterValue, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FillCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FillCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncreasesMeterValue()
        {
            var so = CreateSO(fillPerCapture: 20f);
            so.RecordPlayerCapture();
            Assert.That(so.MeterValue, Is.EqualTo(20f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TriggersFill_AtFull()
        {
            var so = CreateSO(fillPerCapture: 100f);
            so.RecordPlayerCapture();
            Assert.That(so.FillCount,   Is.EqualTo(1));
            Assert.That(so.MeterValue,  Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnMeterFull()
        {
            var so      = CreateSO(fillPerCapture: 100f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureStreakMeterSO)
                .GetField("_onMeterFull", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsMeterValue()
        {
            var so = CreateSO(fillPerCapture: 50f, drainOnBotCapture: 15f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.MeterValue, Is.EqualTo(35f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fillPerCapture: 10f, drainOnBotCapture: 50f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.MeterValue, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(fillPerCapture: 100f, bonusOnFill: 150);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.MeterValue,        Is.EqualTo(0f));
            Assert.That(so.FillCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MeterProgress_Normalised()
        {
            var so = CreateSO(fillPerCapture: 50f);
            so.RecordPlayerCapture();
            Assert.That(so.MeterProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MeterSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MeterSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
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
            typeof(ZoneControlCaptureStreakMeterController)
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

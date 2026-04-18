using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T418: <see cref="ZoneControlCaptureRateSO"/> and
    /// <see cref="ZoneControlCaptureRateController"/>.
    ///
    /// ZoneControlCaptureRateTests (12):
    ///   SO_FreshInstance_MatchStarted_False                     x1
    ///   SO_FreshInstance_CaptureCount_Zero                      x1
    ///   SO_StartMatch_SetsMatchStarted_True                     x1
    ///   SO_RecordCapture_IncrementsCaptureCount                 x1
    ///   SO_GetAverageRate_BeforeMatchStart_ReturnsZero          x1
    ///   SO_GetAverageRate_NoElapsedTime_ReturnsZero             x1
    ///   SO_GetAverageRate_WithCaptures_ComputesRate             x1
    ///   SO_RecordCapture_FiresEvent                             x1
    ///   SO_Reset_ClearsAll                                      x1
    ///   SO_Reset_MatchStarted_False                             x1
    ///   Controller_FreshInstance_RateSO_Null                    x1
    ///   Controller_Refresh_NullSO_HidesPanel                    x1
    /// </summary>
    public sealed class ZoneControlCaptureRateTests
    {
        private static ZoneControlCaptureRateSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureRateSO>();

        private static ZoneControlCaptureRateController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRateController>();
        }

        [Test]
        public void SO_FreshInstance_MatchStarted_False()
        {
            var so = CreateSO();
            Assert.That(so.MatchStarted, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartMatch_SetsMatchStarted_True()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            Assert.That(so.MatchStarted, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptureCount()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.CaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetAverageRate_BeforeMatchStart_ReturnsZero()
        {
            var so = CreateSO();
            Assert.That(so.GetAverageRate(60f), Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetAverageRate_NoElapsedTime_ReturnsZero()
        {
            var so = CreateSO();
            so.StartMatch(100f);
            so.RecordCapture(100f);
            Assert.That(so.GetAverageRate(100f), Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetAverageRate_WithCaptures_ComputesRate()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            so.RecordCapture(10f);
            so.RecordCapture(20f);
            // 2 captures / (60s / 60) = 2 caps/min
            float rate = so.GetAverageRate(60f);
            Assert.That(rate, Is.EqualTo(2f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRateSO)
                .GetField("_onRateUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.StartMatch(0f);
            so.RecordCapture(5f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            so.RecordCapture(10f);
            so.Reset();
            Assert.That(so.MatchStarted,  Is.False);
            Assert.That(so.CaptureCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_MatchStarted_False()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            so.Reset();
            Assert.That(so.MatchStarted, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RateSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RateSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureRateController)
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

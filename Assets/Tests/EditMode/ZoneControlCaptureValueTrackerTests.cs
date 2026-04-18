using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T436: <see cref="ZoneControlCaptureValueTrackerSO"/> and
    /// <see cref="ZoneControlCaptureValueTrackerController"/>.
    ///
    /// ZoneControlCaptureValueTrackerTests (12):
    ///   SO_FreshInstance_CaptureCount_Zero                          x1
    ///   SO_FreshInstance_TotalValue_Zero                            x1
    ///   SO_FreshInstance_AverageValue_Zero                          x1
    ///   SO_RecordCapture_IncrementsCount                            x1
    ///   SO_RecordCapture_AccumulatesValue                           x1
    ///   SO_RecordCapture_NegativeValue_ClampedToZero                x1
    ///   SO_AverageValue_CalculatesCorrectly                         x1
    ///   SO_RecordCapture_FiresEvent                                 x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_TrackerSO_Null                    x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlCaptureValueTrackerTests
    {
        private static ZoneControlCaptureValueTrackerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureValueTrackerSO>();

        private static ZoneControlCaptureValueTrackerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureValueTrackerController>();
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalValue_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalValue, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AverageValue_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AverageValue, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordCapture(100);
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AccumulatesValue()
        {
            var so = CreateSO();
            so.RecordCapture(100);
            so.RecordCapture(200);
            Assert.That(so.TotalValue, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_NegativeValue_ClampedToZero()
        {
            var so = CreateSO();
            so.RecordCapture(-50);
            Assert.That(so.TotalValue, Is.EqualTo(0));
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AverageValue_CalculatesCorrectly()
        {
            var so = CreateSO();
            so.RecordCapture(100);
            so.RecordCapture(300);
            Assert.That(so.AverageValue, Is.EqualTo(200f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureValueTrackerSO)
                .GetField("_onValueUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCapture(100);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCapture(100);
            so.RecordCapture(200);
            so.Reset();
            Assert.That(so.CaptureCount,  Is.EqualTo(0));
            Assert.That(so.TotalValue,    Is.EqualTo(0));
            Assert.That(so.AverageValue,  Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TrackerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrackerSO, Is.Null);
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
            typeof(ZoneControlCaptureValueTrackerController)
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

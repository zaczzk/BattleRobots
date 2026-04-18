using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T416: <see cref="ZoneControlCaptureGapSO"/> and
    /// <see cref="ZoneControlCaptureGapController"/>.
    ///
    /// ZoneControlCaptureGapTests (12):
    ///   SO_FreshInstance_HasFirstCapture_False              x1
    ///   SO_FreshInstance_FastCaptureCount_Zero              x1
    ///   SO_FirstCapture_DoesNotFire                         x1
    ///   SO_FastGap_FiresEvent                               x1
    ///   SO_FastGap_IncrementsCount                          x1
    ///   SO_SlowGap_DoesNotFire                             x1
    ///   SO_SlowGap_DoesNotIncrementCount                   x1
    ///   SO_LastGap_Recorded                                 x1
    ///   SO_Reset_ClearsAll                                  x1
    ///   Controller_FreshInstance_GapSO_Null                 x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow           x1
    ///   Controller_Refresh_NullSO_HidesPanel                x1
    /// </summary>
    public sealed class ZoneControlCaptureGapTests
    {
        private static ZoneControlCaptureGapSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureGapSO>();

        private static ZoneControlCaptureGapController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGapController>();
        }

        [Test]
        public void SO_FreshInstance_HasFirstCapture_False()
        {
            var so = CreateSO();
            Assert.That(so.HasFirstCapture, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FastCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FastCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_DoesNotFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGapSO)
                .GetField("_onFastCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0f);

            Assert.That(fired,              Is.EqualTo(0));
            Assert.That(so.HasFirstCapture, Is.True);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_FastGap_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGapSO)
                .GetField("_onFastCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0f);
            so.RecordCapture(so.FastGapThreshold - 0.1f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_FastGap_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(so.FastGapThreshold * 0.5f);
            Assert.That(so.FastCaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SlowGap_DoesNotFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGapSO)
                .GetField("_onFastCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0f);
            so.RecordCapture(so.FastGapThreshold + 1f);

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_SlowGap_DoesNotIncrementCount()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(so.FastGapThreshold + 10f);
            Assert.That(so.FastCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LastGap_Recorded()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            Assert.That(so.LastGap, Is.EqualTo(3f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.Reset();
            Assert.That(so.HasFirstCapture,  Is.False);
            Assert.That(so.FastCaptureCount, Is.EqualTo(0));
            Assert.That(so.LastGap,          Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GapSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GapSO, Is.Null);
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
            typeof(ZoneControlCaptureGapController)
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

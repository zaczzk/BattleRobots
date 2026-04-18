using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T428: <see cref="ZoneControlCaptureResilienceSO"/> and
    /// <see cref="ZoneControlCaptureResilienceController"/>.
    ///
    /// ZoneControlCaptureResilienceTests (12):
    ///   SO_FreshInstance_RecaptureCount_Zero                      x1
    ///   SO_FreshInstance_HasPendingLoss_False                     x1
    ///   SO_RecordZoneLost_SetsPendingLoss                         x1
    ///   SO_RecordRecapture_WithoutLoss_Ignored                    x1
    ///   SO_RecordRecapture_AfterLoss_IncrementsCount              x1
    ///   SO_RecordRecapture_AfterLoss_ComputesResponseTime         x1
    ///   SO_AverageResponseTime_ZeroWhenNoRecaptures               x1
    ///   SO_RecordRecapture_ClearsPendingLoss                      x1
    ///   SO_Reset_ClearsAll                                        x1
    ///   SO_RecordRecapture_FiresResilienceUpdatedEvent            x1
    ///   Controller_FreshInstance_ResilienceSO_Null                x1
    ///   Controller_Refresh_NullSO_HidesPanel                      x1
    /// </summary>
    public sealed class ZoneControlCaptureResilienceTests
    {
        private static ZoneControlCaptureResilienceSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureResilienceSO>();

        private static ZoneControlCaptureResilienceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureResilienceController>();
        }

        [Test]
        public void SO_FreshInstance_RecaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RecaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HasPendingLoss_False()
        {
            var so = CreateSO();
            Assert.That(so.HasPendingLoss, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordZoneLost_SetsPendingLoss()
        {
            var so = CreateSO();
            so.RecordZoneLost(5f);
            Assert.That(so.HasPendingLoss, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_WithoutLoss_Ignored()
        {
            var so = CreateSO();
            so.RecordRecapture(10f); // no pending loss
            Assert.That(so.RecaptureCount,    Is.EqualTo(0));
            Assert.That(so.TotalResponseTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_AfterLoss_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordZoneLost(0f);
            so.RecordRecapture(5f);
            Assert.That(so.RecaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_AfterLoss_ComputesResponseTime()
        {
            var so = CreateSO();
            so.RecordZoneLost(10f);
            so.RecordRecapture(13f); // 3-second response
            Assert.That(so.TotalResponseTime,   Is.EqualTo(3f).Within(0.001f));
            Assert.That(so.AverageResponseTime, Is.EqualTo(3f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AverageResponseTime_ZeroWhenNoRecaptures()
        {
            var so = CreateSO();
            Assert.That(so.AverageResponseTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_ClearsPendingLoss()
        {
            var so = CreateSO();
            so.RecordZoneLost(0f);
            so.RecordRecapture(2f);
            Assert.That(so.HasPendingLoss, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordZoneLost(0f);
            so.RecordRecapture(4f);
            so.Reset();
            Assert.That(so.RecaptureCount,    Is.EqualTo(0));
            Assert.That(so.TotalResponseTime, Is.EqualTo(0f));
            Assert.That(so.HasPendingLoss,    Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_FiresResilienceUpdatedEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureResilienceSO)
                .GetField("_onResilienceUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordZoneLost(0f);
            so.RecordRecapture(3f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_FreshInstance_ResilienceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ResilienceSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureResilienceController)
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

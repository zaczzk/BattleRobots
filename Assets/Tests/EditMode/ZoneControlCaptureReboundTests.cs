using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T442: <see cref="ZoneControlCaptureReboundSO"/> and
    /// <see cref="ZoneControlCaptureReboundController"/>.
    ///
    /// ZoneControlCaptureReboundTests (12):
    ///   SO_FreshInstance_ReboundCount_Zero                            x1
    ///   SO_FreshInstance_HasPendingLoss_False                         x1
    ///   SO_RecordZoneLost_SetsPendingLoss                             x1
    ///   SO_RecordRecapture_NoPendingLoss_NoRebound                    x1
    ///   SO_RecordRecapture_WithinWindow_CountsRebound                 x1
    ///   SO_RecordRecapture_OutsideWindow_NoRebound                    x1
    ///   SO_RecordRecapture_ClearsPendingLoss_Always                   x1
    ///   SO_RecordCapture_FiresOnRebound                               x1
    ///   SO_Reset_ClearsAll                                            x1
    ///   Controller_FreshInstance_ReboundSO_Null                      x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlCaptureReboundTests
    {
        private static ZoneControlCaptureReboundSO CreateSO(float window = 10f, int bonus = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureReboundSO>();
            typeof(ZoneControlCaptureReboundSO)
                .GetField("_reboundWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, window);
            typeof(ZoneControlCaptureReboundSO)
                .GetField("_bonusPerRebound", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureReboundController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureReboundController>();
        }

        [Test]
        public void SO_FreshInstance_ReboundCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReboundCount, Is.EqualTo(0));
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
        public void SO_RecordRecapture_NoPendingLoss_NoRebound()
        {
            var so = CreateSO(window: 10f);
            so.RecordRecapture(5f);
            Assert.That(so.ReboundCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_WithinWindow_CountsRebound()
        {
            var so = CreateSO(window: 10f, bonus: 150);
            so.RecordZoneLost(0f);
            so.RecordRecapture(5f);
            Assert.That(so.ReboundCount,      Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_OutsideWindow_NoRebound()
        {
            var so = CreateSO(window: 5f);
            so.RecordZoneLost(0f);
            so.RecordRecapture(10f);
            Assert.That(so.ReboundCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRecapture_ClearsPendingLoss_Always()
        {
            var so = CreateSO(window: 5f);
            so.RecordZoneLost(0f);
            so.RecordRecapture(20f); // outside window
            Assert.That(so.HasPendingLoss, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnRebound()
        {
            var so      = CreateSO(window: 10f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureReboundSO)
                .GetField("_onRebound", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordZoneLost(0f);
            Assert.That(fired, Is.EqualTo(0));
            so.RecordRecapture(3f);
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(window: 10f, bonus: 150);
            so.RecordZoneLost(0f);
            so.RecordRecapture(2f);
            so.Reset();
            Assert.That(so.ReboundCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.HasPendingLoss,    Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ReboundSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ReboundSO, Is.Null);
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
            typeof(ZoneControlCaptureReboundController)
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

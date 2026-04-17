using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T394: <see cref="ZoneControlFirstCaptureBonusSO"/> and
    /// <see cref="ZoneControlFirstCaptureBonusController"/>.
    ///
    /// ZoneControlFirstCaptureBonusTests (12):
    ///   SO_FreshInstance_HasFired_False                          x1
    ///   SO_FreshInstance_BonusAmount_DefaultIs500                x1
    ///   SO_RecordCapture_SetsFired                               x1
    ///   SO_RecordCapture_Idempotent_AfterFirstCapture            x1
    ///   SO_RecordCapture_FiresOnFirstCapture                     x1
    ///   SO_RecordCapture_NoSecondFire                            x1
    ///   SO_Reset_ClearsFired                                     x1
    ///   Controller_FreshInstance_BonusSO_Null                    x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               x1
    ///   Controller_Refresh_NullSO_HidesPanel                     x1
    ///   Controller_Refresh_WithSO_ShowsPanel                     x1
    /// </summary>
    public sealed class ZoneControlFirstCaptureBonusTests
    {
        private static ZoneControlFirstCaptureBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlFirstCaptureBonusSO>();

        private static ZoneControlFirstCaptureBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlFirstCaptureBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_HasFired_False()
        {
            var so = CreateSO();
            Assert.That(so.HasFired, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BonusAmount_DefaultIs500()
        {
            var so = CreateSO();
            Assert.That(so.BonusAmount, Is.EqualTo(500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_SetsFired()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.HasFired, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_Idempotent_AfterFirstCapture()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.HasFired, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnFirstCapture()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFirstCaptureBonusSO)
                .GetField("_onFirstCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_NoSecondFire()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFirstCaptureBonusSO)
                .GetField("_onFirstCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordCapture();
            so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsFired()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.HasFired, Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BonusSO, Is.Null);
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
            typeof(ZoneControlFirstCaptureBonusController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlFirstCaptureBonusController)
                .GetField("_bonusSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlFirstCaptureBonusController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(false);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }
    }
}

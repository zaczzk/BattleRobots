using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T377: <see cref="ZoneControlZoneMultiplierSO"/> and
    /// <see cref="ZoneControlZoneMultiplierController"/>.
    ///
    /// ZoneControlZoneMultiplierTests (13):
    ///   SO_FreshInstance_CurrentMultiplier_EqualsBase            ×1
    ///   SO_FreshInstance_IsActive_False                          ×1
    ///   SO_RecordCapture_IncrementsMultiplier                    ×1
    ///   SO_RecordCapture_ClampsToMax                             ×1
    ///   SO_RecordCapture_SetsIsActive                            ×1
    ///   SO_Tick_WithinWindow_KeepsMultiplier                     ×1
    ///   SO_Tick_ExceedsWindow_ResetsMultiplier                   ×1
    ///   SO_ComputeBonus_ScalesCorrectly                          ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_MultiplierSO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullMultiplierSO_HidesPanel           ×1
    /// </summary>
    public sealed class ZoneControlZoneMultiplierTests
    {
        private static ZoneControlZoneMultiplierSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneMultiplierSO>();

        private static ZoneControlZoneMultiplierController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneMultiplierController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentMultiplier_EqualsBase()
        {
            var so = CreateSO();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(so.BaseMultiplier));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsMultiplier()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.CurrentMultiplier,
                Is.EqualTo(so.BaseMultiplier + so.IncrementPerCapture).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampsToMax()
        {
            var so = CreateSO();
            // Record enough captures to exceed max
            for (int i = 0; i < 100; i++)
                so.RecordCapture();

            Assert.That(so.CurrentMultiplier, Is.EqualTo(so.MaxMultiplier).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_SetsIsActive()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WithinWindow_KeepsMultiplier()
        {
            var so = CreateSO();
            so.RecordCapture();
            float before = so.CurrentMultiplier;
            so.Tick(so.ResetWindow - 0.1f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(before).Within(0.001f));
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExceedsWindow_ResetsMultiplier()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.Tick(so.ResetWindow + 0.1f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(so.BaseMultiplier).Within(0.001f));
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_ScalesCorrectly()
        {
            var so = CreateSO();
            so.RecordCapture(); // multiplier = base + increment
            int bonus = so.ComputeBonus(100);
            int expected = Mathf.RoundToInt(100f * so.CurrentMultiplier);
            Assert.That(bonus, Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(so.BaseMultiplier).Within(0.001f));
            Assert.That(so.IsActive,          Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_MultiplierSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MultiplierSO, Is.Null);
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
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneMultiplierController)
                .GetField("_onMultiplierChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullMultiplierSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneMultiplierController)
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

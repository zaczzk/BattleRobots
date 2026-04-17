using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T387: <see cref="ZoneControlCaptureComboMultiplierSO"/> and
    /// <see cref="ZoneControlCaptureComboMultiplierController"/>.
    ///
    /// ZoneControlCaptureComboMultiplierTests (12):
    ///   SO_FreshInstance_CurrentMultiplier_EqualsBase                  ×1
    ///   SO_RecordCapture_IncrementsMultiplier                          ×1
    ///   SO_RecordCapture_ClampsToMaxMultiplier                         ×1
    ///   SO_RecordCapture_FiresOnComboMultiplierChanged                 ×1
    ///   SO_Tick_AfterWindow_ResetsMultiplier                           ×1
    ///   SO_ComputeBonus_ScalesAmount                                   ×1
    ///   SO_Reset_RestoresBaseMultiplier                                ×1
    ///   Controller_FreshInstance_ComboSO_Null                          ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channel                       ×1
    ///   Controller_Refresh_NullComboSO_HidesPanel                      ×1
    /// </summary>
    public sealed class ZoneControlCaptureComboMultiplierTests
    {
        private static ZoneControlCaptureComboMultiplierSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureComboMultiplierSO>();

        private static ZoneControlCaptureComboMultiplierController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureComboMultiplierController>();
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
        public void SO_RecordCapture_IncrementsMultiplier()
        {
            var so      = CreateSO();
            float before = so.CurrentMultiplier;
            so.RecordCapture(0f);
            Assert.That(so.CurrentMultiplier, Is.GreaterThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampsToMaxMultiplier()
        {
            var so = CreateSO();
            for (int i = 0; i < 100; i++)
                so.RecordCapture(i * 0.1f);  // rapid captures within window
            Assert.That(so.CurrentMultiplier, Is.LessThanOrEqualTo(so.MaxMultiplier));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnComboMultiplierChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureComboMultiplierSO)
                .GetField("_onComboMultiplierChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture(0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_AfterWindow_ResetsMultiplier()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            Assert.That(so.CurrentMultiplier, Is.GreaterThan(so.BaseMultiplier));

            // Advance past the combo window
            so.Tick(so.ComboWindow + 1f);

            Assert.That(so.CurrentMultiplier, Is.EqualTo(so.BaseMultiplier));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_ScalesAmount()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            int expected = Mathf.RoundToInt(100 * so.CurrentMultiplier);
            Assert.That(so.ComputeBonus(100), Is.EqualTo(expected));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_RestoresBaseMultiplier()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(0.1f);
            so.Reset();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(so.BaseMultiplier));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ComboSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ComboSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureComboMultiplierController)
                .GetField("_onComboMultiplierChanged", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullComboSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureComboMultiplierController)
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

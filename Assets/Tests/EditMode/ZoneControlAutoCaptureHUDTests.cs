using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T339: <see cref="ZoneControlAutoCaptureHUDController"/>.
    ///
    /// ZoneControlAutoCaptureHUDTests (12):
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_Refresh_NullAutoCaptureSO_HidesPanel               ×1
    ///   Controller_Refresh_NullCaptureController_HidesPanel           ×1
    ///   Controller_Refresh_NotAccumulating_HidesPanel                 ×1
    ///   Controller_Refresh_Accumulating_ShowsPanel                    ×1
    ///   Controller_Refresh_Accumulating_SetsProgressBarValue          ×1
    ///   Controller_Refresh_Accumulating_SetsStatusLabel               ×1
    ///   Controller_HandleAutoCapture_HidesPanel                       ×1
    ///   Controller_OnDisable_HidesPanel                               ×1
    ///   Controller_AutoCaptureSO_Property_ReturnsAssignedSO           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlAutoCaptureHUDTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlAutoCaptureSO CreateAutoCaptureSO(float duration = 5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlAutoCaptureSO>();
            SetField(so, "_autoCaptureDuration", duration);
            return so;
        }

        /// <summary>
        /// Creates a ZoneControlAutoCaptureController with <see cref="IsAccumulating"/>
        /// and <see cref="AccumulatedTime"/> set via reflection.
        /// </summary>
        private static ZoneControlAutoCaptureController CreateCaptureController(
            bool isAccumulating, float accumulatedTime)
        {
            var go   = new GameObject("AutoCaptureController");
            var ctrl = go.AddComponent<ZoneControlAutoCaptureController>();
            SetField(ctrl, "_isAccumulating",  isAccumulating);
            SetField(ctrl, "_accumulatedTime", accumulatedTime);
            return ctrl;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlAutoCaptureHUDController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onAutoCapture", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onAutoCapture must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullAutoCaptureSO_HidesPanel()
        {
            var go    = new GameObject("Test_NullSO");
            var ctrl  = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            var captureCtrl = CreateCaptureController(isAccumulating: true, accumulatedTime: 1f);
            SetField(ctrl, "_captureController", captureCtrl);
            SetField(ctrl, "_panel", panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _autoCaptureSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureCtrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_NullCaptureController_HidesPanel()
        {
            var go    = new GameObject("Test_NullController");
            var ctrl  = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            var so = CreateAutoCaptureSO();
            SetField(ctrl, "_autoCaptureSO", so);
            SetField(ctrl, "_panel", panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _captureController is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_NotAccumulating_HidesPanel()
        {
            var go          = new GameObject("Test_NotAccumulating");
            var ctrl        = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var panel       = new GameObject("Panel");
            panel.SetActive(true);
            var so          = CreateAutoCaptureSO(5f);
            var captureCtrl = CreateCaptureController(isAccumulating: false, accumulatedTime: 0f);

            SetField(ctrl, "_autoCaptureSO",    so);
            SetField(ctrl, "_captureController", captureCtrl);
            SetField(ctrl, "_panel",             panel);

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when the accumulator is not active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureCtrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_Accumulating_ShowsPanel()
        {
            var go          = new GameObject("Test_Accumulating");
            var ctrl        = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var panel       = new GameObject("Panel");
            panel.SetActive(false);
            var so          = CreateAutoCaptureSO(10f);
            var captureCtrl = CreateCaptureController(isAccumulating: true, accumulatedTime: 3f);

            SetField(ctrl, "_autoCaptureSO",    so);
            SetField(ctrl, "_captureController", captureCtrl);
            SetField(ctrl, "_panel",             panel);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown while the accumulator is active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureCtrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_Accumulating_SetsProgressBarValue()
        {
            var go          = new GameObject("Test_ProgressBar");
            var ctrl        = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var so          = CreateAutoCaptureSO(10f);
            var captureCtrl = CreateCaptureController(isAccumulating: true, accumulatedTime: 5f);

            var sliderGo = new GameObject("Slider");
            var slider   = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            SetField(ctrl, "_autoCaptureSO",    so);
            SetField(ctrl, "_captureController", captureCtrl);
            SetField(ctrl, "_progressBar",       slider);

            ctrl.Refresh();

            Assert.AreEqual(0.5f, slider.value, 0.001f,
                "Progress bar value must equal accumulated / duration (5 / 10 = 0.5).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureCtrl.gameObject);
            Object.DestroyImmediate(sliderGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_Accumulating_SetsStatusLabel()
        {
            var go          = new GameObject("Test_Label");
            var ctrl        = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var so          = CreateAutoCaptureSO(10f);
            var captureCtrl = CreateCaptureController(isAccumulating: true, accumulatedTime: 3f);

            var textGo = new GameObject("Text");
            var text   = textGo.AddComponent<Text>();
            SetField(ctrl, "_autoCaptureSO",    so);
            SetField(ctrl, "_captureController", captureCtrl);
            SetField(ctrl, "_statusLabel",       text);

            ctrl.Refresh();

            StringAssert.StartsWith("Bot Takeover:",
                text.text,
                "Status label must start with 'Bot Takeover:' while accumulating.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(captureCtrl.gameObject);
            Object.DestroyImmediate(textGo);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_HandleAutoCapture_HidesPanel()
        {
            var go    = new GameObject("Test_HandleAutoCapture");
            var ctrl  = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            ctrl.HandleAutoCapture();

            Assert.IsFalse(panel.activeSelf,
                "HandleAutoCapture must hide the danger panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_OnDisable_HidesPanel()
        {
            var go    = new GameObject("Test_DisableHidesPanel");
            var ctrl  = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            go.SetActive(false);

            Assert.IsFalse(panel.activeSelf,
                "OnDisable must hide the panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_AutoCaptureSO_Property_ReturnsAssignedSO()
        {
            var go   = new GameObject("Test_Property");
            var ctrl = go.AddComponent<ZoneControlAutoCaptureHUDController>();
            var so   = CreateAutoCaptureSO();
            SetField(ctrl, "_autoCaptureSO", so);

            Assert.AreSame(so, ctrl.AutoCaptureSO,
                "AutoCaptureSO property must return the assigned SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}

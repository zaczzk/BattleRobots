using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T258: <see cref="ZoneCaptureNotificationController"/>.
    ///
    /// ZoneCaptureNotificationControllerTests (12):
    ///   FreshInstance_NotificationDuration_Default_Two                  ×1
    ///   FreshInstance_DisplayTimer_Zero                                 ×1
    ///   FreshInstance_CapturedMessage_Default                           ×1
    ///   FreshInstance_LostMessage_Default                               ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_BothChannels                              ×1
    ///   OnDisable_HidesPanel                                            ×1
    ///   ShowCapture_SetsDisplayTimer                                    ×1
    ///   ShowCapture_ShowsPanel                                          ×1
    ///   ShowLost_SetsDisplayTimer                                       ×1
    ///   Tick_ExpiresTimer_HidesPanel                                    ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCaptureNotificationControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneCaptureNotificationController CreateController() =>
            new GameObject("ZoneCaptureNotif_Test")
                .AddComponent<ZoneCaptureNotificationController>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_NotificationDuration_Default_Two()
        {
            var ctrl = CreateController();
            Assert.AreEqual(2f, ctrl.NotificationDuration, 0.001f,
                "NotificationDuration must default to 2 seconds.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_DisplayTimer_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f,
                "DisplayTimer must be 0 on a fresh ZoneCaptureNotificationController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_CapturedMessage_Default()
        {
            var ctrl = CreateController();
            Assert.AreEqual("ZONE CAPTURED!", ctrl.CapturedMessage,
                "CapturedMessage must default to 'ZONE CAPTURED!'.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_LostMessage_Default()
        {
            var ctrl = CreateController();
            Assert.AreEqual("ZONE LOST!", ctrl.LostMessage,
                "LostMessage must default to 'ZONE LOST!'.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_BothChannels()
        {
            var ctrl     = CreateController();
            var panel    = CreatePanel();
            var captured = CreateEvent();
            var lost     = CreateEvent();

            SetField(ctrl, "_onZoneCaptured",     captured);
            SetField(ctrl, "_onZoneLost",         lost);
            SetField(ctrl, "_notificationPanel",  panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After disable, events must not re-activate the panel.
            panel.SetActive(false);
            captured.Raise();
            Assert.IsFalse(panel.activeSelf,
                "After OnDisable, _onZoneCaptured must not show the panel.");
            lost.Raise();
            Assert.IsFalse(panel.activeSelf,
                "After OnDisable, _onZoneLost must not show the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(captured);
            Object.DestroyImmediate(lost);
        }

        [Test]
        public void OnDisable_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            // Manually set timer so it looks active.
            SetField(ctrl, "_displayTimer", 1f);

            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf,
                "OnDisable must hide the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void ShowCapture_SetsDisplayTimer()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            ctrl.ShowCapture();

            Assert.AreEqual(ctrl.NotificationDuration, ctrl.DisplayTimer, 0.001f,
                "ShowCapture must reset DisplayTimer to NotificationDuration.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void ShowCapture_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(false);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.ShowCapture();

            Assert.IsTrue(panel.activeSelf,
                "ShowCapture must activate the notification panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void ShowLost_SetsDisplayTimer()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            ctrl.ShowLost();

            Assert.AreEqual(ctrl.NotificationDuration, ctrl.DisplayTimer, 0.001f,
                "ShowLost must reset DisplayTimer to NotificationDuration.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_ExpiresTimer_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);
            SetField(ctrl, "_notificationPanel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.ShowCapture(); // sets timer = 2f, shows panel

            // Advance past the duration.
            ctrl.Tick(3f);

            Assert.AreEqual(0f, ctrl.DisplayTimer, 0.001f,
                "DisplayTimer must clamp to 0 after expiry.");
            Assert.IsFalse(panel.activeSelf,
                "Tick must hide the panel when the display timer expires.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

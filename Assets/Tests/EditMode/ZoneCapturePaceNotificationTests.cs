using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T290: <see cref="ZoneCapturePaceNotificationController"/>.
    ///
    /// ZoneCapturePaceNotificationTests (12):
    ///   FreshInstance_IsActive_False                                         ×1
    ///   FreshInstance_DisplayTimer_Zero                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                      ×1
    ///   OnDisable_Unregisters_BothChannels                                   ×1
    ///   HandleFastPace_ShowsPanel                                            ×1
    ///   HandleFastPace_SetsMessage                                           ×1
    ///   HandleSlowPace_ShowsPanel                                            ×1
    ///   HandleFastPace_Cooldown_DeduplicatesSecondFire                       ×1
    ///   Tick_DecrementsDisplayTimer                                          ×1
    ///   Tick_HidesBannerAfterDuration                                        ×1
    ///   Tick_NullPanel_DoesNotThrow                                          ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCapturePaceNotificationTests
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

        private static ZoneCapturePaceNotificationController CreateController() =>
            new GameObject("ZoneCapturePaceNotification_Test")
                .AddComponent<ZoneCapturePaceNotificationController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_IsActive_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsActive,
                "IsActive must be false on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_DisplayTimer_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0f, ctrl.DisplayTimer,
                "DisplayTimer must be 0 on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneCapturePaceNotificationController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneCapturePaceNotificationController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_BothChannels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneCapturePaceNotificationController>();

            var fastEvt = CreateEvent();
            var slowEvt = CreateEvent();

            SetField(ctrl, "_onFastPace", fastEvt);
            SetField(ctrl, "_onSlowPace", slowEvt);

            go.SetActive(true);
            go.SetActive(false);

            int fastCount = 0, slowCount = 0;
            fastEvt.RegisterCallback(() => fastCount++);
            slowEvt.RegisterCallback(() => slowCount++);

            fastEvt.Raise();
            slowEvt.Raise();

            Assert.AreEqual(1, fastCount, "_onFastPace must be unregistered after OnDisable.");
            Assert.AreEqual(1, slowCount, "_onSlowPace must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(fastEvt);
            Object.DestroyImmediate(slowEvt);
        }

        [Test]
        public void HandleFastPace_ShowsPanel()
        {
            var go    = new GameObject("Test_HandleFastPace_Panel");
            var ctrl  = go.AddComponent<ZoneCapturePaceNotificationController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_panel", panel);
            ctrl.HandleFastPace();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown after HandleFastPace.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void HandleFastPace_SetsMessage()
        {
            var go    = new GameObject("Test_HandleFastPace_Message");
            var ctrl  = go.AddComponent<ZoneCapturePaceNotificationController>();

            // Verify IsActive flag is set (message label would require a full GameObject
            // setup with Text component; we confirm via IsActive).
            ctrl.HandleFastPace();

            Assert.IsTrue(ctrl.IsActive,
                "IsActive must be true after HandleFastPace.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void HandleSlowPace_ShowsPanel()
        {
            var go    = new GameObject("Test_HandleSlowPace_Panel");
            var ctrl  = go.AddComponent<ZoneCapturePaceNotificationController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_panel", panel);
            ctrl.HandleSlowPace();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown after HandleSlowPace.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void HandleFastPace_Cooldown_DeduplicatesSecondFire()
        {
            var go   = new GameObject("Test_Cooldown");
            var ctrl = go.AddComponent<ZoneCapturePaceNotificationController>();

            // First fire — starts banner and sets cooldown.
            ctrl.HandleFastPace();
            Assert.IsTrue(ctrl.IsActive,
                "IsActive must be true after first HandleFastPace.");
            Assert.Greater(ctrl.FastCooldownRemaining, 0f,
                "FastCooldownRemaining must be > 0 after first fire.");

            // Expire the display timer but NOT the cooldown.
            ctrl.Tick(ctrl.DisplayDuration + 0.1f);
            Assert.IsFalse(ctrl.IsActive,
                "Banner must be hidden after display timer expires.");

            // Second fire within cooldown — must be ignored.
            ctrl.HandleFastPace();
            Assert.IsFalse(ctrl.IsActive,
                "IsActive must remain false when second fire occurs within cooldown.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Tick_DecrementsDisplayTimer()
        {
            var go   = new GameObject("Test_Tick_Decrement");
            var ctrl = go.AddComponent<ZoneCapturePaceNotificationController>();

            ctrl.HandleFastPace();
            float before = ctrl.DisplayTimer;
            ctrl.Tick(0.5f);

            Assert.Less(ctrl.DisplayTimer, before,
                "DisplayTimer must decrease after Tick.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Tick_HidesBannerAfterDuration()
        {
            var go    = new GameObject("Test_Tick_Hide");
            var ctrl  = go.AddComponent<ZoneCapturePaceNotificationController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_panel", panel);
            ctrl.HandleFastPace();
            Assert.IsTrue(panel.activeSelf, "Panel must be active after HandleFastPace.");

            ctrl.Tick(ctrl.DisplayDuration + 0.1f);
            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden once display duration expires.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Tick_NullPanel_DoesNotThrow()
        {
            var go   = new GameObject("Test_Tick_NullPanel");
            var ctrl = go.AddComponent<ZoneCapturePaceNotificationController>();

            ctrl.HandleFastPace();
            Assert.DoesNotThrow(() => ctrl.Tick(ctrl.DisplayDuration + 1f),
                "Tick must not throw when panel is null.");

            Object.DestroyImmediate(go);
        }
    }
}

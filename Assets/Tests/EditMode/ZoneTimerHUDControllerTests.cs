using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T264: <see cref="ZoneTimerHUDController"/>.
    ///
    /// ZoneTimerHUDControllerTests (12):
    ///   FreshInstance_TimerSO_Null                                      ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_Channels                                  ×1
    ///   Refresh_NullSO_HidesPanel                                       ×1
    ///   Refresh_NotOnCooldown_HidesPanel                                ×1
    ///   Refresh_OnCooldown_ShowsPanel                                   ×1
    ///   Refresh_OnCooldown_ShowsLockedBadge                             ×1
    ///   Refresh_TimerLabel_Format                                       ×1
    ///   Tick_NullSO_DoesNotThrow                                        ×1
    ///   CooldownStarted_Event_ShowsPanel                                ×1
    ///   CooldownEnded_Event_HidesPanel                                  ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneTimerHUDControllerTests
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

        private static ZoneTimerSO CreateTimerSO() =>
            ScriptableObject.CreateInstance<ZoneTimerSO>();

        private static ZoneTimerHUDController CreateController() =>
            new GameObject("ZoneTimerHUD_Test").AddComponent<ZoneTimerHUDController>();

        private static GameObject CreatePanel() => new GameObject("Panel_Test");

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_TimerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TimerSO,
                "TimerSO must be null on a fresh ZoneTimerHUDController.");
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
        public void OnDisable_Unregisters_Channels()
        {
            var ctrl        = CreateController();
            var so          = CreateTimerSO();
            var panel       = CreatePanel();
            var evtStarted  = CreateEvent();
            var evtEnded    = CreateEvent();

            SetField(ctrl, "_timerSO",            so);
            SetField(ctrl, "_onCooldownStarted",  evtStarted);
            SetField(ctrl, "_onCooldownEnded",    evtEnded);
            SetField(ctrl, "_panel",              panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raising events after disable must not toggle the panel.
            panel.SetActive(false);
            evtStarted.Raise();
            Assert.IsFalse(panel.activeSelf,
                "After OnDisable, _onCooldownStarted must not activate the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(evtStarted);
            Object.DestroyImmediate(evtEnded);
        }

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = CreatePanel();
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when TimerSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NotOnCooldown_HidesPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateTimerSO();
            var panel = CreatePanel();
            panel.SetActive(true);

            SetField(ctrl, "_timerSO", so);
            SetField(ctrl, "_panel",   panel);

            // so.IsOnCooldown is false by default.
            Assert.IsFalse(so.IsOnCooldown, "Pre-condition: timer must not be on cooldown.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when IsOnCooldown is false.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_OnCooldown_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateTimerSO();
            var panel = CreatePanel();
            panel.SetActive(false);

            SetField(ctrl, "_timerSO", so);
            SetField(ctrl, "_panel",   panel);

            so.StartCooldown();
            Assert.IsTrue(so.IsOnCooldown, "Pre-condition: timer must be on cooldown.");

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh must show the panel when IsOnCooldown is true.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_OnCooldown_ShowsLockedBadge()
        {
            var ctrl   = CreateController();
            var so     = CreateTimerSO();
            var locked = CreatePanel();
            locked.SetActive(false);

            SetField(ctrl, "_timerSO",    so);
            SetField(ctrl, "_lockedBadge", locked);

            so.StartCooldown();

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(locked.activeSelf,
                "Refresh must show _lockedBadge while IsOnCooldown is true.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(locked);
        }

        [Test]
        public void Refresh_TimerLabel_Format()
        {
            var ctrl    = CreateController();
            var so      = CreateTimerSO();
            var goLabel = new GameObject("Label_Test");
            var label   = goLabel.AddComponent<Text>();

            SetField(ctrl, "_timerSO",    so);
            SetField(ctrl, "_timerLabel", label);

            so.StartCooldown();

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            // RemainingCooldown = CooldownDuration (5f by default).
            StringAssert.EndsWith("s", label.text,
                "Timer label must end with 's' (e.g., '5.0s').");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(goLabel);
        }

        [Test]
        public void Tick_NullSO_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Tick(0.016f),
                "Tick with null TimerSO must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void CooldownStarted_Event_ShowsPanel()
        {
            var ctrl       = CreateController();
            var so         = CreateTimerSO();
            var panel      = CreatePanel();
            var evtStarted = CreateEvent();

            panel.SetActive(false);
            SetField(ctrl, "_timerSO",           so);
            SetField(ctrl, "_onCooldownStarted", evtStarted);
            SetField(ctrl, "_panel",             panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            so.StartCooldown();
            evtStarted.Raise();

            Assert.IsTrue(panel.activeSelf,
                "_onCooldownStarted event must show the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(evtStarted);
        }

        [Test]
        public void CooldownEnded_Event_HidesPanel()
        {
            var ctrl     = CreateController();
            var so       = CreateTimerSO();
            var panel    = CreatePanel();
            var evtEnded = CreateEvent();

            panel.SetActive(true);
            SetField(ctrl, "_timerSO",          so);
            SetField(ctrl, "_onCooldownEnded",  evtEnded);
            SetField(ctrl, "_panel",            panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            evtEnded.Raise();

            Assert.IsFalse(panel.activeSelf,
                "_onCooldownEnded event must hide the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(evtEnded);
        }
    }
}

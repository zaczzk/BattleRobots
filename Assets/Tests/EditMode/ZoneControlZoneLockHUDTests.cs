using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T324: <see cref="ZoneControlZoneLockHUDController"/>.
    ///
    /// ZoneControlZoneLockHUDTests (12):
    ///   FreshInstance_LockSO_Null                                        ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                   ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_Unregisters_Channel                                    ×1
    ///   Refresh_NullLockSO_HidesPanel                                    ×1
    ///   Refresh_NotLocked_HidesPanel                                     ×1
    ///   Refresh_Locked_ShowsPanel                                        ×1
    ///   Refresh_Locked_ProgressBar_Updated                               ×1
    ///   Refresh_Locked_Label_ShowsTimer                                  ×1
    ///   Refresh_NotLocked_Label_ShowsAvailable                           ×1
    ///   OnZoneLocked_Raise_CallsRefresh                                  ×1
    ///   OnZoneUnlocked_Raise_CallsRefresh                                ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlZoneLockHUDTests
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

        private static ZoneControlZoneLockSO CreateLockSO(float duration = 5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneLockSO>();
            SetField(so, "_lockDuration", duration);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneLockHUDController CreateController() =>
            new GameObject("LockHUD_Test")
                .AddComponent<ZoneControlZoneLockHUDController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_LockSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.LockSO,
                "LockSO must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlZoneLockHUDController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlZoneLockHUDController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlZoneLockHUDController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onZoneLocked", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onZoneLocked must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullLockSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when LockSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NotLocked_HidesPanel()
        {
            var go    = new GameObject("Test_NotLocked");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so    = CreateLockSO();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_lockSO", so);
            SetField(ctrl, "_panel",  panel);

            ctrl.Refresh(); // zone is not locked

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when zone is not locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_Locked_ShowsPanel()
        {
            var go    = new GameObject("Test_Locked");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so    = CreateLockSO();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_lockSO", so);
            SetField(ctrl, "_panel",  panel);

            so.LockZone();
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when zone is locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_Locked_ProgressBar_Updated()
        {
            var go   = new GameObject("Test_ProgressBar");
            var ctrl = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so   = CreateLockSO(duration: 5f);

            var barGO = new GameObject("Bar");
            var bar   = barGO.AddComponent<Slider>();

            SetField(ctrl, "_lockSO",           so);
            SetField(ctrl, "_lockProgressBar",  bar);

            so.LockZone();
            ctrl.Refresh();

            Assert.Greater(bar.value, 0f,
                "_lockProgressBar.value must be > 0 while zone is locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(barGO);
        }

        [Test]
        public void Refresh_Locked_Label_ShowsTimer()
        {
            var go    = new GameObject("Test_Label_Locked");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so    = CreateLockSO(duration: 5f);

            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_lockSO",    so);
            SetField(ctrl, "_lockLabel", label);

            so.LockZone();
            ctrl.Refresh();

            StringAssert.Contains("Locked:", label.text,
                "_lockLabel must contain 'Locked:' while zone is locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Refresh_NotLocked_Label_ShowsAvailable()
        {
            var go    = new GameObject("Test_Label_Available");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so    = CreateLockSO();

            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_lockSO",    so);
            SetField(ctrl, "_lockLabel", label);

            ctrl.Refresh(); // not locked

            Assert.AreEqual("Available", label.text,
                "_lockLabel must show 'Available' when zone is not locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void OnZoneLocked_Raise_CallsRefresh()
        {
            var go    = new GameObject("Test_ZoneLocked_Event");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so    = CreateLockSO();
            var evt   = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_lockSO",       so);
            SetField(ctrl, "_onZoneLocked", evt);
            SetField(ctrl, "_panel",        panel);

            go.SetActive(true); // subscribes

            so.LockZone();       // sets locked state first
            evt.Raise();         // triggers Refresh

            Assert.IsTrue(panel.activeSelf,
                "Raising _onZoneLocked must trigger Refresh and show panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnZoneUnlocked_Raise_CallsRefresh()
        {
            var go    = new GameObject("Test_ZoneUnlocked_Event");
            var ctrl  = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so    = CreateLockSO();
            var evt   = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_lockSO",         so);
            SetField(ctrl, "_onZoneUnlocked", evt);
            SetField(ctrl, "_panel",          panel);

            go.SetActive(true); // subscribes

            // zone is NOT locked, raise unlock event
            evt.Raise(); // triggers Refresh → panel should hide

            Assert.IsFalse(panel.activeSelf,
                "Raising _onZoneUnlocked must trigger Refresh and hide panel when not locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(panel);
        }
    }
}

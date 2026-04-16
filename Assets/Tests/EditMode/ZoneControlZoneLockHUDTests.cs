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
    ///   FreshInstance_IsLocked_False                                  ×1
    ///   FreshInstance_LockSO_Null                                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                ×1
    ///   OnDisable_NullRefs_DoesNotThrow                               ×1
    ///   OnDisable_Unregisters_Channel                                 ×1
    ///   Refresh_NullLockSO_HidesPanel                                 ×1
    ///   HandleZoneLocked_SetsIsLocked_True                            ×1
    ///   HandleZoneUnlocked_SetsIsLocked_False                         ×1
    ///   HandleMatchStarted_ResetsIsLocked                             ×1
    ///   Refresh_NotLocked_ShowsAvailable                              ×1
    ///   Refresh_Locked_ShowsLockedLabel                               ×1
    ///   Tick_NotLocked_DoesNotThrow                                   ×1
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
        public void FreshInstance_IsLocked_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsLocked,
                "IsLocked must be false on a fresh controller instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_LockSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.LockSO,
                "LockSO must be null on a fresh controller instance.");
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
        public void HandleZoneLocked_SetsIsLocked_True()
        {
            var ctrl = CreateController();
            ctrl.HandleZoneLocked();
            Assert.IsTrue(ctrl.IsLocked,
                "HandleZoneLocked must set IsLocked to true.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleZoneUnlocked_SetsIsLocked_False()
        {
            var ctrl = CreateController();
            ctrl.HandleZoneLocked();
            ctrl.HandleZoneUnlocked();
            Assert.IsFalse(ctrl.IsLocked,
                "HandleZoneUnlocked must set IsLocked to false.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_ResetsIsLocked()
        {
            var ctrl = CreateController();
            ctrl.HandleZoneLocked();
            ctrl.HandleMatchStarted();
            Assert.IsFalse(ctrl.IsLocked,
                "HandleMatchStarted must reset IsLocked to false.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NotLocked_ShowsAvailable()
        {
            var go   = new GameObject("Test_Refresh_Available");
            var ctrl = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so   = CreateLockSO();

            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_lockSO",    so);
            SetField(ctrl, "_lockLabel", label);

            ctrl.Refresh();

            Assert.AreEqual("Available", label.text,
                "Lock label must show 'Available' when zone is not locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Refresh_Locked_ShowsLockedLabel()
        {
            var go   = new GameObject("Test_Refresh_Locked");
            var ctrl = go.AddComponent<ZoneControlZoneLockHUDController>();
            var so   = CreateLockSO(duration: 5f);

            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            so.LockZone();
            SetField(ctrl, "_lockSO",    so);
            SetField(ctrl, "_lockLabel", label);

            ctrl.Refresh();

            StringAssert.StartsWith("Locked:", label.text,
                "Lock label must start with 'Locked:' when zone is locked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Tick_NotLocked_DoesNotThrow()
        {
            var go   = new GameObject("Test_Tick_NotLocked");
            var ctrl = go.AddComponent<ZoneControlZoneLockHUDController>();
            Assert.DoesNotThrow(
                () => ctrl.Tick(0.1f),
                "Tick must not throw when zone is not locked.");
            Object.DestroyImmediate(go);
        }
    }
}

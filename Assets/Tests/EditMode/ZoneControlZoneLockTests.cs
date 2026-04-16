using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T320: <see cref="ZoneControlZoneLockSO"/> and
    /// <see cref="ZoneControlZoneLockController"/>.
    ///
    /// ZoneControlZoneLockTests (12):
    ///   SO_FreshInstance_IsLocked_False                                          ×1
    ///   SO_LockZone_SetsIsLocked                                                 ×1
    ///   SO_LockZone_FiresOnZoneLockedEvent                                       ×1
    ///   SO_LockZone_AlreadyLocked_ResetsTimer_DoesNotRefire                      ×1
    ///   SO_Tick_NotLocked_NoOp                                                   ×1
    ///   SO_Tick_Expired_UnlocksZone                                              ×1
    ///   SO_Reset_ClearsLock                                                      ×1
    ///   Controller_FreshInstance_ZoneLockSO_Null                                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channel                                 ×1
    ///   Controller_HandleZoneCaptured_CallsLockZone                              ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlZoneLockTests
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

        private static ZoneControlZoneLockSO CreateZoneLockSO(float lockDuration = 3f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneLockSO>();
            SetField(so, "_lockDuration", lockDuration);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneLockController CreateController() =>
            new GameObject("ZoneLockCtrl_Test")
                .AddComponent<ZoneControlZoneLockController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsLocked_False()
        {
            var so = CreateZoneLockSO();
            Assert.IsFalse(so.IsLocked,
                "IsLocked must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LockZone_SetsIsLocked()
        {
            var so = CreateZoneLockSO(lockDuration: 5f);
            so.LockZone();
            Assert.IsTrue(so.IsLocked,
                "IsLocked must be true after LockZone is called.");
            Assert.AreEqual(5f, so.LockTimer, 0.001f,
                "LockTimer must equal LockDuration immediately after LockZone.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LockZone_FiresOnZoneLockedEvent()
        {
            var so      = CreateZoneLockSO();
            var onLocked = CreateEvent();
            SetField(so, "_onZoneLocked", onLocked);

            int fired = 0;
            onLocked.RegisterCallback(() => fired++);

            so.LockZone();
            Assert.AreEqual(1, fired,
                "_onZoneLocked must fire once when the zone transitions from unlocked to locked.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onLocked);
        }

        [Test]
        public void SO_LockZone_AlreadyLocked_ResetsTimer_DoesNotRefire()
        {
            var so      = CreateZoneLockSO(lockDuration: 3f);
            var onLocked = CreateEvent();
            SetField(so, "_onZoneLocked", onLocked);

            int fired = 0;
            onLocked.RegisterCallback(() => fired++);

            so.LockZone();           // first lock → event fires
            so.Tick(1f);             // consume 1 second of lock
            float timerAfterTick = so.LockTimer;

            so.LockZone();           // re-lock while already locked → timer resets, no re-fire
            Assert.AreEqual(1, fired,
                "_onZoneLocked must not re-fire when zone is already locked.");
            Assert.Greater(so.LockTimer, timerAfterTick,
                "LockTimer must be reset to LockDuration when LockZone is called while already locked.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onLocked);
        }

        [Test]
        public void SO_Tick_NotLocked_NoOp()
        {
            var so         = CreateZoneLockSO();
            var onUnlocked = CreateEvent();
            SetField(so, "_onZoneUnlocked", onUnlocked);

            int fired = 0;
            onUnlocked.RegisterCallback(() => fired++);

            so.Tick(10f); // zone is not locked; should be a no-op
            Assert.IsFalse(so.IsLocked,
                "IsLocked must remain false when Tick is called on an unlocked zone.");
            Assert.AreEqual(0, fired,
                "_onZoneUnlocked must not fire when zone was not locked.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onUnlocked);
        }

        [Test]
        public void SO_Tick_Expired_UnlocksZone()
        {
            var so         = CreateZoneLockSO(lockDuration: 2f);
            var onUnlocked = CreateEvent();
            SetField(so, "_onZoneUnlocked", onUnlocked);

            int fired = 0;
            onUnlocked.RegisterCallback(() => fired++);

            so.LockZone();
            so.Tick(3f); // exceeds lock duration
            Assert.IsFalse(so.IsLocked,
                "IsLocked must be false after the lock duration has expired.");
            Assert.AreEqual(1, fired,
                "_onZoneUnlocked must fire once when the lock expires.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onUnlocked);
        }

        [Test]
        public void SO_Reset_ClearsLock()
        {
            var so = CreateZoneLockSO();
            so.LockZone();
            Assert.IsTrue(so.IsLocked);

            so.Reset();
            Assert.IsFalse(so.IsLocked,
                "IsLocked must be false after Reset.");
            Assert.AreEqual(0f, so.LockTimer, 0.001f,
                "LockTimer must be 0 after Reset.");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ZoneLockSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ZoneLockSO,
                "ZoneLockSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlZoneLockController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlZoneLockController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlZoneLockController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onZoneCaptured", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onZoneCaptured must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleZoneCaptured_CallsLockZone()
        {
            var go   = new GameObject("Test_LockZone");
            var ctrl = go.AddComponent<ZoneControlZoneLockController>();
            var so   = CreateZoneLockSO(lockDuration: 3f);
            SetField(ctrl, "_zoneLockSO", so);

            ctrl.HandleZoneCaptured();

            Assert.IsTrue(so.IsLocked,
                "ZoneLockSO.IsLocked must be true after HandleZoneCaptured is called.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}

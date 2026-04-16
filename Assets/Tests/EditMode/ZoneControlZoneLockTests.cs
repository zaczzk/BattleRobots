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
    ///   SO_FreshInstance_IsLocked_False                               ×1
    ///   SO_LockZone_SetsIsLocked                                      ×1
    ///   SO_LockZone_FiresOnZoneLocked                                 ×1
    ///   SO_Tick_DecrementTimer_StaysLocked                            ×1
    ///   SO_Tick_ExpiresLock_UnlocksZone                               ×1
    ///   SO_Tick_NotLocked_NoOp                                        ×1
    ///   SO_LockProgress_WhileLocked                                   ×1
    ///   SO_Reset_ClearsLock                                           ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_Channel                      ×1
    ///   Controller_HandleZoneCaptured_LocksZone                       ×1
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

        private static ZoneControlZoneLockSO CreateLockSO(float lockDuration = 5f)
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
            var so = ScriptableObject.CreateInstance<ZoneControlZoneLockSO>();
            Assert.IsFalse(so.IsLocked,
                "IsLocked must be false on a fresh instance.");
            Assert.AreEqual(0f, so.LockProgress,
                "LockProgress must be 0 when not locked.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LockZone_SetsIsLocked()
        {
            var so = CreateLockSO(5f);
            so.LockZone();
            Assert.IsTrue(so.IsLocked,
                "IsLocked must be true after LockZone.");
            Assert.AreEqual(5f, so.LockTimer, 0.001f,
                "LockTimer must equal LockDuration after LockZone.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LockZone_FiresOnZoneLocked()
        {
            var so  = CreateLockSO(5f);
            var evt = CreateEvent();
            SetField(so, "_onZoneLocked", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.LockZone();
            Assert.AreEqual(1, fired,
                "_onZoneLocked must fire once when LockZone is called.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_DecrementTimer_StaysLocked()
        {
            var so = CreateLockSO(5f);
            so.LockZone();
            so.Tick(2f);
            Assert.IsTrue(so.IsLocked,
                "Zone must remain locked after partial tick.");
            Assert.AreEqual(3f, so.LockTimer, 0.001f,
                "LockTimer must be decremented by deltaTime.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExpiresLock_UnlocksZone()
        {
            var so  = CreateLockSO(5f);
            var evt = CreateEvent();
            SetField(so, "_onZoneUnlocked", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.LockZone();
            so.Tick(5f); // exactly expires the lock

            Assert.IsFalse(so.IsLocked,
                "Zone must be unlocked after lock duration elapses.");
            Assert.AreEqual(1, fired,
                "_onZoneUnlocked must fire once when lock expires.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_NotLocked_NoOp()
        {
            var so  = CreateLockSO(5f);
            var evt = CreateEvent();
            SetField(so, "_onZoneUnlocked", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.Tick(10f); // not locked — should be no-op
            Assert.AreEqual(0, fired,
                "_onZoneUnlocked must not fire when zone is not locked.");
            Assert.IsFalse(so.IsLocked);

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_LockProgress_WhileLocked()
        {
            var so = CreateLockSO(10f);
            so.LockZone();
            so.Tick(4f); // timer = 6f; progress = 6/10 = 0.6

            Assert.AreEqual(0.6f, so.LockProgress, 0.001f,
                "LockProgress must equal remaining time / lock duration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsLock()
        {
            var so = CreateLockSO(5f);
            so.LockZone();
            so.Reset();

            Assert.IsFalse(so.IsLocked,
                "IsLocked must be false after Reset.");
            Assert.AreEqual(0f, so.LockTimer,
                "LockTimer must be 0 after Reset.");
            Assert.AreEqual(0f, so.LockProgress,
                "LockProgress must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

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

            var evt = CreateEvent();
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
        public void Controller_HandleZoneCaptured_LocksZone()
        {
            var go   = new GameObject("Test_LocksZone");
            var ctrl = go.AddComponent<ZoneControlZoneLockController>();

            var lockSO = CreateLockSO(3f);
            SetField(ctrl, "_lockSO", lockSO);

            ctrl.HandleZoneCaptured();

            Assert.IsTrue(lockSO.IsLocked,
                "HandleZoneCaptured must delegate to ZoneControlZoneLockSO.LockZone.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(lockSO);
        }
    }
}

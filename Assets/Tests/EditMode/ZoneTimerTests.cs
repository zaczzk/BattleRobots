using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T262: <see cref="ZoneTimerSO"/> and
    /// <see cref="ZoneTimerController"/>.
    ///
    /// ZoneTimerTests (12):
    ///   SO_FreshInstance_CooldownDuration_Default_Five                    ×1
    ///   SO_FreshInstance_IsOnCooldown_False                               ×1
    ///   SO_StartCooldown_SetsIsOnCooldown                                 ×1
    ///   SO_StartCooldown_SetsRemainingCooldown                            ×1
    ///   SO_Tick_DecrementsRemaining                                        ×1
    ///   SO_Tick_ExpiresIsOnCooldown                                        ×1
    ///   SO_Reset_ClearsCooldownState                                       ×1
    ///   Controller_FreshInstance_TimerSO_Null                             ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_Unregisters_ZoneLostChannel                  ×1
    ///   Controller_HandleZoneLost_StartsTimerCooldown                     ×1
    ///   Controller_HandleMatchStarted_ResetsTimer                         ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneTimerTests
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

        private static ZoneTimerController CreateController() =>
            new GameObject("ZoneTimerCtrl_Test").AddComponent<ZoneTimerController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CooldownDuration_Default_Five()
        {
            var so = CreateTimerSO();
            Assert.AreEqual(5f, so.CooldownDuration, 0.001f,
                "CooldownDuration must default to 5 seconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsOnCooldown_False()
        {
            var so = CreateTimerSO();
            Assert.IsFalse(so.IsOnCooldown,
                "IsOnCooldown must be false on a fresh ZoneTimerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCooldown_SetsIsOnCooldown()
        {
            var so = CreateTimerSO();
            so.StartCooldown();
            Assert.IsTrue(so.IsOnCooldown,
                "StartCooldown must set IsOnCooldown to true.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCooldown_SetsRemainingCooldown()
        {
            var so = CreateTimerSO();
            SetField(so, "_cooldownDuration", 3f);
            so.StartCooldown();
            Assert.AreEqual(3f, so.RemainingCooldown, 0.001f,
                "StartCooldown must set RemainingCooldown to CooldownDuration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DecrementsRemaining()
        {
            var so = CreateTimerSO();
            SetField(so, "_cooldownDuration", 4f);
            so.StartCooldown();

            so.Tick(1f);

            Assert.AreEqual(3f, so.RemainingCooldown, 0.001f,
                "Tick must decrement RemainingCooldown by dt.");
            Assert.IsTrue(so.IsOnCooldown,
                "IsOnCooldown must remain true before the cooldown expires.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExpiresIsOnCooldown()
        {
            var so    = CreateTimerSO();
            var evt   = CreateEvent();
            int fired = 0;
            SetField(so, "_cooldownDuration", 2f);
            SetField(so, "_onCooldownEnded", evt);
            evt.RegisterCallback(() => fired++);

            so.StartCooldown();
            so.Tick(3f);   // past the duration

            Assert.IsFalse(so.IsOnCooldown,
                "Tick must clear IsOnCooldown when remaining time reaches 0.");
            Assert.AreEqual(0f, so.RemainingCooldown, 0.001f,
                "RemainingCooldown must clamp to 0 after expiry.");
            Assert.AreEqual(1, fired,
                "_onCooldownEnded must fire once when the cooldown expires.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsCooldownState()
        {
            var so = CreateTimerSO();
            SetField(so, "_cooldownDuration", 5f);
            so.StartCooldown();
            Assert.IsTrue(so.IsOnCooldown, "Pre-condition: cooldown must be active.");

            so.Reset();

            Assert.IsFalse(so.IsOnCooldown,
                "Reset must clear IsOnCooldown.");
            Assert.AreEqual(0f, so.RemainingCooldown, 0.001f,
                "Reset must zero RemainingCooldown.");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TimerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TimerSO,
                "TimerSO must be null on a fresh ZoneTimerController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_ZoneLostChannel()
        {
            var ctrl   = CreateController();
            var timer  = CreateTimerSO();
            var lost   = CreateEvent();
            SetField(ctrl, "_timerSO",    timer);
            SetField(ctrl, "_onZoneLost", lost);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After disable, _onZoneLost must not start the cooldown.
            lost.Raise();
            Assert.IsFalse(timer.IsOnCooldown,
                "After OnDisable, _onZoneLost must not start the cooldown.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timer);
            Object.DestroyImmediate(lost);
        }

        [Test]
        public void Controller_HandleZoneLost_StartsTimerCooldown()
        {
            var ctrl  = CreateController();
            var timer = CreateTimerSO();
            SetField(ctrl, "_timerSO", timer);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleZoneLost();

            Assert.IsTrue(timer.IsOnCooldown,
                "HandleZoneLost must call ZoneTimerSO.StartCooldown().");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timer);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsTimer()
        {
            var ctrl  = CreateController();
            var timer = CreateTimerSO();
            SetField(ctrl, "_timerSO", timer);
            InvokePrivate(ctrl, "Awake");

            timer.StartCooldown();
            Assert.IsTrue(timer.IsOnCooldown, "Pre-condition: cooldown must be active.");

            ctrl.HandleMatchStarted();

            Assert.IsFalse(timer.IsOnCooldown,
                "HandleMatchStarted must reset ZoneTimerSO and clear the cooldown.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timer);
        }
    }
}

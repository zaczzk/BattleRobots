using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T248: <see cref="MatchClockSO"/> and
    /// <see cref="MatchClockHUDController"/>.
    ///
    /// MatchClockTests (16):
    ///   SO_FreshInstance_Duration_DefaultValue                        ×1
    ///   SO_FreshInstance_IsRunning_False                              ×1
    ///   SO_StartClock_SetsIsRunning_True                              ×1
    ///   SO_StopClock_SetsIsRunning_False                              ×1
    ///   SO_Tick_BelowDuration_TimeRemainingDecreases                  ×1
    ///   SO_Tick_ExceedsDuration_FiresOnTimeExpired                    ×1
    ///   SO_Tick_CrossesWarningThreshold_FiresOnTimeWarning            ×1
    ///   SO_Tick_WarningFiresOnlyOnce                                  ×1
    ///   Controller_FreshInstance_ClockSO_Null                         ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_Unregisters_BothChannels                 ×1
    ///   Controller_HandleMatchStarted_StartsClockSO                   ×1
    ///   Controller_HandleMatchEnded_StopsClockSO                      ×1
    ///   Controller_Tick_NullSO_DoesNotThrow                           ×1
    ///   Controller_Refresh_NullLabel_DoesNotThrow                     ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchClockTests
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

        private static MatchClockSO CreateClockSO() =>
            ScriptableObject.CreateInstance<MatchClockSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchClockHUDController CreateController() =>
            new GameObject("MatchClockHUD_Test").AddComponent<MatchClockHUDController>();

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Duration_DefaultValue()
        {
            var so = CreateClockSO();
            // Default _duration is 180f.
            Assert.AreEqual(180f, so.Duration, 0.001f,
                "Duration must default to 180 seconds on a fresh MatchClockSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsRunning_False()
        {
            var so = CreateClockSO();
            Assert.IsFalse(so.IsRunning,
                "IsRunning must be false on a fresh MatchClockSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartClock_SetsIsRunning_True()
        {
            var so = CreateClockSO();
            so.StartClock();
            Assert.IsTrue(so.IsRunning,
                "IsRunning must be true after StartClock().");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StopClock_SetsIsRunning_False()
        {
            var so = CreateClockSO();
            so.StartClock();
            so.StopClock();
            Assert.IsFalse(so.IsRunning,
                "IsRunning must be false after StopClock().");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowDuration_TimeRemainingDecreases()
        {
            var so = CreateClockSO();
            SetField(so, "_duration", 10f);
            SetField(so, "_warningThreshold", 0f);
            so.StartClock();

            so.Tick(3f);

            Assert.AreEqual(7f, so.TimeRemaining, 0.001f,
                "TimeRemaining must be Duration - elapsed after a partial Tick.");
            Assert.IsTrue(so.IsRunning,
                "Clock must still be running after a sub-duration Tick.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExceedsDuration_FiresOnTimeExpired()
        {
            var so       = CreateClockSO();
            var expEvt   = CreateEvent();
            SetField(so, "_duration", 5f);
            SetField(so, "_warningThreshold", 0f);
            SetField(so, "_onTimeExpired", expEvt);
            so.StartClock();

            int count = 0;
            expEvt.RegisterCallback(() => count++);

            so.Tick(6f);   // exceeds duration

            Assert.AreEqual(1, count,
                "_onTimeExpired must fire exactly once when the clock reaches zero.");
            Assert.IsFalse(so.IsRunning,
                "IsRunning must be false after expiry.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(expEvt);
        }

        [Test]
        public void SO_Tick_CrossesWarningThreshold_FiresOnTimeWarning()
        {
            var so        = CreateClockSO();
            var warnEvt   = CreateEvent();
            SetField(so, "_duration", 10f);
            SetField(so, "_warningThreshold", 3f);
            SetField(so, "_onTimeWarning", warnEvt);
            so.StartClock();

            int count = 0;
            warnEvt.RegisterCallback(() => count++);

            so.Tick(8f);   // 2s remaining → crosses 3s threshold

            Assert.AreEqual(1, count,
                "_onTimeWarning must fire once when TimeRemaining drops to or below the threshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(warnEvt);
        }

        [Test]
        public void SO_Tick_WarningFiresOnlyOnce()
        {
            var so      = CreateClockSO();
            var warnEvt = CreateEvent();
            SetField(so, "_duration", 10f);
            SetField(so, "_warningThreshold", 5f);
            SetField(so, "_onTimeWarning", warnEvt);
            so.StartClock();

            int count = 0;
            warnEvt.RegisterCallback(() => count++);

            so.Tick(6f);   // crosses threshold
            so.Tick(1f);   // still within threshold — must NOT fire again

            Assert.AreEqual(1, count,
                "_onTimeWarning must fire at most once per clock run.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(warnEvt);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ClockSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ClockSO,
                "ClockSO must be null on a fresh MatchClockHUDController.");
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_BothChannels()
        {
            var ctrl     = CreateController();
            var startEvt = CreateEvent();
            var endEvt   = CreateEvent();

            SetField(ctrl, "_onMatchStarted", startEvt);
            SetField(ctrl, "_onMatchEnded",   endEvt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int startCount = 0, endCount = 0;
            startEvt.RegisterCallback(() => startCount++);
            endEvt.RegisterCallback(() => endCount++);

            startEvt.Raise();
            endEvt.Raise();

            Assert.AreEqual(1, startCount, "Only external callbacks fire after OnDisable on _onMatchStarted.");
            Assert.AreEqual(1, endCount,   "Only external callbacks fire after OnDisable on _onMatchEnded.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(startEvt);
            Object.DestroyImmediate(endEvt);
        }

        [Test]
        public void Controller_HandleMatchStarted_StartsClockSO()
        {
            var ctrl    = CreateController();
            var clockSO = CreateClockSO();
            SetField(ctrl, "_clockSO", clockSO);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(clockSO.IsRunning,
                "HandleMatchStarted must call MatchClockSO.StartClock().");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(clockSO);
        }

        [Test]
        public void Controller_HandleMatchEnded_StopsClockSO()
        {
            var ctrl    = CreateController();
            var clockSO = CreateClockSO();
            SetField(ctrl, "_clockSO", clockSO);
            InvokePrivate(ctrl, "Awake");

            clockSO.StartClock();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(clockSO.IsRunning,
                "HandleMatchEnded must call MatchClockSO.StopClock().");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(clockSO);
        }

        [Test]
        public void Controller_Tick_NullSO_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.Tick(1f),
                "Tick with null ClockSO must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullLabel_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var clockSO = CreateClockSO();
            SetField(ctrl, "_clockSO", clockSO);
            InvokePrivate(ctrl, "Awake");

            // _timerLabel and _warningPanel are null (not assigned in test).
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null UI refs must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(clockSO);
        }
    }
}

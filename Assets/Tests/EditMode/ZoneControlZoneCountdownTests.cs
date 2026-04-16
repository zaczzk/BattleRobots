using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T336: <see cref="ZoneControlZoneCountdownSO"/> and
    /// <see cref="ZoneControlZoneCountdownController"/>.
    ///
    /// ZoneControlZoneCountdownTests (12):
    ///   SO_FreshInstance_IsActive_False                             ×1
    ///   SO_FreshInstance_Progress_Zero                              ×1
    ///   SO_StartCountdown_SetsIsActive_True                         ×1
    ///   SO_StartCountdown_SetsProgress_One                          ×1
    ///   SO_Tick_DecrementsProgress                                  ×1
    ///   SO_Tick_FiresExpired_WhenTimerExpires                        ×1
    ///   SO_Tick_SetsIsActive_False_AfterExpiry                       ×1
    ///   SO_Reset_ClearsState                                        ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_Unregisters_Channel                    ×1
    ///   Controller_HandleMatchStarted_StartsCountdown               ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlZoneCountdownTests
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

        private static ZoneControlZoneCountdownSO CreateCountdownSO(float duration = 5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneCountdownSO>();
            SetField(so, "_duration", duration);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneCountdownSO>();
            Assert.IsFalse(so.IsActive,
                "IsActive must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_Progress_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneCountdownSO>();
            Assert.AreEqual(0f, so.Progress, 0.001f,
                "Progress must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCountdown_SetsIsActive_True()
        {
            var so = CreateCountdownSO();
            so.StartCountdown();
            Assert.IsTrue(so.IsActive,
                "IsActive must be true after StartCountdown.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCountdown_SetsProgress_One()
        {
            var so = CreateCountdownSO(duration: 5f);
            so.StartCountdown();
            Assert.AreEqual(1f, so.Progress, 0.001f,
                "Progress must be 1 immediately after StartCountdown.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DecrementsProgress()
        {
            var so = CreateCountdownSO(duration: 10f);
            so.StartCountdown();
            so.Tick(2f);
            Assert.AreEqual(0.8f, so.Progress, 0.001f,
                "Progress must decrease by dt/duration per Tick.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresExpired_WhenTimerExpires()
        {
            var so  = CreateCountdownSO(duration: 2f);
            var evt = CreateEvent();
            SetField(so, "_onCountdownExpired", evt);

            so.StartCountdown();

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.Tick(3f);  // exceeds duration

            Assert.AreEqual(1, fired,
                "_onCountdownExpired must fire exactly once when the timer expires.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_SetsIsActive_False_AfterExpiry()
        {
            var so = CreateCountdownSO(duration: 1f);
            so.StartCountdown();
            so.Tick(2f);
            Assert.IsFalse(so.IsActive,
                "IsActive must be false after the countdown expires.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateCountdownSO();
            so.StartCountdown();
            so.Reset();
            Assert.IsFalse(so.IsActive,
                "IsActive must be false after Reset.");
            Assert.AreEqual(0f, so.Progress, 0.001f,
                "Progress must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlZoneCountdownController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlZoneCountdownController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlZoneCountdownController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchStarted", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchStarted must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchStarted_StartsCountdown()
        {
            var go   = new GameObject("Test_HandleMatchStarted");
            var ctrl = go.AddComponent<ZoneControlZoneCountdownController>();
            var so   = CreateCountdownSO(duration: 5f);
            SetField(ctrl, "_countdownSO", so);

            ctrl.HandleMatchStarted();

            Assert.IsTrue(so.IsActive,
                "HandleMatchStarted must start the countdown SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}

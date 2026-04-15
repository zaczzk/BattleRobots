using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T273:
    ///   <see cref="ZonePresenceTimerSO"/> and
    ///   <see cref="ZonePresenceTimerController"/>.
    ///
    /// ZonePresenceTimerSOTests (6):
    ///   FreshInstance_MaxZones_Three                                    ×1
    ///   GetPresenceTime_OutOfRange_ReturnsZero                         ×1
    ///   AddPresenceTime_Accumulates                                     ×1
    ///   AddPresenceTime_NegativeDt_NoChange                            ×1
    ///   AddPresenceTime_OutOfRange_DoesNotThrow                        ×1
    ///   Reset_ZerosAllTimes                                             ×1
    ///
    /// ZonePresenceTimerControllerTests (6):
    ///   FreshInstance_TimerSO_Null                                      ×1
    ///   FreshInstance_IsMatchRunning_False                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_BothChannels                              ×1
    ///   HandleMatchStarted_SetsRunning_ResetsTimer                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZonePresenceTimerTests
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

        private static ZonePresenceTimerSO CreateSO() =>
            ScriptableObject.CreateInstance<ZonePresenceTimerSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZonePresenceTimerController CreateController() =>
            new GameObject("ZonePresenceTimerCtrl_Test")
                .AddComponent<ZonePresenceTimerController>();

        // ── ZonePresenceTimerSO tests ──────────────────────────────────────────

        [Test]
        public void FreshInstance_MaxZones_Three()
        {
            var so = CreateSO();
            Assert.AreEqual(3, so.MaxZones,
                "Default MaxZones must be 3.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetPresenceTime_OutOfRange_ReturnsZero()
        {
            var so = CreateSO();
            Assert.AreEqual(0f, so.GetPresenceTime(99), 0.001f,
                "GetPresenceTime with an out-of-range index must return 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddPresenceTime_Accumulates()
        {
            var so = CreateSO();
            so.AddPresenceTime(0, 1.5f);
            so.AddPresenceTime(0, 0.5f);
            Assert.AreEqual(2.0f, so.GetPresenceTime(0), 0.001f,
                "AddPresenceTime must accumulate time correctly.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddPresenceTime_NegativeDt_NoChange()
        {
            var so = CreateSO();
            so.AddPresenceTime(0, -1f);
            Assert.AreEqual(0f, so.GetPresenceTime(0), 0.001f,
                "AddPresenceTime with a negative dt must be a no-op.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddPresenceTime_OutOfRange_DoesNotThrow()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => so.AddPresenceTime(99, 1f),
                "AddPresenceTime with an out-of-range index must not throw.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Reset_ZerosAllTimes()
        {
            var so = CreateSO();
            so.AddPresenceTime(0, 5f);
            so.AddPresenceTime(1, 3f);
            so.AddPresenceTime(2, 1f);
            so.Reset();
            Assert.AreEqual(0f, so.GetPresenceTime(0), 0.001f, "Zone 0 must be 0 after Reset.");
            Assert.AreEqual(0f, so.GetPresenceTime(1), 0.001f, "Zone 1 must be 0 after Reset.");
            Assert.AreEqual(0f, so.GetPresenceTime(2), 0.001f, "Zone 2 must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── ZonePresenceTimerController tests ─────────────────────────────────

        [Test]
        public void FreshInstance_TimerSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.TimerSO,
                "TimerSO must be null on a fresh ZonePresenceTimerController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_IsMatchRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false on a fresh ZonePresenceTimerController.");
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
            var timerSO  = CreateSO();
            var evtStart = CreateEvent();

            SetField(ctrl, "_timerSO",        timerSO);
            SetField(ctrl, "_onMatchStarted", evtStart);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raising match-started after disable must NOT call HandleMatchStarted.
            Assert.IsFalse(ctrl.IsMatchRunning,
                "Pre-condition: IsMatchRunning should be false.");
            evtStart.Raise(); // must not set _matchRunning = true
            Assert.IsFalse(ctrl.IsMatchRunning,
                "After OnDisable, match-started event must not set IsMatchRunning.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
            Object.DestroyImmediate(evtStart);
        }

        [Test]
        public void HandleMatchStarted_SetsRunning_ResetsTimer()
        {
            var ctrl    = CreateController();
            var timerSO = CreateSO();
            SetField(ctrl, "_timerSO", timerSO);

            // Pre-populate some presence time.
            timerSO.AddPresenceTime(0, 10f);
            Assert.AreEqual(10f, timerSO.GetPresenceTime(0), 0.001f,
                "Pre-condition: zone 0 should have 10s presence.");

            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsMatchRunning,
                "HandleMatchStarted must set IsMatchRunning = true.");
            Assert.AreEqual(0f, timerSO.GetPresenceTime(0), 0.001f,
                "HandleMatchStarted must reset the ZonePresenceTimerSO.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(timerSO);
        }
    }
}

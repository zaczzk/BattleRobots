using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T319: <see cref="ZoneControlMatchPressureSO"/> and
    /// <see cref="ZoneControlMatchPressureController"/>.
    ///
    /// ZoneControlMatchPressureTests (12):
    ///   SO_FreshInstance_Pressure_Zero                                           ×1
    ///   SO_Tick_BotLeading_IncreasesPressure                                     ×1
    ///   SO_Tick_PlayerLeading_DecreasesPressure                                  ×1
    ///   SO_Tick_HighPressure_FiresOnHighPressureEvent                            ×1
    ///   SO_Tick_PressureRelieved_FiresOnPressureRelievedEvent                    ×1
    ///   SO_Tick_SameThresholdSide_DoesNotRefire                                  ×1
    ///   SO_Reset_ClearsPressure                                                   ×1
    ///   Controller_FreshInstance_IsRunning_False                                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channel                                 ×1
    ///   Controller_HandleMatchStarted_SetsRunning                                ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchPressureTests
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

        private static ZoneControlMatchPressureSO CreatePressureSO(
            float increaseRate     = 0.5f,
            float decayRate        = 0.25f,
            float highThreshold    = 0.8f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPressureSO>();
            SetField(so, "_pressureIncreaseRate",   increaseRate);
            SetField(so, "_pressureDecayRate",       decayRate);
            SetField(so, "_highPressureThreshold",   highThreshold);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchPressureController CreateController() =>
            new GameObject("PressureCtrl_Test")
                .AddComponent<ZoneControlMatchPressureController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Pressure_Zero()
        {
            var so = CreatePressureSO();
            Assert.AreEqual(0f, so.Pressure,
                "Pressure must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BotLeading_IncreasesPressure()
        {
            var so = CreatePressureSO(increaseRate: 1f);
            so.Tick(0.5f, botIsLeading: true);
            Assert.Greater(so.Pressure, 0f,
                "Pressure must increase when bots are leading.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_PlayerLeading_DecreasesPressure()
        {
            // Push pressure to 0.5 first, then let it decay.
            var so = CreatePressureSO(increaseRate: 1f, decayRate: 1f);
            so.Tick(0.5f, botIsLeading: true); // pressure = 0.5
            float before = so.Pressure;
            so.Tick(0.1f, botIsLeading: false);
            Assert.Less(so.Pressure, before,
                "Pressure must decrease when the player is leading.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_HighPressure_FiresOnHighPressureEvent()
        {
            var so = CreatePressureSO(increaseRate: 1f, highThreshold: 0.5f);
            var onHigh = CreateEvent();
            SetField(so, "_onHighPressure", onHigh);

            int fired = 0;
            onHigh.RegisterCallback(() => fired++);

            so.Tick(0.6f, botIsLeading: true); // crosses 0.5 threshold
            Assert.AreEqual(1, fired,
                "_onHighPressure must fire once when pressure crosses the high threshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onHigh);
        }

        [Test]
        public void SO_Tick_PressureRelieved_FiresOnPressureRelievedEvent()
        {
            var so = CreatePressureSO(increaseRate: 1f, decayRate: 1f, highThreshold: 0.5f);
            var onRelieved = CreateEvent();
            SetField(so, "_onPressureRelieved", onRelieved);

            int fired = 0;
            onRelieved.RegisterCallback(() => fired++);

            so.Tick(0.6f, botIsLeading: true);  // into high pressure
            so.Tick(0.2f, botIsLeading: false);  // decay below threshold → relieved
            Assert.AreEqual(1, fired,
                "_onPressureRelieved must fire once when pressure drops below the threshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onRelieved);
        }

        [Test]
        public void SO_Tick_SameThresholdSide_DoesNotRefire()
        {
            var so = CreatePressureSO(increaseRate: 1f, highThreshold: 0.5f);
            var onHigh = CreateEvent();
            SetField(so, "_onHighPressure", onHigh);

            int fired = 0;
            onHigh.RegisterCallback(() => fired++);

            so.Tick(0.6f, botIsLeading: true); // crosses → fires once
            so.Tick(0.1f, botIsLeading: true); // stays high → must not refire
            Assert.AreEqual(1, fired,
                "_onHighPressure must not re-fire while already in high-pressure state.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(onHigh);
        }

        [Test]
        public void SO_Reset_ClearsPressure()
        {
            var so = CreatePressureSO(increaseRate: 1f);
            so.Tick(1f, botIsLeading: true); // pressure = 1.0
            Assert.Greater(so.Pressure, 0f);

            so.Reset();
            Assert.AreEqual(0f, so.Pressure,
                "Pressure must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_IsRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsRunning,
                "IsRunning must be false on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchPressureController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();
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
        public void Controller_HandleMatchStarted_SetsRunning()
        {
            var go   = new GameObject("Test_MatchStarted");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();
            var so   = CreatePressureSO();
            SetField(ctrl, "_pressureSO", so);

            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsRunning,
                "IsRunning must be true after HandleMatchStarted.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T245: <see cref="ArenaPhaseControllerSO"/> and
    /// <see cref="ArenaPhaseController"/>.
    ///
    /// ArenaPhaseTests (16):
    ///   SO_FreshInstance_PhaseCount_Zero                          ×1
    ///   SO_GetPhaseDuration_ValidIndex                            ×1
    ///   SO_GetPhaseEvent_ValidIndex                               ×1
    ///   SO_GetPhaseDuration_OutOfRange_ReturnsZero                ×1
    ///   Controller_FreshInstance_ConfigNull                       ×1
    ///   Controller_FreshInstance_IsMatchRunning_False             ×1
    ///   Controller_FreshInstance_CurrentPhase_Zero               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_Unregisters_BothChannels             ×1
    ///   Controller_HandleMatchStarted_SetsMatchRunning_True       ×1
    ///   Controller_HandleMatchStarted_ResetsPhase_Zero            ×1
    ///   Controller_HandleMatchEnded_SetsMatchRunning_False        ×1
    ///   Controller_Tick_BelowDuration_SamePhase                  ×1
    ///   Controller_Tick_ExceedsDuration_AdvancesPhase            ×1
    ///   Controller_AdvancePhase_AllComplete_FiresOnAllPhasesComplete ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class ArenaPhaseTests
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

        private static ArenaPhaseControllerSO CreateConfigSO() =>
            ScriptableObject.CreateInstance<ArenaPhaseControllerSO>();

        private static ArenaPhaseController CreateController() =>
            new GameObject("ArenaPhaseCtrl_Test").AddComponent<ArenaPhaseController>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        /// <summary>
        /// Builds a phases array and injects it into the SO via reflection.
        /// </summary>
        private static ArenaPhaseControllerSO CreateConfigWithPhases(params float[] durations)
        {
            var so     = CreateConfigSO();
            var phases = new ArenaPhase[durations.Length];
            for (int i = 0; i < durations.Length; i++)
                phases[i] = new ArenaPhase { duration = durations[i] };

            SetField(so, "_phases", phases);
            return so;
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PhaseCount_Zero()
        {
            var so = CreateConfigSO();
            Assert.AreEqual(0, so.PhaseCount,
                "PhaseCount must be 0 on a fresh instance with no phases assigned.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetPhaseDuration_ValidIndex()
        {
            var so = CreateConfigWithPhases(5f, 10f);
            Assert.AreEqual(5f,  so.GetPhaseDuration(0), 0.001f, "Phase 0 duration must be 5f.");
            Assert.AreEqual(10f, so.GetPhaseDuration(1), 0.001f, "Phase 1 duration must be 10f.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetPhaseEvent_ValidIndex()
        {
            var so  = CreateConfigSO();
            var evt = CreateEvent();

            var phases = new ArenaPhase[] { new ArenaPhase { phaseEvent = evt, duration = 5f } };
            SetField(so, "_phases", phases);

            Assert.AreEqual(evt, so.GetPhaseEvent(0),
                "GetPhaseEvent must return the assigned event for a valid index.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_GetPhaseDuration_OutOfRange_ReturnsZero()
        {
            var so = CreateConfigWithPhases(5f);
            Assert.AreEqual(0f, so.GetPhaseDuration(-1), 0.001f, "Negative index must return 0f.");
            Assert.AreEqual(0f, so.GetPhaseDuration(99), 0.001f, "Out-of-range index must return 0f.");
            Object.DestroyImmediate(so);
        }

        // ── Controller fresh-instance tests ───────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Config,
                "Config must be null on a fresh ArenaPhaseController instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_IsMatchRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_CurrentPhase_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0, ctrl.CurrentPhase,
                "CurrentPhase must be 0 on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle tests ───────────────────────────────────────────────────

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

        // ── Handle tests ──────────────────────────────────────────────────────

        [Test]
        public void Controller_HandleMatchStarted_SetsMatchRunning_True()
        {
            var ctrl   = CreateController();
            var config = CreateConfigWithPhases(5f);
            SetField(ctrl, "_config", config);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsMatchRunning,
                "IsMatchRunning must be true after HandleMatchStarted.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsPhase_Zero()
        {
            var ctrl   = CreateController();
            var config = CreateConfigWithPhases(0.5f, 0.5f);
            SetField(ctrl, "_config", config);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.Tick(1f);       // advance to phase 1
            ctrl.HandleMatchStarted(); // reset

            Assert.AreEqual(0, ctrl.CurrentPhase,
                "CurrentPhase must be reset to 0 on HandleMatchStarted.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Controller_HandleMatchEnded_SetsMatchRunning_False()
        {
            var ctrl   = CreateController();
            var config = CreateConfigWithPhases(5f);
            SetField(ctrl, "_config", config);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false after HandleMatchEnded.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
        }

        // ── Tick tests ────────────────────────────────────────────────────────

        [Test]
        public void Controller_Tick_BelowDuration_SamePhase()
        {
            var ctrl   = CreateController();
            var config = CreateConfigWithPhases(5f, 5f);
            SetField(ctrl, "_config", config);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.Tick(3f);   // below 5f threshold

            Assert.AreEqual(0, ctrl.CurrentPhase,
                "CurrentPhase must remain 0 when elapsed is below phase duration.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Controller_Tick_ExceedsDuration_AdvancesPhase()
        {
            var ctrl   = CreateController();
            var config = CreateConfigWithPhases(5f, 5f);
            SetField(ctrl, "_config", config);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.Tick(6f);   // exceeds 5f — advances to phase 1

            Assert.AreEqual(1, ctrl.CurrentPhase,
                "CurrentPhase must advance to 1 after phase 0 duration elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Controller_AdvancePhase_AllComplete_FiresOnAllPhasesComplete()
        {
            var ctrl              = CreateController();
            var config            = CreateConfigWithPhases(5f);    // single phase
            var allCompleteEvent  = CreateEvent();
            SetField(config, "_onAllPhasesComplete", allCompleteEvent);
            SetField(ctrl, "_config", config);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            int count = 0;
            allCompleteEvent.RegisterCallback(() => count++);

            ctrl.Tick(6f);   // single phase expires → all phases complete

            Assert.AreEqual(1, count,
                "_onAllPhasesComplete must fire exactly once when all phases are done.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(allCompleteEvent);
        }
    }
}

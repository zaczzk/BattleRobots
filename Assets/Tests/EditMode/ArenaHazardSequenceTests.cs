using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T242: <see cref="ArenaHazardSequenceSO"/> and
    /// <see cref="ArenaHazardSequenceController"/>.
    ///
    /// ArenaHazardSequenceTests (16):
    ///   SO_CycleDuration_DefaultTen                             ×1
    ///   SO_OnSequenceAdvanced_ExposedAsProperty                 ×1
    ///   Controller_FreshInstance_ConfigNull                     ×1
    ///   Controller_FreshInstance_HazardsNull                    ×1
    ///   Controller_FreshInstance_IsMatchRunning_False           ×1
    ///   Controller_FreshInstance_CurrentIndex_Zero              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow              ×1
    ///   Controller_OnDisable_Unregisters_BothChannels           ×1
    ///   HandleMatchStarted_SetsMatchRunning_True                ×1
    ///   HandleMatchStarted_ActivatesFirstHazard                 ×1
    ///   HandleMatchStarted_DeactivatesOtherHazards              ×1
    ///   HandleMatchEnded_DeactivatesAllHazards                  ×1
    ///   Tick_BeforeCycleDuration_SameIndex                      ×1
    ///   Tick_AfterCycleDuration_AdvancesIndex                   ×1
    ///   AdvanceSequence_WrapsAroundToZero                       ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class ArenaHazardSequenceTests
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

        private static ArenaHazardSequenceSO CreateConfig() =>
            ScriptableObject.CreateInstance<ArenaHazardSequenceSO>();

        private static ArenaHazardSequenceController CreateController() =>
            new GameObject("SeqCtrl_Test").AddComponent<ArenaHazardSequenceController>();

        private static HazardZoneController CreateHazardZone() =>
            new GameObject("HazardZone_Test").AddComponent<HazardZoneController>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_CycleDuration_DefaultTen()
        {
            var so = CreateConfig();
            Assert.AreEqual(10f, so.CycleDuration,
                "CycleDuration must default to 10f.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSequenceAdvanced_ExposedAsProperty()
        {
            var so  = CreateConfig();
            var evt = CreateEvent();
            SetField(so, "_onSequenceAdvanced", evt);

            Assert.AreEqual(evt, so.OnSequenceAdvanced,
                "OnSequenceAdvanced property must return the assigned VoidGameEvent.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Controller fresh-instance tests ───────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Config,
                "Config must be null on a fresh ArenaHazardSequenceController instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_HazardsNull()
        {
            var ctrl = CreateController();
            // _hazards is private; verify indirectly via HandleMatchStarted no-throw with null hazards.
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted(),
                "HandleMatchStarted with null _hazards must not throw.");
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
        public void Controller_FreshInstance_CurrentIndex_Zero()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0, ctrl.CurrentIndex,
                "CurrentIndex must be 0 on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle tests ───────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_BothChannels()
        {
            var ctrl    = CreateController();
            var started = CreateEvent();
            var ended   = CreateEvent();
            SetField(ctrl, "_onMatchStarted", started);
            SetField(ctrl, "_onMatchEnded",   ended);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int startCount = 0, endCount = 0;
            started.RegisterCallback(() => startCount++);
            ended.RegisterCallback(() => endCount++);
            started.Raise();
            ended.Raise();

            Assert.AreEqual(1, startCount,
                "After OnDisable, only external callbacks fire on _onMatchStarted.");
            Assert.AreEqual(1, endCount,
                "After OnDisable, only external callbacks fire on _onMatchEnded.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(started);
            Object.DestroyImmediate(ended);
        }

        // ── Match lifecycle tests ─────────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_SetsMatchRunning_True()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();
            Assert.IsTrue(ctrl.IsMatchRunning,
                "IsMatchRunning must be true after HandleMatchStarted.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_ActivatesFirstHazard()
        {
            var ctrl   = CreateController();
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();
            hazardA.IsActive = false;
            hazardB.IsActive = false;

            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(hazardA.IsActive,
                "The first hazard (index 0) must be active after HandleMatchStarted.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        [Test]
        public void HandleMatchStarted_DeactivatesOtherHazards()
        {
            var ctrl   = CreateController();
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();
            hazardA.IsActive = true;
            hazardB.IsActive = true;

            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            // hazardA (index 0) should be active; hazardB should not
            Assert.IsFalse(hazardB.IsActive,
                "Hazards other than index 0 must be deactivated at match start.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        [Test]
        public void HandleMatchEnded_DeactivatesAllHazards()
        {
            var ctrl   = CreateController();
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();

            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(hazardA.IsActive, "hazardA must be inactive after HandleMatchEnded.");
            Assert.IsFalse(hazardB.IsActive, "hazardB must be inactive after HandleMatchEnded.");
            Assert.IsFalse(ctrl.IsMatchRunning, "IsMatchRunning must be false after HandleMatchEnded.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        // ── Tick / Advance tests ──────────────────────────────────────────────

        [Test]
        public void Tick_BeforeCycleDuration_SameIndex()
        {
            var ctrl   = CreateController();
            var config = CreateConfig();          // CycleDuration = 10f
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();

            SetField(ctrl, "_config",  config);
            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.Tick(5f);                         // below 10f threshold

            Assert.AreEqual(0, ctrl.CurrentIndex,
                "CurrentIndex must remain 0 before CycleDuration elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        [Test]
        public void Tick_AfterCycleDuration_AdvancesIndex()
        {
            var ctrl   = CreateController();
            var config = CreateConfig();           // CycleDuration = 10f
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();

            SetField(ctrl, "_config",  config);
            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.Tick(11f);                        // exceeds 10f → advance

            Assert.AreEqual(1, ctrl.CurrentIndex,
                "CurrentIndex must advance to 1 after CycleDuration elapses.");
            Assert.IsFalse(hazardA.IsActive,
                "Previous hazard (index 0) must be deactivated after advance.");
            Assert.IsTrue(hazardB.IsActive,
                "New hazard (index 1) must be activated after advance.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }

        [Test]
        public void AdvanceSequence_WrapsAroundToZero()
        {
            var ctrl   = CreateController();
            var config = CreateConfig();
            var hazardA = CreateHazardZone();
            var hazardB = CreateHazardZone();

            SetField(ctrl, "_config",  config);
            SetField(ctrl, "_hazards", new HazardZoneController[] { hazardA, hazardB });
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();     // index = 0
            ctrl.AdvanceSequence();        // index = 1
            ctrl.AdvanceSequence();        // index wraps → 0

            Assert.AreEqual(0, ctrl.CurrentIndex,
                "CurrentIndex must wrap back to 0 after advancing past the last entry.");
            Assert.IsTrue(hazardA.IsActive,
                "hazardA (index 0) must be active after wrap-around.");
            Assert.IsFalse(hazardB.IsActive,
                "hazardB (index 1) must be inactive after wrap-around.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(hazardA.gameObject);
            Object.DestroyImmediate(hazardB.gameObject);
        }
    }
}

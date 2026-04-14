using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T233: <see cref="HazardZoneToggleController"/>.
    ///
    /// HazardZoneToggleControllerTests (14):
    ///   FreshInstance_HazardZoneNull                            ×1
    ///   FreshInstance_EnableOnMatchStart_True                   ×1
    ///   OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_NullRefs_DoesNotThrow                         ×1
    ///   OnDisable_Unregisters_BothChannels                      ×1
    ///   HandleMatchStarted_SetsMatchRunning_True                ×1
    ///   HandleMatchEnded_SetsMatchRunning_False                 ×1
    ///   HandleMatchStarted_NoTimedEnable_EnableOnStart_SetsHazardActive    ×1
    ///   HandleMatchStarted_NoTimedEnable_DisableOnStart_HazardInactive     ×1
    ///   HandleMatchStarted_NoTimedEnable_EnableOnStart_FiresHazardActivated×1
    ///   HandleMatchEnded_SetsHazardInactive                     ×1
    ///   HandleMatchStarted_TimedEnable_HazardStartsInactive     ×1
    ///   Tick_TimedEnable_ActivatesAfterDelay                    ×1
    ///   Tick_TimedEnable_NotYetDelay_StaysInactive              ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class HazardZoneToggleControllerTests
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

        private static HazardZoneToggleController CreateToggle() =>
            new GameObject("HazardToggle_Test").AddComponent<HazardZoneToggleController>();

        private static HazardZoneController CreateHazardZone() =>
            new GameObject("HazardZone_Test").AddComponent<HazardZoneController>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_HazardZoneNull()
        {
            var ctrl = CreateToggle();
            Assert.IsNull(ctrl.HazardZone,
                "HazardZone must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_EnableOnMatchStart_True()
        {
            var ctrl = CreateToggle();
            Assert.IsTrue(ctrl.EnableOnMatchStart,
                "EnableOnMatchStart must default to true.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateToggle();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateToggle();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_BothChannels()
        {
            var ctrl    = CreateToggle();
            var started = CreateVoidEvent();
            var ended   = CreateVoidEvent();
            SetField(ctrl, "_onMatchStarted", started);
            SetField(ctrl, "_onMatchEnded",   ended);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int startCount = 0;
            int endCount   = 0;
            started.RegisterCallback(() => startCount++);
            ended.RegisterCallback(() => endCount++);
            started.Raise();
            ended.Raise();

            Assert.AreEqual(1, startCount,
                "After OnDisable only the manual callback fires on _onMatchStarted.");
            Assert.AreEqual(1, endCount,
                "After OnDisable only the manual callback fires on _onMatchEnded.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(started);
            Object.DestroyImmediate(ended);
        }

        [Test]
        public void HandleMatchStarted_SetsMatchRunning_True()
        {
            var ctrl = CreateToggle();
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsMatchRunning,
                "IsMatchRunning must be true after HandleMatchStarted.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchEnded_SetsMatchRunning_False()
        {
            var ctrl = CreateToggle();
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false after HandleMatchEnded.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NoTimedEnable_EnableOnStart_SetsHazardActive()
        {
            var ctrl  = CreateToggle();
            var hazard = CreateHazardZone();
            hazard.IsActive = false;

            SetField(ctrl, "_hazardZone",         hazard);
            SetField(ctrl, "_enableOnMatchStart",  true);
            SetField(ctrl, "_timedEnable",         false);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(hazard.IsActive,
                "HazardZone.IsActive must be true when EnableOnMatchStart=true and TimedEnable=false.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NoTimedEnable_DisableOnStart_HazardInactive()
        {
            var ctrl   = CreateToggle();
            var hazard = CreateHazardZone();
            hazard.IsActive = true;

            SetField(ctrl, "_hazardZone",        hazard);
            SetField(ctrl, "_enableOnMatchStart", false);
            SetField(ctrl, "_timedEnable",        false);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsFalse(hazard.IsActive,
                "HazardZone.IsActive must be false when EnableOnMatchStart=false.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NoTimedEnable_EnableOnStart_FiresHazardActivated()
        {
            var ctrl  = CreateToggle();
            var ch    = CreateVoidEvent();

            SetField(ctrl, "_enableOnMatchStart", true);
            SetField(ctrl, "_timedEnable",        false);
            SetField(ctrl, "_onHazardActivated",  ch);
            InvokePrivate(ctrl, "Awake");

            int count = 0;
            ch.RegisterCallback(() => count++);

            ctrl.HandleMatchStarted();

            Assert.AreEqual(1, count,
                "_onHazardActivated must fire once when EnableOnMatchStart=true and TimedEnable=false.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void HandleMatchEnded_SetsHazardInactive()
        {
            var ctrl   = CreateToggle();
            var hazard = CreateHazardZone();
            hazard.IsActive = true;

            SetField(ctrl, "_hazardZone", hazard);
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(hazard.IsActive,
                "HazardZone.IsActive must be false after HandleMatchEnded.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }

        [Test]
        public void HandleMatchStarted_TimedEnable_HazardStartsInactive()
        {
            var ctrl   = CreateToggle();
            var hazard = CreateHazardZone();
            hazard.IsActive = true;

            SetField(ctrl, "_hazardZone",        hazard);
            SetField(ctrl, "_enableOnMatchStart", true);
            SetField(ctrl, "_timedEnable",        true);
            SetField(ctrl, "_enableDelay",        10f);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsFalse(hazard.IsActive,
                "HazardZone.IsActive must be false at match start when TimedEnable=true.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }

        [Test]
        public void Tick_TimedEnable_ActivatesAfterDelay()
        {
            var ctrl   = CreateToggle();
            var hazard = CreateHazardZone();
            hazard.IsActive = false;

            SetField(ctrl, "_hazardZone",        hazard);
            SetField(ctrl, "_enableOnMatchStart", true);
            SetField(ctrl, "_timedEnable",        true);
            SetField(ctrl, "_enableDelay",        5f);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted(); // arms timer; hazard inactive
            ctrl.Tick(3f);             // 3s — below threshold

            Assert.IsFalse(hazard.IsActive,
                "HazardZone must still be inactive before delay elapses.");

            ctrl.Tick(3f);             // 6s total — delay exceeded

            Assert.IsTrue(hazard.IsActive,
                "HazardZone.IsActive must become true once EnableDelay seconds have elapsed.");
            Assert.IsTrue(ctrl.IsTimedActivated,
                "IsTimedActivated must be true after the delay fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }

        [Test]
        public void Tick_TimedEnable_NotYetDelay_StaysInactive()
        {
            var ctrl   = CreateToggle();
            var hazard = CreateHazardZone();
            hazard.IsActive = false;

            SetField(ctrl, "_hazardZone",        hazard);
            SetField(ctrl, "_enableOnMatchStart", true);
            SetField(ctrl, "_timedEnable",        true);
            SetField(ctrl, "_enableDelay",        10f);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            ctrl.Tick(4f); // 4s — below 10s threshold

            Assert.IsFalse(hazard.IsActive,
                "HazardZone must remain inactive before EnableDelay seconds elapse.");
            Assert.IsFalse(ctrl.IsTimedActivated,
                "IsTimedActivated must remain false before delay fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }
    }
}

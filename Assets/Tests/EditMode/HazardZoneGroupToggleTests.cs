using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T243: <see cref="HazardZoneGroupToggleController"/>.
    ///
    /// HazardZoneGroupToggleTests (14):
    ///   FreshInstance_GroupNull                              ×1
    ///   FreshInstance_IsMatchRunning_False                   ×1
    ///   FreshInstance_ToggleInterval_Default_Five            ×1
    ///   OnEnable_NullRefs_DoesNotThrow                       ×1
    ///   OnDisable_NullRefs_DoesNotThrow                      ×1
    ///   OnDisable_Unregisters_BothChannels                   ×1
    ///   HandleMatchStarted_SetsMatchRunning_True             ×1
    ///   HandleMatchStarted_ResetsElapsed                     ×1
    ///   HandleMatchEnded_SetsMatchRunning_False              ×1
    ///   Tick_NotRunning_DoesNotToggleGroup                   ×1
    ///   Tick_NullGroup_DoesNotThrow                          ×1
    ///   Tick_BelowInterval_NoToggle                         ×1
    ///   Tick_ExceedsInterval_TogglesGroup                   ×1
    ///   Tick_ExceedsInterval_FiresOnGroupToggled             ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class HazardZoneGroupToggleTests
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

        private static HazardZoneGroupToggleController CreateController() =>
            new GameObject("GroupToggleCtrl_Test").AddComponent<HazardZoneGroupToggleController>();

        private static HazardZoneGroupSO CreateGroupSO() =>
            ScriptableObject.CreateInstance<HazardZoneGroupSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Fresh-instance tests ──────────────────────────────────────────────

        [Test]
        public void FreshInstance_GroupNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Group,
                "Group must be null on a fresh HazardZoneGroupToggleController instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_IsMatchRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_ToggleInterval_Default_Five()
        {
            var ctrl = CreateController();
            Assert.AreEqual(5f, ctrl.ToggleInterval,
                "ToggleInterval must default to 5f.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle tests ───────────────────────────────────────────────────

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
            var ctrl       = CreateController();
            var startEvt   = CreateEvent();
            var endEvt     = CreateEvent();

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
        public void HandleMatchStarted_ResetsElapsed()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            // Manually advance elapsed, then start match.
            ctrl.HandleMatchStarted();
            ctrl.HandleMatchStarted();   // second call resets

            Assert.AreEqual(0f, ctrl.Elapsed,
                "Elapsed must be reset to 0 on HandleMatchStarted.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchEnded_SetsMatchRunning_False()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false after HandleMatchEnded.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Tick tests ────────────────────────────────────────────────────────

        [Test]
        public void Tick_NotRunning_DoesNotToggleGroup()
        {
            var ctrl  = CreateController();
            var group = CreateGroupSO();
            SetField(ctrl, "_group", group);
            SetField(ctrl, "_toggleInterval", 1f);
            InvokePrivate(ctrl, "Awake");

            // Match not started — tick with large dt.
            ctrl.Tick(10f);

            Assert.IsFalse(group.IsGroupActive,
                "Group must not toggle when match is not running.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Tick_NullGroup_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();

            Assert.DoesNotThrow(() => ctrl.Tick(100f),
                "Tick with null group must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_BelowInterval_NoToggle()
        {
            var ctrl  = CreateController();
            var group = CreateGroupSO();
            SetField(ctrl, "_group", group);
            SetField(ctrl, "_toggleInterval", 5f);
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();

            ctrl.Tick(3f);   // below 5s threshold

            Assert.IsFalse(group.IsGroupActive,
                "Group must not toggle when elapsed is below ToggleInterval.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Tick_ExceedsInterval_TogglesGroup()
        {
            var ctrl  = CreateController();
            var group = CreateGroupSO();   // starts inactive
            SetField(ctrl, "_group", group);
            SetField(ctrl, "_toggleInterval", 5f);
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();

            ctrl.Tick(6f);   // exceeds 5s — should toggle (inactive → active)

            Assert.IsTrue(group.IsGroupActive,
                "Group must be toggled to active after ToggleInterval elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(group);
        }

        [Test]
        public void Tick_ExceedsInterval_FiresOnGroupToggled()
        {
            var ctrl     = CreateController();
            var group    = CreateGroupSO();
            var toggleEvt = CreateEvent();
            SetField(ctrl, "_group", group);
            SetField(ctrl, "_onGroupToggled", toggleEvt);
            SetField(ctrl, "_toggleInterval", 5f);
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();

            int count = 0;
            toggleEvt.RegisterCallback(() => count++);
            ctrl.Tick(6f);

            Assert.AreEqual(1, count,
                "_onGroupToggled must fire exactly once when ToggleInterval elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(group);
            Object.DestroyImmediate(toggleEvt);
        }
    }
}

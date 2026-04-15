using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T253: <see cref="MatchPaceSO"/> and
    /// <see cref="MatchPaceController"/>.
    ///
    /// MatchPaceTests (14):
    ///   SO_FreshInstance_EventCount_Zero                            ×1
    ///   SO_FreshInstance_WindowDuration_Default_Ten                 ×1
    ///   SO_FreshInstance_FastThreshold_Default_Five                 ×1
    ///   SO_FreshInstance_SlowThreshold_Default_One                  ×1
    ///   SO_IncrementEvent_IncreasesCount                            ×1
    ///   SO_Tick_ExceedsWindow_EvaluatesFastPace                     ×1
    ///   SO_Tick_ExceedsWindow_EvaluatesSlowPace                     ×1
    ///   Controller_FreshInstance_PaceSO_Null                        ×1
    ///   Controller_FreshInstance_IsMatchRunning_False               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_Unregisters_BothChannels               ×1
    ///   Controller_HandleMatchStarted_SetsRunning_ResetsPace        ×1
    ///   Controller_HandleMatchEnded_ClearsRunning                   ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class MatchPaceTests
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

        private static MatchPaceSO CreatePaceSO() =>
            ScriptableObject.CreateInstance<MatchPaceSO>();

        private static MatchPaceController CreateController() =>
            new GameObject("MatchPace_Test").AddComponent<MatchPaceController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EventCount_Zero()
        {
            var so = CreatePaceSO();
            Assert.AreEqual(0, so.EventCount,
                "EventCount must be 0 on a fresh MatchPaceSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WindowDuration_Default_Ten()
        {
            var so = CreatePaceSO();
            Assert.AreEqual(10f, so.WindowDuration, 0.001f,
                "WindowDuration must default to 10 seconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FastThreshold_Default_Five()
        {
            var so = CreatePaceSO();
            Assert.AreEqual(5, so.FastThreshold,
                "FastThreshold must default to 5.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SlowThreshold_Default_One()
        {
            var so = CreatePaceSO();
            Assert.AreEqual(1, so.SlowThreshold,
                "SlowThreshold must default to 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IncrementEvent_IncreasesCount()
        {
            var so = CreatePaceSO();
            so.IncrementEvent();
            so.IncrementEvent();
            Assert.AreEqual(2, so.EventCount,
                "IncrementEvent must increase EventCount by 1 each call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExceedsWindow_EvaluatesFastPace()
        {
            var so     = CreatePaceSO();
            var evt    = CreateEvent();
            int called = 0;
            SetField(so, "_onFastPace", evt);
            evt.RegisterCallback(() => called++);

            // Add enough events to meet fast threshold (default 5).
            for (int i = 0; i < 5; i++)
                so.IncrementEvent();

            // Close the window (default 10 seconds).
            so.Tick(10f);

            Assert.AreEqual(1, called,
                "_onFastPace must fire once when event count meets FastThreshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_ExceedsWindow_EvaluatesSlowPace()
        {
            var so     = CreatePaceSO();
            var evt    = CreateEvent();
            int called = 0;
            SetField(so, "_onSlowPace", evt);
            evt.RegisterCallback(() => called++);

            // Add only 1 event — at or below default SlowThreshold of 1.
            so.IncrementEvent();

            // Close the window.
            so.Tick(10f);

            Assert.AreEqual(1, called,
                "_onSlowPace must fire once when event count is at or below SlowThreshold.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_PaceSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PaceSO,
                "PaceSO must be null on a fresh MatchPaceController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_IsMatchRunning_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false on a fresh MatchPaceController.");
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
            var ctrl  = CreateController();
            var start = CreateEvent();
            var end   = CreateEvent();
            SetField(ctrl, "_onMatchStarted", start);
            SetField(ctrl, "_onMatchEnded",   end);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After disable, raising start must NOT set IsMatchRunning.
            start.Raise();
            Assert.IsFalse(ctrl.IsMatchRunning,
                "After OnDisable, _onMatchStarted must not update IsMatchRunning.");

            // Confirm end channel also unregistered (would flip to false but it's
            // already false — test that no exception is thrown).
            Assert.DoesNotThrow(() => end.Raise(),
                "After OnDisable, raising _onMatchEnded must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
        }

        [Test]
        public void Controller_HandleMatchStarted_SetsRunning_ResetsPace()
        {
            var ctrl   = CreateController();
            var paceSO = CreatePaceSO();
            SetField(ctrl, "_paceSO", paceSO);
            InvokePrivate(ctrl, "Awake");

            // Pre-condition: add events to the SO.
            paceSO.IncrementEvent();
            paceSO.IncrementEvent();
            Assert.AreEqual(2, paceSO.EventCount, "Pre-condition: EventCount must be 2.");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsMatchRunning,
                "HandleMatchStarted must set IsMatchRunning to true.");
            Assert.AreEqual(0, paceSO.EventCount,
                "HandleMatchStarted must reset EventCount on the SO.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(paceSO);
        }

        [Test]
        public void Controller_HandleMatchEnded_ClearsRunning()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();
            Assert.IsTrue(ctrl.IsMatchRunning, "Pre-condition: match must be running.");

            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsMatchRunning,
                "HandleMatchEnded must set IsMatchRunning to false.");

            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}

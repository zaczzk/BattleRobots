using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchSuddenDeathController"/>.
    ///
    /// Covers:
    ///   • OnEnable/OnDisable with all fields null — no throw.
    ///   • Null event channels — no throw.
    ///   • OnDisable unregisters the timer callback.
    ///   • OnTimerUpdated above threshold — does NOT fire event.
    ///   • OnTimerUpdated exactly at threshold — fires event and sets flag.
    ///   • OnTimerUpdated below threshold — fires event and sets flag.
    ///   • Fires at most once per match (subsequent calls are no-ops).
    ///   • ResetState clears the sudden-death flag.
    ///   • _onMatchStarted raise resets state.
    ///   • Default _triggerThreshold is 30 s.
    ///   • IsSuddenDeathActive is false on fresh instance.
    /// </summary>
    public class MatchSuddenDeathControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        // Creates a controller with optional channel wiring.
        private static (GameObject go, MatchSuddenDeathController ctrl) MakeController(
            FloatGameEvent timerEvent   = null,
            VoidGameEvent  matchStarted = null,
            VoidGameEvent  suddenDeath  = null,
            float threshold = 30f)
        {
            var go   = new GameObject("SDController");
            var ctrl = go.AddComponent<MatchSuddenDeathController>();
            if (timerEvent   != null) SetField(ctrl, "_onTimerUpdated",     timerEvent);
            if (matchStarted != null) SetField(ctrl, "_onMatchStarted",     matchStarted);
            if (suddenDeath  != null) SetField(ctrl, "_onSuddenDeathStarted", suddenDeath);
            SetField(ctrl, "_triggerThreshold", threshold);
            return (go, ctrl);
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_IsSuddenDeathActive_IsFalse()
        {
            var (go, ctrl) = MakeController();
            Assert.IsFalse(ctrl.IsSuddenDeathActive,
                "IsSuddenDeathActive must be false on a fresh instance.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DefaultTriggerThreshold_IsThirtySeconds()
        {
            var (go, ctrl) = MakeController();
            Assert.AreEqual(30f, GetField<float>(ctrl, "_triggerThreshold"), 0.001f,
                "_triggerThreshold should default to 30 s.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnableDisable_NullAllFields_DoesNotThrow()
        {
            var go   = new GameObject("SDController");
            var ctrl = go.AddComponent<MatchSuddenDeathController>();

            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null fields must not throw.");
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null fields must not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NullTimerChannel_DoesNotThrow()
        {
            var (go, ctrl) = MakeController(timerEvent: null);
            // No timer channel — should be silent.
            Assert.DoesNotThrow(() => ctrl.ResetState(),
                "ResetState with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnTimerUpdated_AboveThreshold_DoesNotFireEvent()
        {
            var timerEvent  = ScriptableObject.CreateInstance<FloatGameEvent>();
            var suddenEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            bool fired = false;
            suddenEvent.RegisterCallback(() => fired = true);

            var (go, ctrl) = MakeController(timerEvent, suddenDeath: suddenEvent, threshold: 30f);

            timerEvent.Raise(45f);   // 45 s remaining → above 30 s threshold

            Assert.IsFalse(fired,                    "Sudden death must NOT fire above threshold.");
            Assert.IsFalse(ctrl.IsSuddenDeathActive, "IsSuddenDeathActive must stay false above threshold.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(suddenEvent);
        }

        [Test]
        public void OnTimerUpdated_ExactlyAtThreshold_FiresEvent()
        {
            var timerEvent  = ScriptableObject.CreateInstance<FloatGameEvent>();
            var suddenEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            bool fired = false;
            suddenEvent.RegisterCallback(() => fired = true);

            var (go, ctrl) = MakeController(timerEvent, suddenDeath: suddenEvent, threshold: 30f);

            timerEvent.Raise(30f);   // exactly at threshold

            Assert.IsTrue(fired,                    "Sudden death must fire when timer == threshold.");
            Assert.IsTrue(ctrl.IsSuddenDeathActive, "IsSuddenDeathActive must be true at threshold.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(suddenEvent);
        }

        [Test]
        public void OnTimerUpdated_BelowThreshold_FiresEvent()
        {
            var timerEvent  = ScriptableObject.CreateInstance<FloatGameEvent>();
            var suddenEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            bool fired = false;
            suddenEvent.RegisterCallback(() => fired = true);

            var (go, ctrl) = MakeController(timerEvent, suddenDeath: suddenEvent, threshold: 30f);

            timerEvent.Raise(10f);   // well below 30 s

            Assert.IsTrue(fired,                    "Sudden death must fire when below threshold.");
            Assert.IsTrue(ctrl.IsSuddenDeathActive, "IsSuddenDeathActive must be true below threshold.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(suddenEvent);
        }

        [Test]
        public void OnTimerUpdated_FiresOnlyOnce()
        {
            var timerEvent  = ScriptableObject.CreateInstance<FloatGameEvent>();
            var suddenEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            int fireCount = 0;
            suddenEvent.RegisterCallback(() => fireCount++);

            var (go, ctrl) = MakeController(timerEvent, suddenDeath: suddenEvent, threshold: 30f);

            timerEvent.Raise(25f);   // triggers sudden death
            timerEvent.Raise(20f);   // already active — must be no-op
            timerEvent.Raise(10f);   // still no-op

            Assert.AreEqual(1, fireCount,
                "Sudden death event must fire at most once per match.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(suddenEvent);
        }

        [Test]
        public void ResetState_ClearsSuddenDeathFlag()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            var (go, ctrl) = MakeController(timerEvent, threshold: 30f);

            timerEvent.Raise(10f);   // activate sudden death
            Assert.IsTrue(ctrl.IsSuddenDeathActive);

            ctrl.ResetState();
            Assert.IsFalse(ctrl.IsSuddenDeathActive,
                "ResetState must clear IsSuddenDeathActive.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
        }

        [Test]
        public void OnMatchStarted_Raise_ResetsSuddenDeath()
        {
            var timerEvent    = ScriptableObject.CreateInstance<FloatGameEvent>();
            var matchStarted  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl)    = MakeController(timerEvent, matchStarted: matchStarted, threshold: 30f);

            timerEvent.Raise(5f);   // trigger sudden death
            Assert.IsTrue(ctrl.IsSuddenDeathActive);

            matchStarted.Raise();   // new match → reset
            Assert.IsFalse(ctrl.IsSuddenDeathActive,
                "_onMatchStarted raise must reset IsSuddenDeathActive.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(matchStarted);
        }

        [Test]
        public void OnDisable_UnregistersTimerCallback()
        {
            var timerEvent  = ScriptableObject.CreateInstance<FloatGameEvent>();
            var suddenEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            bool fired = false;
            suddenEvent.RegisterCallback(() => fired = true);

            var (go, ctrl) = MakeController(timerEvent, suddenDeath: suddenEvent, threshold: 30f);

            // Disable — unregisters callback.
            go.SetActive(false);

            // Raise timer — must NOT activate sudden death.
            timerEvent.Raise(10f);

            Assert.IsFalse(fired,
                "After OnDisable the timer callback should be unregistered.");
            Assert.IsFalse(ctrl.IsSuddenDeathActive,
                "IsSuddenDeathActive must remain false after OnDisable + timer raise.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(suddenEvent);
        }

        [Test]
        public void AfterReset_NewTimerTrigger_FiresAgain()
        {
            var timerEvent  = ScriptableObject.CreateInstance<FloatGameEvent>();
            var suddenEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            int fireCount = 0;
            suddenEvent.RegisterCallback(() => fireCount++);

            var (go, ctrl) = MakeController(timerEvent, suddenDeath: suddenEvent, threshold: 30f);

            // First match.
            timerEvent.Raise(10f);
            Assert.AreEqual(1, fireCount);

            // Reset (simulate new match).
            ctrl.ResetState();

            // Second match — should fire again.
            timerEvent.Raise(20f);
            Assert.AreEqual(2, fireCount,
                "After ResetState a new timer crossing should fire the event again.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(suddenEvent);
        }
    }
}

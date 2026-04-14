using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T216:
    ///   <see cref="MatchTimerSO"/> and <see cref="MatchTimerController"/>.
    ///
    /// MatchTimerSOTests (10):
    ///   FreshInstance_DefaultDuration_Is120                       ×1
    ///   FreshInstance_IsRunning_False                             ×1
    ///   FreshInstance_TimeRemaining_EqualsDuration                ×1
    ///   StartTimer_SetsIsRunning                                  ×1
    ///   StopTimer_ClearsIsRunning                                 ×1
    ///   Tick_Running_DecrementsTimeRemaining                      ×1
    ///   Tick_NotRunning_NoDecrement                               ×1
    ///   Tick_Clamps_ToZero                                        ×1
    ///   Tick_Expires_FiresOnTimerExpired                          ×1
    ///   Reset_RestoresStateAndClearsRunning                       ×1
    ///
    /// MatchTimerControllerTests (6):
    ///   FreshInstance_WarningThreshold_Is30                       ×1
    ///   OnEnable_NullRefs_DoesNotThrow                            ×1
    ///   OnDisable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_Unregisters                                     ×1
    ///   OnTimerUpdated_FormatsLabel_AboveThreshold                ×1
    ///   OnTimerUpdated_AppliesWarningColor_AtOrBelowThreshold     ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MatchTimerTests
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

        private static void InvokeWithFloat(object target, string method, float value)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, new object[] { value });
        }

        private static MatchTimerSO CreateSO(float duration = 120f)
        {
            var so = ScriptableObject.CreateInstance<MatchTimerSO>();
            SetField(so, "_duration", duration);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static MatchTimerController CreateController() =>
            new GameObject("MatchTimerController_Test").AddComponent<MatchTimerController>();

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static FloatGameEvent CreateFloatEvent() =>
            ScriptableObject.CreateInstance<FloatGameEvent>();

        // ── MatchTimerSOTests ──────────────────────────────────────────────────

        [Test]
        public void FreshInstance_DefaultDuration_Is120()
        {
            var so = ScriptableObject.CreateInstance<MatchTimerSO>();
            Assert.AreEqual(120f, so.Duration, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsRunning_False()
        {
            var so = CreateSO();
            Assert.IsFalse(so.IsRunning);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_TimeRemaining_EqualsDuration()
        {
            var so = CreateSO(60f);
            Assert.AreEqual(60f, so.TimeRemaining, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void StartTimer_SetsIsRunning()
        {
            var so = CreateSO();
            so.StartTimer();
            Assert.IsTrue(so.IsRunning);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void StopTimer_ClearsIsRunning()
        {
            var so = CreateSO();
            so.StartTimer();
            so.StopTimer();
            Assert.IsFalse(so.IsRunning);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_Running_DecrementsTimeRemaining()
        {
            var so = CreateSO(100f);
            so.StartTimer();
            so.Tick(10f);
            Assert.AreEqual(90f, so.TimeRemaining, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_NotRunning_NoDecrement()
        {
            var so = CreateSO(100f);
            // Not running — Tick should be a no-op.
            so.Tick(10f);
            Assert.AreEqual(100f, so.TimeRemaining, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_Clamps_ToZero()
        {
            var so = CreateSO(5f);
            so.StartTimer();
            so.Tick(100f);
            Assert.AreEqual(0f, so.TimeRemaining, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Tick_Expires_FiresOnTimerExpired()
        {
            var so       = CreateSO(5f);
            var expEvent = CreateVoidEvent();
            SetField(so, "_onTimerExpired", expEvent);

            int firedCount = 0;
            expEvent.RegisterCallback(() => firedCount++);

            so.StartTimer();
            so.Tick(10f);   // overshoots to zero

            Assert.AreEqual(1, firedCount, "_onTimerExpired should fire exactly once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(expEvent);
        }

        [Test]
        public void Reset_RestoresStateAndClearsRunning()
        {
            var so = CreateSO(50f);
            so.StartTimer();
            so.Tick(20f);
            so.Reset();

            Assert.AreEqual(50f, so.TimeRemaining, 0.001f,
                "TimeRemaining must equal Duration after Reset.");
            Assert.IsFalse(so.IsRunning, "IsRunning must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── MatchTimerControllerTests ──────────────────────────────────────────

        [Test]
        public void FreshInstance_WarningThreshold_Is30()
        {
            var ctrl = CreateController();
            Assert.AreEqual(30f, ctrl.WarningThreshold, 0.001f);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateFloatEvent();
            SetField(ctrl, "_onTimerUpdated", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback((f) => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise(10f);

            Assert.AreEqual(1, count, "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnTimerUpdated_FormatsLabel_AboveThreshold()
        {
            var ctrl  = CreateController();
            var label = new GameObject("label").AddComponent<Text>();
            SetField(ctrl, "_timerLabel", label);
            SetField(ctrl, "_warningThreshold", 30f);
            SetField(ctrl, "_normalColor", Color.white);
            SetField(ctrl, "_warningColor", Color.red);

            InvokePrivate(ctrl, "Awake");
            InvokeWithFloat(ctrl, "OnTimerUpdated", 90f);  // 1m 30s, above threshold

            // CeilToInt(90) = 90 → 1:30
            Assert.AreEqual("1:30", label.text, "Label should format as M:SS.");
            Assert.AreEqual(Color.white, label.color, "Color should be normalColor above threshold.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }

        [Test]
        public void OnTimerUpdated_AppliesWarningColor_AtOrBelowThreshold()
        {
            var ctrl  = CreateController();
            var label = new GameObject("label").AddComponent<Text>();
            SetField(ctrl, "_timerLabel", label);
            SetField(ctrl, "_warningThreshold", 30f);
            SetField(ctrl, "_normalColor", Color.white);
            SetField(ctrl, "_warningColor", Color.red);

            InvokePrivate(ctrl, "Awake");
            InvokeWithFloat(ctrl, "OnTimerUpdated", 30f);  // exactly at threshold

            Assert.AreEqual(Color.red, label.color, "Color should be warningColor at threshold.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }
    }
}

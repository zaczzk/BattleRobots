using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CountdownManager"/>.
    ///
    /// ── EditMode coroutine caveat ─────────────────────────────────────────────
    ///   In EditMode, <c>StartCoroutine</c> launches but no frames advance, so
    ///   <c>WaitForSeconds</c> yields never resolve.  Tests therefore cover:
    ///   • <c>_countdownFrom ≤ 0</c>                    — synchronous; events fire immediately.
    ///   • <c>_countdownFrom &gt; 0, both delays == 0</c> — synchronous path; all ticks + complete
    ///     fire in the same frame (the <c>RunSynchronousCountdown</c> branch).
    ///   • <c>_countdownFrom &gt; 0, any delay &gt; 0</c>  — coroutine starts; events must NOT
    ///     fire synchronously (yield never resolves in EditMode).
    ///   • Null-guard: missing <c>_matchStartedEvent</c> → no exception.
    ///
    /// All tests use the inactive-GO pattern: the GO is created with
    /// <c>SetActive(false)</c> so fields can be injected via reflection before
    /// Start() runs; <c>SetActive(true)</c> triggers Awake() then Start().
    /// </summary>
    public class CountdownManagerTests
    {
        private GameObject       _go;
        private CountdownManager _manager;
        private VoidGameEvent    _matchStartedEvent;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go                = new GameObject("CountdownManager");
            _go.SetActive(false);
            _manager           = _go.AddComponent<CountdownManager>();
            _matchStartedEvent = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_matchStartedEvent);
            _go = null; _manager = null; _matchStartedEvent = null;
        }

        // ── Null-event guard ──────────────────────────────────────────────────

        [Test]
        public void Start_NullMatchStartedEvent_DoesNotThrow()
        {
            // No event assigned; Start() must log error and return without throwing.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "CountdownManager.Start() must not throw when _matchStartedEvent is null.");
        }

        // ── Zero countdown, zero delays ───────────────────────────────────────

        [Test]
        public void Start_ZeroCountdown_ZeroDelays_FiresCompleteEvent()
        {
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownComplete", complete);
            SetField(_manager, "_countdownFrom", 0);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            int raised = 0;
            complete.RegisterCallback(() => raised++);

            _go.SetActive(true);

            Assert.AreEqual(1, raised,
                "_onCountdownComplete must fire immediately when _countdownFrom is 0.");

            Object.DestroyImmediate(complete);
        }

        [Test]
        public void Start_ZeroCountdown_ZeroDelays_FiresMatchStartedEvent()
        {
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_countdownFrom", 0);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            int raised = 0;
            _matchStartedEvent.RegisterCallback(() => raised++);

            _go.SetActive(true);

            Assert.AreEqual(1, raised,
                "_matchStartedEvent must fire immediately when _countdownFrom is 0.");
        }

        [Test]
        public void Start_ZeroCountdown_ZeroDelays_DoesNotFireTickEvent()
        {
            var tick = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownTick", tick);
            SetField(_manager, "_countdownFrom", 0);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            int raised = 0;
            tick.RegisterCallback(v => raised++);

            _go.SetActive(true);

            Assert.AreEqual(0, raised,
                "_onCountdownTick must not fire when _countdownFrom is 0 (no ticks to emit).");

            Object.DestroyImmediate(tick);
        }

        // ── Positive countdown, zero delays (synchronous path) ────────────────

        [Test]
        public void Start_PositiveCountdown_ZeroDelays_FiresTicksInDescendingOrder()
        {
            var tick = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownTick", tick);
            SetField(_manager, "_countdownFrom", 3);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            var values = new List<int>();
            tick.RegisterCallback(v => values.Add(v));

            _go.SetActive(true);

            Assert.AreEqual(new List<int> { 3, 2, 1 }, values,
                "Ticks must fire in descending order from _countdownFrom to 1.");

            Object.DestroyImmediate(tick);
        }

        [Test]
        public void Start_PositiveCountdown_ZeroDelays_TickCount_EqualsCountdownFrom()
        {
            var tick = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownTick", tick);
            SetField(_manager, "_countdownFrom", 5);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            int count = 0;
            tick.RegisterCallback(_ => count++);

            _go.SetActive(true);

            Assert.AreEqual(5, count,
                "The number of ticks must equal _countdownFrom (5 ticks for 5-2-1).");

            Object.DestroyImmediate(tick);
        }

        [Test]
        public void Start_PositiveCountdown_ZeroDelays_FiresMatchStartedAfterAllTicks()
        {
            var tick = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownTick", tick);
            SetField(_manager, "_countdownFrom", 3);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            var order = new List<string>();
            tick.RegisterCallback(_ => order.Add("tick"));
            _matchStartedEvent.RegisterCallback(() => order.Add("start"));

            _go.SetActive(true);

            // Last entry must be "start"; all three "tick" entries must precede it.
            Assert.AreEqual(4, order.Count,
                "Expected 3 tick events and 1 matchStarted event.");
            Assert.AreEqual("start", order[order.Count - 1],
                "_matchStartedEvent must fire after all ticks are emitted.");

            Object.DestroyImmediate(tick);
        }

        [Test]
        public void Start_PositiveCountdown_ZeroDelays_FiresCompleteBeforeMatchStarted()
        {
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownComplete", complete);
            SetField(_manager, "_countdownFrom", 2);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            var order = new List<string>();
            complete.RegisterCallback(() => order.Add("complete"));
            _matchStartedEvent.RegisterCallback(() => order.Add("start"));

            _go.SetActive(true);

            Assert.AreEqual(2, order.Count,
                "Expected exactly _onCountdownComplete then _matchStartedEvent.");
            Assert.AreEqual("complete", order[0],
                "_onCountdownComplete must fire before _matchStartedEvent.");
            Assert.AreEqual("start", order[1],
                "_matchStartedEvent must fire immediately after _onCountdownComplete.");

            Object.DestroyImmediate(complete);
        }

        [Test]
        public void Start_SingleCountdown_ZeroDelays_OneTickThenComplete()
        {
            var tick     = ScriptableObject.CreateInstance<IntGameEvent>();
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownTick", tick);
            SetField(_manager, "_onCountdownComplete", complete);
            SetField(_manager, "_countdownFrom", 1);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            var order = new List<string>();
            tick.RegisterCallback(v => order.Add($"tick{v}"));
            complete.RegisterCallback(() => order.Add("complete"));

            _go.SetActive(true);

            Assert.AreEqual(new List<string> { "tick1", "complete" }, order,
                "For _countdownFrom=1: must fire tick(1), then complete.");

            Object.DestroyImmediate(tick);
            Object.DestroyImmediate(complete);
        }

        // ── Positive countdown + positive delay (coroutine path — EditMode caveat) ─

        [Test]
        public void Start_PositiveCountdown_PositiveDelay_DoesNotFireEventsSynchronously()
        {
            var tick     = ScriptableObject.CreateInstance<IntGameEvent>();
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_onCountdownTick", tick);
            SetField(_manager, "_onCountdownComplete", complete);
            SetField(_manager, "_countdownFrom", 3);
            SetField(_manager, "_initialDelay", 0.1f); // any > 0 → coroutine
            SetField(_manager, "_tickInterval", 1f);

            int tickCount  = 0;
            int startCount = 0;
            tick.RegisterCallback(_ => tickCount++);
            _matchStartedEvent.RegisterCallback(() => startCount++);

            _go.SetActive(true); // coroutine starts; yield never resolves in EditMode

            Assert.AreEqual(0, tickCount,
                "With _initialDelay > 0, no tick must fire synchronously in EditMode.");
            Assert.AreEqual(0, startCount,
                "With _initialDelay > 0, _matchStartedEvent must not fire synchronously.");

            Object.DestroyImmediate(tick);
            Object.DestroyImmediate(complete);
        }

        // ── Null optional event guards ────────────────────────────────────────

        [Test]
        public void Start_NullTickEvent_PositiveCountdown_ZeroDelays_DoesNotThrow()
        {
            // _onCountdownTick left null; ?. guard must silently skip Raise calls.
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_countdownFrom", 3);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Null _onCountdownTick must not throw during tick emissions.");
        }

        [Test]
        public void Start_NullCompleteEvent_ZeroCountdown_DoesNotThrow()
        {
            // _onCountdownComplete left null; ?. guard must silently skip.
            SetField(_manager, "_matchStartedEvent", _matchStartedEvent);
            SetField(_manager, "_countdownFrom", 0);
            SetField(_manager, "_initialDelay", 0f);
            SetField(_manager, "_tickInterval", 0f);

            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Null _onCountdownComplete must not throw when FireCompleteAndStart() is called.");
        }

        // ── Default inspector values ──────────────────────────────────────────

        [Test]
        public void DefaultCountdownFrom_IsThree()
        {
            int value = GetField<int>(_manager, "_countdownFrom");
            Assert.AreEqual(3, value,
                "_countdownFrom default must be 3 for a standard '3, 2, 1' countdown.");
        }

        [Test]
        public void DefaultTickInterval_IsOneSec()
        {
            float value = GetField<float>(_manager, "_tickInterval");
            Assert.AreEqual(1f, value, 0.001f,
                "_tickInterval default must be 1 s so ticks show for one second each.");
        }

        [Test]
        public void DefaultInitialDelay_IsPointOneSec()
        {
            float value = GetField<float>(_manager, "_initialDelay");
            Assert.AreEqual(0.1f, value, 0.001f,
                "_initialDelay default must be 0.1 s to let ArticulationBody physics settle.");
        }
    }
}

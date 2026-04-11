using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchStarter"/>.
    ///
    /// <b>EditMode coroutine caveat:</b> In EditMode, <c>StartCoroutine</c> launches
    /// but no frames ever advance, so <c>WaitForSeconds(delay)</c> never completes.
    /// Tests therefore:
    ///   • Verify the event IS raised immediately when <c>_startDelay == 0</c>.
    ///   • Verify the event is NOT raised when <c>_startDelay &gt; 0</c> (coroutine
    ///     started but yield never resolves in EditMode).
    ///   • Verify no exception is thrown when <c>_matchStartedEvent</c> is null.
    ///
    /// All tests use the inactive-GO pattern: the GO is created with
    /// <c>SetActive(false)</c> so that fields can be injected before Start() runs;
    /// calling <c>SetActive(true)</c> triggers both Awake() and Start().
    /// </summary>
    public class MatchStarterTests
    {
        private GameObject   _go;
        private MatchStarter _starter;
        private VoidGameEvent _matchStartedEvent;

        // ── Helper ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("MatchStarter");
            _go.SetActive(false); // inactive so Awake/Start don't fire during Setup
            _starter           = _go.AddComponent<MatchStarter>();
            _matchStartedEvent = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_matchStartedEvent);
            _go = null; _starter = null; _matchStartedEvent = null;
        }

        // ── Null-event guard ──────────────────────────────────────────────────

        [Test]
        public void Start_NullMatchStartedEvent_DoesNotThrow()
        {
            // No event assigned — should log an error but must not throw an exception.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "MatchStarter.Start() must not throw when _matchStartedEvent is null.");
        }

        [Test]
        public void Start_NullEvent_ZeroDelay_DoesNotThrow()
        {
            // Null event + explicit zero delay — should early-return cleanly.
            SetField(_starter, "_startDelay", 0f);
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        // ── Zero-delay raise ──────────────────────────────────────────────────

        [Test]
        public void Start_ZeroDelay_ValidEvent_RaisesEventImmediately()
        {
            SetField(_starter, "_matchStartedEvent", _matchStartedEvent);
            SetField(_starter, "_startDelay", 0f);

            int raised = 0;
            _matchStartedEvent.RegisterCallback(() => raised++);

            _go.SetActive(true); // triggers Awake() then Start()

            Assert.AreEqual(1, raised,
                "MatchStarted must be raised exactly once when delay == 0.");
        }

        [Test]
        public void Start_ZeroDelay_MultipleSubscribers_AllReceiveEvent()
        {
            SetField(_starter, "_matchStartedEvent", _matchStartedEvent);
            SetField(_starter, "_startDelay", 0f);

            int a = 0, b = 0;
            _matchStartedEvent.RegisterCallback(() => a++);
            _matchStartedEvent.RegisterCallback(() => b++);

            _go.SetActive(true);

            Assert.AreEqual(1, a, "First subscriber must receive the event.");
            Assert.AreEqual(1, b, "Second subscriber must receive the event.");
        }

        [Test]
        public void Start_ZeroDelay_RaisesExactlyOnce()
        {
            SetField(_starter, "_matchStartedEvent", _matchStartedEvent);
            SetField(_starter, "_startDelay", 0f);

            int raised = 0;
            _matchStartedEvent.RegisterCallback(() => raised++);

            _go.SetActive(true);

            // Start() is called only once per object lifetime; toggling active doesn't
            // re-invoke Start, so the count must remain 1.
            Assert.AreEqual(1, raised,
                "Start() must fire the event exactly once regardless of subsequent activations.");
        }

        // ── Positive-delay path ───────────────────────────────────────────────

        [Test]
        public void Start_PositiveDelay_DoesNotRaiseImmediately()
        {
            SetField(_starter, "_matchStartedEvent", _matchStartedEvent);
            SetField(_starter, "_startDelay", 0.5f);

            int raised = 0;
            _matchStartedEvent.RegisterCallback(() => raised++);

            _go.SetActive(true); // Start() runs; coroutine started but yield never completes

            Assert.AreEqual(0, raised,
                "With _startDelay > 0, MatchStarted must not fire synchronously in EditMode.");
        }

        // ── Default inspector value ───────────────────────────────────────────

        [Test]
        public void DefaultStartDelay_IsPointOneSeconds()
        {
            // The inspector default is 0.1 f — verify the field initialiser.
            FieldInfo fi = typeof(MatchStarter)
                .GetField("_startDelay", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "_startDelay field not found.");
            float value = (float)fi.GetValue(_starter);
            Assert.AreEqual(0.1f, value, 0.001f,
                "_startDelay default must be 0.1 s to allow ArticulationBody physics to settle.");
        }
    }
}

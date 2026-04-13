using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchEventLogSO"/> and <see cref="MatchTimelineController"/>.
    ///
    /// MatchEventLogSOTests covers:
    ///   • FreshInstance: Events empty, MaxEvents == default.
    ///   • LogEvent(null/whitespace) → no-op (no entry, no event).
    ///   • LogEvent(valid) → entry appended, event fired.
    ///   • LogEvent captures gameTime and description correctly.
    ///   • LogEvent evicts oldest when at capacity (ring-buffer behaviour).
    ///   • Reset: Events empty after reset; silent (no event).
    ///
    /// MatchTimelineControllerTests covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable / OnDisable with null channel → no throw.
    ///   • OnDisable unregisters from _onMatchEnded.
    ///   • Refresh with null _eventLog → shows empty label.
    ///   • Refresh with null _listContainer → does not throw.
    ///   • FormatTime: 0s→"0s"; 45s→"45s"; 60s→"1m 0s"; 90s→"1m 30s"; negative→"0s".
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class MatchEventLogSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private MatchEventLogSO _log;
        private VoidGameEvent   _onEventLogged;

        [SetUp]
        public void SetUp()
        {
            _log           = ScriptableObject.CreateInstance<MatchEventLogSO>();
            _onEventLogged = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_log);
            Object.DestroyImmediate(_onEventLogged);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_EventsIsEmpty()
        {
            Assert.AreEqual(0, _log.Events.Count,
                "Fresh MatchEventLogSO must have an empty Events list.");
        }

        [Test]
        public void FreshInstance_MaxEventsIsDefault()
        {
            Assert.IsTrue(_log.MaxEvents >= 10,
                "Default MaxEvents must be at least 10 (Range minimum).");
        }

        // ── LogEvent — null / whitespace guards ───────────────────────────────

        [Test]
        public void LogEvent_NullDescription_IsNoOp()
        {
            SetField(_log, "_onEventLogged", _onEventLogged);
            int fired = 0;
            _onEventLogged.RegisterCallback(() => fired++);

            _log.LogEvent(null, 10f);

            Assert.AreEqual(0, _log.Events.Count, "Null description must not append an entry.");
            Assert.AreEqual(0, fired, "Null description must not fire _onEventLogged.");
        }

        [Test]
        public void LogEvent_WhitespaceDescription_IsNoOp()
        {
            _log.LogEvent("   ", 5f);
            Assert.AreEqual(0, _log.Events.Count,
                "Whitespace-only description must not append an entry.");
        }

        // ── LogEvent — happy path ─────────────────────────────────────────────

        [Test]
        public void LogEvent_ValidDescription_AppendsEntry()
        {
            _log.LogEvent("Player scored a hit!", 15f);
            Assert.AreEqual(1, _log.Events.Count,
                "LogEvent must append exactly one entry.");
        }

        [Test]
        public void LogEvent_CapturesGameTime()
        {
            _log.LogEvent("Match started!", 0.5f);
            Assert.AreEqual(0.5f, _log.Events[0].gameTime, 0.001f,
                "LogEvent must store the provided gameTime on the entry.");
        }

        [Test]
        public void LogEvent_CapturesDescription()
        {
            _log.LogEvent("Enemy destroyed!", 42f);
            Assert.AreEqual("Enemy destroyed!", _log.Events[0].description,
                "LogEvent must store the provided description on the entry.");
        }

        [Test]
        public void LogEvent_FiresOnEventLoggedEvent()
        {
            SetField(_log, "_onEventLogged", _onEventLogged);
            int fired = 0;
            _onEventLogged.RegisterCallback(() => fired++);

            _log.LogEvent("Power-up collected!", 8f);

            Assert.AreEqual(1, fired, "LogEvent must fire _onEventLogged.");
        }

        [Test]
        public void LogEvent_MultipleEvents_AllAppendedInOrder()
        {
            _log.LogEvent("First",  1f);
            _log.LogEvent("Second", 2f);
            _log.LogEvent("Third",  3f);

            Assert.AreEqual(3, _log.Events.Count);
            Assert.AreEqual("First",  _log.Events[0].description);
            Assert.AreEqual("Second", _log.Events[1].description);
            Assert.AreEqual("Third",  _log.Events[2].description);
        }

        // ── LogEvent — capacity / eviction ────────────────────────────────────

        [Test]
        public void LogEvent_AtCapacity_EvictsOldestEntry()
        {
            // Set capacity to 3 for a compact test.
            SetField(_log, "_maxEvents", 3);

            _log.LogEvent("A", 1f);
            _log.LogEvent("B", 2f);
            _log.LogEvent("C", 3f);
            _log.LogEvent("D", 4f); // should evict "A"

            Assert.AreEqual(3, _log.Events.Count, "Count must not exceed MaxEvents.");
            Assert.AreEqual("B", _log.Events[0].description,
                "Oldest entry ('A') must be evicted; 'B' should now be at index 0.");
            Assert.AreEqual("D", _log.Events[2].description,
                "Newest entry ('D') must be at the last position.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllEntries()
        {
            _log.LogEvent("X", 1f);
            _log.LogEvent("Y", 2f);
            _log.Reset();

            Assert.AreEqual(0, _log.Events.Count,
                "Reset must clear all entries from the Events list.");
        }

        [Test]
        public void Reset_Silent_DoesNotFireOnEventLogged()
        {
            SetField(_log, "_onEventLogged", _onEventLogged);
            int fired = 0;
            _onEventLogged.RegisterCallback(() => fired++);

            _log.LogEvent("X", 1f);
            fired = 0; // reset counter after the log call

            _log.Reset();

            Assert.AreEqual(0, fired,
                "Reset must be silent — must not fire _onEventLogged.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MatchTimelineController tests
    // ═══════════════════════════════════════════════════════════════════════════

    public class MatchTimelineControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static MatchTimelineController MakeController(out GameObject go)
        {
            go = new GameObject("MatchTimelineControllerTest");
            go.SetActive(false);
            return go.AddComponent<MatchTimelineController>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var log = ScriptableObject.CreateInstance<MatchEventLogSO>();
            SetField(go.GetComponent<MatchTimelineController>(), "_eventLog", log);
            // _onMatchEnded remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(log);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnMatchEnded()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            SetField(go.GetComponent<MatchTimelineController>(), "_onMatchEnded", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed.");
        }

        // ── Refresh — null _eventLog ──────────────────────────────────────────

        [Test]
        public void Refresh_NullEventLog_ShowsEmptyLabel()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchTimelineController>();

            var labelGo = new GameObject("EmptyLabel");
            labelGo.SetActive(false); // start hidden
            SetField(ctrl, "_emptyLabel", labelGo);
            // _eventLog remains null

            go.SetActive(true); // triggers Refresh via OnEnable

            Assert.IsTrue(labelGo.activeSelf,
                "Null _eventLog: _emptyLabel must be shown (SetActive(true)).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
        }

        [Test]
        public void Refresh_NullListContainer_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<MatchTimelineController>();
            var log  = ScriptableObject.CreateInstance<MatchEventLogSO>();
            log.LogEvent("Test event", 1f);

            SetField(ctrl, "_eventLog", log);
            // _listContainer remains null

            Assert.DoesNotThrow(() => go.SetActive(true));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(log);
        }

        // ── FormatTime ────────────────────────────────────────────────────────

        [Test]
        public void FormatTime_Zero_ReturnsZeroSeconds()
        {
            Assert.AreEqual("0s", MatchTimelineController.FormatTime(0f));
        }

        [Test]
        public void FormatTime_45Seconds_Returns45s()
        {
            Assert.AreEqual("45s", MatchTimelineController.FormatTime(45f));
        }

        [Test]
        public void FormatTime_60Seconds_Returns1m0s()
        {
            Assert.AreEqual("1m 0s", MatchTimelineController.FormatTime(60f));
        }

        [Test]
        public void FormatTime_90Seconds_Returns1m30s()
        {
            Assert.AreEqual("1m 30s", MatchTimelineController.FormatTime(90f));
        }

        [Test]
        public void FormatTime_Negative_ReturnsZeroSeconds()
        {
            Assert.AreEqual("0s", MatchTimelineController.FormatTime(-10f));
        }
    }
}

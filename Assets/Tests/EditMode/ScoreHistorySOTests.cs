using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ScoreHistorySO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance invariants (Scores not null, empty, MaxEntries=20,
    ///     AverageScore=0, TrendDelta=0).
    ///   • Record single score → Count=1, AverageScore=score, TrendDelta=0.
    ///   • Record two scores → chronological order, AverageScore=(a+b)/2, TrendDelta=b-a.
    ///   • Record exceeds capacity → oldest entry evicted from front.
    ///   • Record fires _onHistoryUpdated on each call.
    ///   • Record with null event channel → does not throw.
    ///   • AverageScore when empty returns 0f.
    ///   • TrendDelta with one entry returns 0.
    ///   • TrendDelta with multiple entries returns last-first.
    ///   • LoadSnapshot null input → clears, does not throw.
    ///   • LoadSnapshot valid list → entries stored in order, count capped to MaxEntries.
    ///   • LoadSnapshot keeps tail (most recent) when list longer than MaxEntries.
    ///   • TakeSnapshot returns independent copy in same order.
    ///   • Reset clears scores silently (no event).
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class ScoreHistorySOTests
    {
        private ScoreHistorySO _history;

        // ── Reflection helpers ────────────────────────────────────────────────

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
            _history = ScriptableObject.CreateInstance<ScoreHistorySO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_history);
        }

        // ── Fresh-instance invariants ─────────────────────────────────────────

        [Test]
        public void FreshInstance_Scores_IsNotNull()
        {
            Assert.IsNotNull(_history.Scores);
        }

        [Test]
        public void FreshInstance_Scores_IsEmpty()
        {
            Assert.AreEqual(0, _history.Scores.Count);
        }

        [Test]
        public void FreshInstance_MaxEntries_IsDefaultTwenty()
        {
            Assert.AreEqual(20, _history.MaxEntries);
        }

        [Test]
        public void FreshInstance_AverageScore_IsZero()
        {
            Assert.AreEqual(0f, _history.AverageScore, 0.001f);
        }

        [Test]
        public void FreshInstance_TrendDelta_IsZero()
        {
            Assert.AreEqual(0, _history.TrendDelta);
        }

        // ── Record — single score ─────────────────────────────────────────────

        [Test]
        public void Record_Single_CountIsOne()
        {
            _history.Record(500);
            Assert.AreEqual(1, _history.Scores.Count);
        }

        [Test]
        public void Record_Single_AverageEqualsScore()
        {
            _history.Record(400);
            Assert.AreEqual(400f, _history.AverageScore, 0.001f);
        }

        [Test]
        public void Record_Single_TrendDeltaIsZero()
        {
            _history.Record(300);
            Assert.AreEqual(0, _history.TrendDelta,
                "TrendDelta with one entry must be 0 (need at least two scores).");
        }

        // ── Record — two scores ───────────────────────────────────────────────

        [Test]
        public void Record_Two_StoredInChronologicalOrder()
        {
            _history.Record(100);
            _history.Record(200);

            Assert.AreEqual(100, _history.Scores[0], "Oldest score must be at index 0.");
            Assert.AreEqual(200, _history.Scores[1], "Newest score must be at index 1.");
        }

        [Test]
        public void Record_Two_AverageIsCorrect()
        {
            _history.Record(100);
            _history.Record(300);
            Assert.AreEqual(200f, _history.AverageScore, 0.001f);
        }

        [Test]
        public void Record_Two_TrendDeltaIsLastMinusFirst()
        {
            _history.Record(100);
            _history.Record(350);
            Assert.AreEqual(250, _history.TrendDelta);
        }

        [Test]
        public void Record_Two_NegativeTrendDeltaWhenDeclining()
        {
            _history.Record(500);
            _history.Record(200);
            Assert.AreEqual(-300, _history.TrendDelta);
        }

        // ── Record — capacity enforcement ─────────────────────────────────────

        [Test]
        public void Record_ExceedsCapacity_EvictsOldestEntry()
        {
            SetField(_history, "_maxEntries", 5);

            for (int i = 1; i <= 5; i++)
                _history.Record(i * 10); // 10, 20, 30, 40, 50

            Assert.AreEqual(5, _history.Scores.Count, "Board should be full at 5 entries.");
            Assert.AreEqual(10, _history.Scores[0], "Oldest entry before overflow is 10.");

            _history.Record(60); // should evict 10

            Assert.AreEqual(5, _history.Scores.Count, "Count must not exceed MaxEntries.");
            Assert.AreEqual(20, _history.Scores[0], "After eviction, oldest should now be 20.");
            Assert.AreEqual(60, _history.Scores[4], "Newest score (60) at last index.");
        }

        // ── Event firing ──────────────────────────────────────────────────────

        [Test]
        public void Record_FiresOnHistoryUpdated()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_history, "_onHistoryUpdated", channel);

            int fireCount = 0;
            channel.RegisterCallback(() => fireCount++);

            _history.Record(100);
            _history.Record(200);

            Object.DestroyImmediate(channel);
            Assert.AreEqual(2, fireCount, "Event must fire once per Record() call.");
        }

        [Test]
        public void Record_NullEventChannel_DoesNotThrow()
        {
            // _onHistoryUpdated is null by default — just confirm no NullReferenceException.
            Assert.DoesNotThrow(() => _history.Record(999));
        }

        // ── AverageScore edge cases ───────────────────────────────────────────

        [Test]
        public void AverageScore_EmptyHistory_ReturnsZero()
        {
            Assert.AreEqual(0f, _history.AverageScore, 0.001f);
        }

        // ── TrendDelta edge cases ─────────────────────────────────────────────

        [Test]
        public void TrendDelta_OneEntry_ReturnsZero()
        {
            _history.Record(700);
            Assert.AreEqual(0, _history.TrendDelta);
        }

        [Test]
        public void TrendDelta_MultipleEntries_ReturnsLastMinusFirst()
        {
            _history.Record(100);
            _history.Record(200);
            _history.Record(150);
            // First=100, Last=150 → delta=50
            Assert.AreEqual(50, _history.TrendDelta);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_NullInput_ClearsScores_DoesNotThrow()
        {
            _history.Record(500);

            Assert.DoesNotThrow(() => _history.LoadSnapshot(null));
            Assert.AreEqual(0, _history.Scores.Count, "Null snapshot must clear all scores.");
        }

        [Test]
        public void LoadSnapshot_ValidList_StoresInOrder()
        {
            var list = new List<int> { 100, 200, 300 };
            _history.LoadSnapshot(list);

            Assert.AreEqual(3, _history.Scores.Count);
            Assert.AreEqual(100, _history.Scores[0]);
            Assert.AreEqual(200, _history.Scores[1]);
            Assert.AreEqual(300, _history.Scores[2]);
        }

        [Test]
        public void LoadSnapshot_TruncatesToMaxEntries_KeepsTailEntries()
        {
            SetField(_history, "_maxEntries", 3);

            // List with 5 entries; only the last 3 (most recent) should be retained.
            var list = new List<int> { 10, 20, 30, 40, 50 };
            _history.LoadSnapshot(list);

            Assert.AreEqual(3, _history.Scores.Count,
                "LoadSnapshot must retain only the most recent MaxEntries scores.");
            Assert.AreEqual(30, _history.Scores[0], "Tail start should be 30.");
            Assert.AreEqual(40, _history.Scores[1]);
            Assert.AreEqual(50, _history.Scores[2], "Most recent score should be 50.");
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_ReturnsIndependentCopy()
        {
            _history.Record(100);
            _history.Record(200);

            List<int> snapshot = _history.TakeSnapshot();
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(2, snapshot.Count);
            Assert.AreEqual(100, snapshot[0]);
            Assert.AreEqual(200, snapshot[1]);

            // Mutate snapshot — must not affect internal list.
            snapshot[0] = 9999;
            Assert.AreNotEqual(9999, _history.Scores[0],
                "TakeSnapshot must return an independent copy — mutation must not propagate.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsScoresSilently()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_history, "_onHistoryUpdated", channel);

            int fireCount = 0;
            channel.RegisterCallback(() => fireCount++);

            _history.Record(100); // fires event (count=1)
            _history.Reset();     // must NOT fire event

            Object.DestroyImmediate(channel);

            Assert.AreEqual(0, _history.Scores.Count, "Reset must clear all scores.");
            Assert.AreEqual(1, fireCount, "Reset must not fire _onHistoryUpdated.");
        }
    }
}

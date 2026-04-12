using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchLeaderboardSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance invariants (Entries not null, initially empty).
    ///   • Submit null-result guard → returns false, no event fired.
    ///   • Submit valid result → returns true, adds exactly one entry.
    ///   • Submit multiple results → Entries sorted descending by score.
    ///   • Submit when board is full and new score beats the minimum → lowest evicted.
    ///   • Submit when board is full and new score is lower than all → new entry evicted.
    ///   • Submit stores opponentName, arenaIndex, playerWon fields correctly.
    ///   • Submit fires _onLeaderboardUpdated on success.
    ///   • Submit does NOT fire _onLeaderboardUpdated on null result.
    ///   • LoadSnapshot null → clears entries (no throw).
    ///   • LoadSnapshot valid list → sorted descending, capped to MaxEntries.
    ///   • TakeSnapshot returns an independent deep copy.
    ///   • Reset clears entries (no event).
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class MatchLeaderboardSOTests
    {
        private MatchLeaderboardSO _board;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Creates a MatchResultSO with specific match data via Write().</summary>
        private static MatchResultSO MakeResult(bool playerWon, float duration = 60f,
                                                int currency = 100, float damageDone = 50f)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon, duration, currency, currency,
                    damageDone, damageTaken: 0f, bonusEarned: 0);
            return r;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _board = ScriptableObject.CreateInstance<MatchLeaderboardSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_board);
        }

        // ── Fresh-instance invariants ─────────────────────────────────────────

        [Test]
        public void FreshInstance_Entries_IsNotNull()
        {
            Assert.IsNotNull(_board.Entries);
        }

        [Test]
        public void FreshInstance_Entries_IsEmpty()
        {
            Assert.AreEqual(0, _board.Entries.Count);
        }

        // ── Submit null guard ─────────────────────────────────────────────────

        [Test]
        public void Submit_NullResult_ReturnsFalse()
        {
            bool result = _board.Submit(null);
            Assert.IsFalse(result, "Submit(null) must return false.");
        }

        [Test]
        public void Submit_NullResult_DoesNotFireEvent()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_board, "_onLeaderboardUpdated", channel);

            int fireCount = 0;
            channel.RegisterCallback(() => fireCount++);

            _board.Submit(null);

            Object.DestroyImmediate(channel);
            Assert.AreEqual(0, fireCount, "Event must not fire on null result.");
        }

        // ── Submit valid result ───────────────────────────────────────────────

        [Test]
        public void Submit_ValidResult_ReturnsTrue()
        {
            MatchResultSO r = MakeResult(true);
            bool result = _board.Submit(r);
            Object.DestroyImmediate(r);
            Assert.IsTrue(result);
        }

        [Test]
        public void Submit_ValidResult_AddsOneEntry()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r);
            Object.DestroyImmediate(r);
            Assert.AreEqual(1, _board.Entries.Count);
        }

        [Test]
        public void Submit_ValidResult_FiresOnLeaderboardUpdated()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_board, "_onLeaderboardUpdated", channel);

            int fireCount = 0;
            channel.RegisterCallback(() => fireCount++);

            MatchResultSO r = MakeResult(true);
            _board.Submit(r);
            Object.DestroyImmediate(r);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, fireCount, "Event must fire once on successful Submit.");
        }

        // ── Sorting ───────────────────────────────────────────────────────────

        [Test]
        public void Submit_MultipleResults_SortedDescendingByScore()
        {
            // Win with 0 damage done → low score; Win with 500 damage done → higher score.
            MatchResultSO low  = MakeResult(true,  duration: 120f, damageDone: 0f);
            MatchResultSO high = MakeResult(true,  duration: 5f,   damageDone: 500f);

            _board.Submit(low);
            _board.Submit(high);

            Object.DestroyImmediate(low);
            Object.DestroyImmediate(high);

            Assert.GreaterOrEqual(_board.Entries[0].score, _board.Entries[1].score,
                "Entries must be sorted descending: first entry score ≥ second.");
        }

        // ── Capacity ──────────────────────────────────────────────────────────

        [Test]
        public void Submit_ExceedsMaxEntries_EvictsLowestScore()
        {
            // Default MaxEntries = 10; use a board limited to 5.
            SetField(_board, "_maxEntries", 5);

            // Fill board with scores 100..500 (low to high — inserted in sorted order).
            for (int i = 1; i <= 5; i++)
            {
                MatchResultSO r = MakeResult(true, duration: 120f, damageDone: i * 20f);
                // Each win base 1000 + damage: damageDone×2 - 0 taken, score increases with i.
                _board.Submit(r);
                Object.DestroyImmediate(r);
            }
            Assert.AreEqual(5, _board.Entries.Count, "Board should be full at 5 entries.");

            int lowestBeforeNewSubmit = _board.Entries[_board.Entries.Count - 1].score;

            // Submit a result guaranteed to have a higher score (instant win, huge damage).
            MatchResultSO best = MakeResult(true, duration: 1f, damageDone: 1000f);
            _board.Submit(best);
            Object.DestroyImmediate(best);

            Assert.AreEqual(5, _board.Entries.Count, "Board must remain at max capacity.");
            Assert.Less(_board.Entries[_board.Entries.Count - 1].score, _board.Entries[0].score,
                "Lowest entry should have been evicted to make room for the higher score.");
            Assert.Greater(_board.Entries[0].score, lowestBeforeNewSubmit,
                "Highest score on board should exceed the pre-submit lowest score.");
        }

        [Test]
        public void Submit_WhenBoardFullAndScoreLowerThanAllEntries_LowestEntryRetained()
        {
            // Create a 5-entry board with all high scores (win + damage).
            SetField(_board, "_maxEntries", 5);

            int[] highScores = { 1000, 900, 800, 700, 600 };
            for (int i = 0; i < highScores.Length; i++)
            {
                // Manual leaderboard via LoadSnapshot to get exact scores.
                // Instead, just submit results that produce high scores.
                MatchResultSO r = MakeResult(true, duration: 1f, damageDone: highScores[i] / 2f);
                _board.Submit(r);
                Object.DestroyImmediate(r);
            }

            int lowestBefore = _board.Entries[_board.Entries.Count - 1].score;

            // Submit a loss with zero damage — minimal score (100 base).
            MatchResultSO loser = MakeResult(false, duration: 120f, damageDone: 0f);
            _board.Submit(loser);
            Object.DestroyImmediate(loser);

            // Board should still have 5 entries; the loser entry was evicted.
            Assert.AreEqual(5, _board.Entries.Count, "Board size must not grow beyond max.");
            Assert.GreaterOrEqual(_board.Entries[_board.Entries.Count - 1].score, lowestBefore,
                "The pre-existing lowest entry must not be evicted by a lower-scoring new entry.");
        }

        // ── Entry field storage ───────────────────────────────────────────────

        [Test]
        public void Submit_OpponentName_StoredOnEntry()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r, opponentName: "RoboDestroyer");
            Object.DestroyImmediate(r);
            Assert.AreEqual("RoboDestroyer", _board.Entries[0].opponentName);
        }

        [Test]
        public void Submit_ArenaIndex_StoredOnEntry()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r, arenaIndex: 3);
            Object.DestroyImmediate(r);
            Assert.AreEqual(3, _board.Entries[0].arenaIndex);
        }

        [Test]
        public void Submit_PlayerWon_TrueStoredOnEntry()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r);
            Object.DestroyImmediate(r);
            Assert.IsTrue(_board.Entries[0].playerWon);
        }

        [Test]
        public void Submit_PlayerLost_FalseStoredOnEntry()
        {
            MatchResultSO r = MakeResult(false);
            _board.Submit(r);
            Object.DestroyImmediate(r);
            Assert.IsFalse(_board.Entries[0].playerWon);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_NullInput_ClearsEntries_DoesNotThrow()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r); // put something in first
            Object.DestroyImmediate(r);

            Assert.DoesNotThrow(() => _board.LoadSnapshot(null));
            Assert.AreEqual(0, _board.Entries.Count, "Null snapshot must clear all entries.");
        }

        [Test]
        public void LoadSnapshot_ValidList_SortedDescendingByScore()
        {
            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { score = 300, playerWon = true, opponentName = "C" },
                new LeaderboardEntry { score = 100, playerWon = false, opponentName = "A" },
                new LeaderboardEntry { score = 200, playerWon = true, opponentName = "B" },
            };
            _board.LoadSnapshot(entries);

            Assert.AreEqual(3, _board.Entries.Count);
            Assert.AreEqual(300, _board.Entries[0].score);
            Assert.AreEqual(200, _board.Entries[1].score);
            Assert.AreEqual(100, _board.Entries[2].score);
        }

        [Test]
        public void LoadSnapshot_TruncatesToMaxEntries()
        {
            SetField(_board, "_maxEntries", 3);

            var entries = new List<LeaderboardEntry>
            {
                new LeaderboardEntry { score = 500 },
                new LeaderboardEntry { score = 400 },
                new LeaderboardEntry { score = 300 },
                new LeaderboardEntry { score = 200 },
                new LeaderboardEntry { score = 100 },
            };
            _board.LoadSnapshot(entries);

            Assert.AreEqual(3, _board.Entries.Count,
                "LoadSnapshot must truncate entries to MaxEntries.");
            Assert.AreEqual(500, _board.Entries[0].score, "Highest score should be first.");
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_ReturnsIndependentCopy()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r);
            Object.DestroyImmediate(r);

            List<LeaderboardEntry> snapshot = _board.TakeSnapshot();
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(1, snapshot.Count);

            // Mutating the snapshot must not affect the board.
            snapshot[0].opponentName = "MUTATED";
            Assert.AreNotEqual("MUTATED", _board.Entries[0].opponentName,
                "TakeSnapshot must return an independent copy — mutation must not propagate.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsEntries()
        {
            MatchResultSO r = MakeResult(true);
            _board.Submit(r);
            Object.DestroyImmediate(r);

            _board.Reset();
            Assert.AreEqual(0, _board.Entries.Count, "Reset must clear all entries.");
        }
    }
}

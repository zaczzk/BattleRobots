using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T048 — Room history (recently-visited rooms).
    ///
    /// Coverage (15 cases):
    ///
    /// RecentRoomsSO — default state
    ///   [01] DefaultState_Count_IsZero
    ///   [02] DefaultState_Recent_IsEmpty
    ///   [03] MaxCapacity_IsTen
    ///
    /// RecentRoomsSO — RecordVisit
    ///   [04] RecordVisit_SingleCode_CountIsOne
    ///   [05] RecordVisit_SingleCode_IsAtFront
    ///   [06] RecordVisit_DuplicateCode_MovesToFront
    ///   [07] RecordVisit_DuplicateCode_DoesNotIncreaseCount
    ///   [08] RecordVisit_NullCode_DoesNotThrow
    ///   [09] RecordVisit_EmptyCode_DoesNotThrow
    ///   [10] RecordVisit_ExceedsCapacity_OldestDropped
    ///
    /// RecentRoomsSO — Clear
    ///   [11] Clear_AfterVisits_CountIsZero
    ///   [12] Clear_OnEmpty_DoesNotThrow
    ///
    /// RecentRoomsSO — LoadFromData / BuildData
    ///   [13] LoadFromData_PopulatesRecent_NewestFirst
    ///   [14] LoadFromData_Null_EmptiesList
    ///   [15] LoadFromData_TrimsToMaxCapacity
    ///   [16] BuildData_RoundTripThroughLoadFromData
    ///   [17] LoadFromData_SkipsNullAndEmptyEntries
    ///
    /// SaveData — field presence
    ///   [18] SaveData_RecentRoomCodes_DefaultIsEmptyList
    /// </summary>
    [TestFixture]
    public sealed class RecentRoomsTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private RecentRoomsSO _recent;

        [SetUp]
        public void SetUp()
        {
            _recent = ScriptableObject.CreateInstance<RecentRoomsSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_recent);
        }

        // ── [01] Default state — Count ────────────────────────────────────────

        [Test]
        public void DefaultState_Count_IsZero()
        {
            Assert.AreEqual(0, _recent.Count);
        }

        // ── [02] Default state — Recent list ──────────────────────────────────

        [Test]
        public void DefaultState_Recent_IsEmpty()
        {
            Assert.IsNotNull(_recent.Recent);
            Assert.AreEqual(0, _recent.Recent.Count);
        }

        // ── [03] MaxCapacity constant ─────────────────────────────────────────

        [Test]
        public void MaxCapacity_IsTen()
        {
            Assert.AreEqual(10, RecentRoomsSO.MaxCapacity);
        }

        // ── [04] RecordVisit — single code, count is 1 ────────────────────────

        [Test]
        public void RecordVisit_SingleCode_CountIsOne()
        {
            _recent.RecordVisit("ABCD");
            Assert.AreEqual(1, _recent.Count);
        }

        // ── [05] RecordVisit — single code appears at front ───────────────────

        [Test]
        public void RecordVisit_SingleCode_IsAtFront()
        {
            _recent.RecordVisit("ABCD");
            Assert.AreEqual("ABCD", _recent.Recent[0]);
        }

        // ── [06] RecordVisit — duplicate moves existing entry to front ─────────

        [Test]
        public void RecordVisit_DuplicateCode_MovesToFront()
        {
            _recent.RecordVisit("AAAA");
            _recent.RecordVisit("BBBB");
            _recent.RecordVisit("AAAA"); // should become [0] again

            Assert.AreEqual("AAAA", _recent.Recent[0]);
            Assert.AreEqual("BBBB", _recent.Recent[1]);
        }

        // ── [07] RecordVisit — duplicate does not increase count ──────────────

        [Test]
        public void RecordVisit_DuplicateCode_DoesNotIncreaseCount()
        {
            _recent.RecordVisit("AAAA");
            _recent.RecordVisit("AAAA");
            Assert.AreEqual(1, _recent.Count);
        }

        // ── [08] RecordVisit — null code ──────────────────────────────────────

        [Test]
        public void RecordVisit_NullCode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _recent.RecordVisit(null));
            Assert.AreEqual(0, _recent.Count);
        }

        // ── [09] RecordVisit — empty code ─────────────────────────────────────

        [Test]
        public void RecordVisit_EmptyCode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _recent.RecordVisit(string.Empty));
            Assert.AreEqual(0, _recent.Count);
        }

        // ── [10] RecordVisit — exceeding capacity drops oldest entry ──────────

        [Test]
        public void RecordVisit_ExceedsCapacity_OldestDropped()
        {
            // Fill to capacity + 1
            for (int i = 0; i <= RecentRoomsSO.MaxCapacity; i++)
                _recent.RecordVisit($"R{i:D2}");

            // Should be capped at MaxCapacity
            Assert.AreEqual(RecentRoomsSO.MaxCapacity, _recent.Count);

            // The newest entry should be at the front
            Assert.AreEqual($"R{RecentRoomsSO.MaxCapacity:D2}", _recent.Recent[0]);

            // The very first code visited ("R00") should have been evicted
            bool foundFirst = false;
            foreach (string code in _recent.Recent)
            {
                if (code == "R00") { foundFirst = true; break; }
            }
            Assert.IsFalse(foundFirst, "Oldest code was not evicted when capacity exceeded.");
        }

        // ── [11] Clear — empties the list ────────────────────────────────────

        [Test]
        public void Clear_AfterVisits_CountIsZero()
        {
            _recent.RecordVisit("AAAA");
            _recent.RecordVisit("BBBB");
            _recent.Clear();
            Assert.AreEqual(0, _recent.Count);
        }

        // ── [12] Clear — on empty list is safe ───────────────────────────────

        [Test]
        public void Clear_OnEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _recent.Clear());
        }

        // ── [13] LoadFromData — populates list in provided order ──────────────

        [Test]
        public void LoadFromData_PopulatesRecent_NewestFirst()
        {
            var codes = new List<string> { "NEWEST", "MIDDLE", "OLDEST" };
            _recent.LoadFromData(codes);

            Assert.AreEqual(3, _recent.Count);
            Assert.AreEqual("NEWEST", _recent.Recent[0]);
            Assert.AreEqual("MIDDLE", _recent.Recent[1]);
            Assert.AreEqual("OLDEST", _recent.Recent[2]);
        }

        // ── [14] LoadFromData — null input empties list ────────────────────────

        [Test]
        public void LoadFromData_Null_EmptiesList()
        {
            _recent.RecordVisit("AAAA");
            _recent.LoadFromData(null);
            Assert.AreEqual(0, _recent.Count);
        }

        // ── [15] LoadFromData — trims to MaxCapacity ──────────────────────────

        [Test]
        public void LoadFromData_TrimsToMaxCapacity()
        {
            var codes = new List<string>();
            for (int i = 0; i < RecentRoomsSO.MaxCapacity + 5; i++)
                codes.Add($"R{i:D2}");

            _recent.LoadFromData(codes);
            Assert.AreEqual(RecentRoomsSO.MaxCapacity, _recent.Count);
        }

        // ── [16] BuildData / LoadFromData round-trip ──────────────────────────

        [Test]
        public void BuildData_RoundTripThroughLoadFromData()
        {
            _recent.RecordVisit("ROOM");
            _recent.RecordVisit("TEST");

            List<string> snapshot = _recent.BuildData();

            var second = ScriptableObject.CreateInstance<RecentRoomsSO>();
            try
            {
                second.LoadFromData(snapshot);

                Assert.AreEqual(2, second.Count);
                // RecordVisit("ROOM") then RecordVisit("TEST") — TEST is newest-first.
                Assert.AreEqual("TEST", second.Recent[0]);
                Assert.AreEqual("ROOM", second.Recent[1]);
            }
            finally
            {
                Object.DestroyImmediate(second);
            }
        }

        // ── [17] LoadFromData — null/empty entries are skipped ────────────────

        [Test]
        public void LoadFromData_SkipsNullAndEmptyEntries()
        {
            var codes = new List<string> { null, string.Empty, "CCCC", null };
            _recent.LoadFromData(codes);

            Assert.AreEqual(1, _recent.Count);
            Assert.AreEqual("CCCC", _recent.Recent[0]);
        }

        // ── [18] SaveData — field exists and defaults to empty list ───────────

        [Test]
        public void SaveData_RecentRoomCodes_DefaultIsEmptyList()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.recentRoomCodes);
            Assert.AreEqual(0, save.recentRoomCodes.Count);
        }
    }
}

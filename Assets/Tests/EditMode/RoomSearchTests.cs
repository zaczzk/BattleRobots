using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T049 — Room search / filter by code prefix.
    ///
    /// Coverage (10 cases):
    ///
    /// RoomListSO.GetFilteredRooms — null / empty prefix
    ///   [01] NullPrefix_ReturnsAllRooms
    ///   [02] EmptyPrefix_ReturnsAllRooms
    ///
    /// RoomListSO.GetFilteredRooms — prefix matching
    ///   [03] MatchingPrefix_ReturnsOnlyMatchingRooms
    ///   [04] SingleCharPrefix_FiltersCorrectly
    ///   [05] FullCodePrefix_ReturnsSingleRoom
    ///   [06] NoMatch_ReturnsEmptyList
    ///   [07] PrefixIsCaseInsensitive_LowerInput
    ///   [08] PrefixIsCaseInsensitive_UpperInput
    ///
    /// RoomListSO.GetFilteredRooms — edge cases
    ///   [09] EmptyRoomList_ReturnsEmpty_ForAnyPrefix
    ///   [10] RoomsWithNullCode_AreSkipped_NotThrown
    /// </summary>
    [TestFixture]
    public sealed class RoomSearchTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private RoomListSO _roomList;

        [SetUp]
        public void SetUp()
        {
            _roomList = ScriptableObject.CreateInstance<RoomListSO>();

            // Populate with a representative set of room codes.
            _roomList.SetRooms(new List<RoomEntry>
            {
                new RoomEntry("ABCD", 1),
                new RoomEntry("ABEF", 2),
                new RoomEntry("CDAB", 1),
                new RoomEntry("XYZW", 2),
                new RoomEntry("XY12", 1),
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_roomList);
        }

        // ── [01] Null prefix → all rooms ─────────────────────────────────────

        [Test]
        public void NullPrefix_ReturnsAllRooms()
        {
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms(null);

            Assert.AreEqual(5, result.Count);
        }

        // ── [02] Empty prefix → all rooms ────────────────────────────────────

        [Test]
        public void EmptyPrefix_ReturnsAllRooms()
        {
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms(string.Empty);

            Assert.AreEqual(5, result.Count);
        }

        // ── [03] Matching prefix → only matching rooms ────────────────────────

        [Test]
        public void MatchingPrefix_ReturnsOnlyMatchingRooms()
        {
            // "AB" matches "ABCD" and "ABEF" but not "CDAB", "XYZW", "XY12".
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("AB");

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("ABCD", result[0].roomCode);
            Assert.AreEqual("ABEF", result[1].roomCode);
        }

        // ── [04] Single-char prefix ───────────────────────────────────────────

        [Test]
        public void SingleCharPrefix_FiltersCorrectly()
        {
            // "X" matches "XYZW" and "XY12".
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("X");

            Assert.AreEqual(2, result.Count);
        }

        // ── [05] Full 4-char code as prefix → exactly one match ──────────────

        [Test]
        public void FullCodePrefix_ReturnsSingleRoom()
        {
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("CDAB");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("CDAB", result[0].roomCode);
        }

        // ── [06] No match → empty list ────────────────────────────────────────

        [Test]
        public void NoMatch_ReturnsEmptyList()
        {
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("ZZ");

            Assert.AreEqual(0, result.Count);
        }

        // ── [07] Case insensitive — lower input matches upper codes ───────────

        [Test]
        public void PrefixIsCaseInsensitive_LowerInput()
        {
            // Lowercase "ab" should match "ABCD" and "ABEF".
            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("ab");

            Assert.AreEqual(2, result.Count);
        }

        // ── [08] Case insensitive — upper input matches lower codes ───────────

        [Test]
        public void PrefixIsCaseInsensitive_UpperInput()
        {
            // The rooms in this test have uppercase codes; verify the comparison is
            // symmetric by temporarily adding a mixed-case entry.
            _roomList.SetRooms(new List<RoomEntry>
            {
                new RoomEntry("ab12", 1),
                new RoomEntry("AB34", 2),
                new RoomEntry("XY56", 1),
            });

            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("AB");

            // Both "ab12" and "AB34" start with the same two letters regardless of case.
            Assert.AreEqual(2, result.Count);
        }

        // ── [09] Empty room list → always empty ───────────────────────────────

        [Test]
        public void EmptyRoomList_ReturnsEmpty_ForAnyPrefix()
        {
            _roomList.Clear();

            IReadOnlyList<RoomEntry> result = _roomList.GetFilteredRooms("AB");

            Assert.AreEqual(0, result.Count);
        }

        // ── [10] Rooms with null room code are skipped without throwing ────────

        [Test]
        public void RoomsWithNullCode_AreSkipped_NotThrown()
        {
            // RoomEntry constructor guards against null, but verify via manual struct.
            _roomList.SetRooms(new List<RoomEntry>
            {
                new RoomEntry(null, 1),   // constructor maps null → string.Empty
                new RoomEntry("ABCD", 2),
            });

            IReadOnlyList<RoomEntry> result = null;
            Assert.DoesNotThrow(() => result = _roomList.GetFilteredRooms("AB"));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("ABCD", result[0].roomCode);
        }
    }
}

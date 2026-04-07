using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T050 — Room sort order.
    ///
    /// Coverage (10 cases):
    ///
    /// RoomSortMode.None (no sort)
    ///   [01] NoneSort_NullPrefix_ReturnsAllRoomsInOriginalOrder
    ///   [02] NoneSort_EmptyPrefix_ReturnsAllRooms
    ///
    /// RoomSortMode.ByPlayerCountDesc
    ///   [03] ByPlayerCountDesc_SortsFromHighestToLowest
    ///   [04] ByPlayerCountDesc_NullPrefix_SortsAllRooms
    ///   [05] ByPlayerCountDesc_WithPrefix_FiltersThenSorts
    ///
    /// RoomSortMode.ByRoomCodeAsc
    ///   [06] ByRoomCodeAsc_SortsAlphabetically
    ///   [07] ByRoomCodeAsc_IsCaseInsensitive
    ///   [08] ByRoomCodeAsc_WithPrefix_FiltersThenSorts
    ///
    /// Edge cases
    ///   [09] EmptyList_AnySort_ReturnsEmpty
    ///   [10] NoMatchPrefix_AnySort_ReturnsEmpty
    /// </summary>
    [TestFixture]
    public sealed class RoomSortTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private RoomListSO _roomList;

        [SetUp]
        public void SetUp()
        {
            _roomList = ScriptableObject.CreateInstance<RoomListSO>();

            // Rooms ordered as received: mixed player counts, mixed codes.
            _roomList.SetRooms(new List<RoomEntry>
            {
                new RoomEntry("CDAB", 1),
                new RoomEntry("ABEF", 3),
                new RoomEntry("XYZW", 2),
                new RoomEntry("ABCD", 3),
                new RoomEntry("XY12", 1),
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_roomList);
        }

        // ── [01] None sort, null prefix → original order ──────────────────────

        [Test]
        public void NoneSort_NullPrefix_ReturnsAllRoomsInOriginalOrder()
        {
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms(null, RoomSortMode.None);

            Assert.AreEqual(5, result.Count);
            // Original order preserved.
            Assert.AreEqual("CDAB", result[0].roomCode);
            Assert.AreEqual("ABEF", result[1].roomCode);
            Assert.AreEqual("XYZW", result[2].roomCode);
            Assert.AreEqual("ABCD", result[3].roomCode);
            Assert.AreEqual("XY12", result[4].roomCode);
        }

        // ── [02] None sort, empty prefix → all rooms ──────────────────────────

        [Test]
        public void NoneSort_EmptyPrefix_ReturnsAllRooms()
        {
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms(string.Empty, RoomSortMode.None);

            Assert.AreEqual(5, result.Count);
        }

        // ── [03] ByPlayerCountDesc → highest first ────────────────────────────

        [Test]
        public void ByPlayerCountDesc_SortsFromHighestToLowest()
        {
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms(null, RoomSortMode.ByPlayerCountDesc);

            Assert.AreEqual(5, result.Count);

            // First two should have count 3; last two should have count 1.
            Assert.AreEqual(3, result[0].playerCount);
            Assert.AreEqual(3, result[1].playerCount);
            Assert.AreEqual(2, result[2].playerCount);
            Assert.AreEqual(1, result[3].playerCount);
            Assert.AreEqual(1, result[4].playerCount);
        }

        // ── [04] ByPlayerCountDesc, null prefix → sorts all rooms ────────────

        [Test]
        public void ByPlayerCountDesc_NullPrefix_SortsAllRooms()
        {
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms(null, RoomSortMode.ByPlayerCountDesc);

            // Verify all five rooms are returned and the first has the maximum count.
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(3, result[0].playerCount, "First entry should have max player count.");
        }

        // ── [05] ByPlayerCountDesc with prefix → filter first, then sort ──────

        [Test]
        public void ByPlayerCountDesc_WithPrefix_FiltersThenSorts()
        {
            // "AB" matches "ABEF" (3 players) and "ABCD" (3 players).
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms("AB", RoomSortMode.ByPlayerCountDesc);

            Assert.AreEqual(2, result.Count);
            // Both have 3 players; verify neither is from the XY or CD group.
            Assert.IsTrue(result[0].roomCode.StartsWith("AB", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result[1].roomCode.StartsWith("AB", System.StringComparison.OrdinalIgnoreCase));
        }

        // ── [06] ByRoomCodeAsc → alphabetical order ───────────────────────────

        [Test]
        public void ByRoomCodeAsc_SortsAlphabetically()
        {
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms(null, RoomSortMode.ByRoomCodeAsc);

            Assert.AreEqual(5, result.Count);
            // Expected: ABCD, ABEF, CDAB, XY12, XYZW
            Assert.AreEqual("ABCD", result[0].roomCode);
            Assert.AreEqual("ABEF", result[1].roomCode);
            Assert.AreEqual("CDAB", result[2].roomCode);
            Assert.AreEqual("XY12", result[3].roomCode);
            Assert.AreEqual("XYZW", result[4].roomCode);
        }

        // ── [07] ByRoomCodeAsc is case-insensitive ────────────────────────────

        [Test]
        public void ByRoomCodeAsc_IsCaseInsensitive()
        {
            _roomList.SetRooms(new List<RoomEntry>
            {
                new RoomEntry("zz99", 1),
                new RoomEntry("AA11", 1),
                new RoomEntry("mm55", 1),
            });

            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms(null, RoomSortMode.ByRoomCodeAsc);

            // Case-insensitive: AA < mm < zz
            Assert.AreEqual("AA11", result[0].roomCode);
            Assert.AreEqual("mm55", result[1].roomCode);
            Assert.AreEqual("zz99", result[2].roomCode);
        }

        // ── [08] ByRoomCodeAsc with prefix → filter first, then sort alphabetically

        [Test]
        public void ByRoomCodeAsc_WithPrefix_FiltersThenSorts()
        {
            // "X" matches "XYZW" and "XY12".
            IReadOnlyList<RoomEntry> result =
                _roomList.GetSortedFilteredRooms("X", RoomSortMode.ByRoomCodeAsc);

            Assert.AreEqual(2, result.Count);
            // XY12 < XYZW alphabetically.
            Assert.AreEqual("XY12", result[0].roomCode);
            Assert.AreEqual("XYZW", result[1].roomCode);
        }

        // ── [09] Empty room list → always empty regardless of sort mode ───────

        [Test]
        public void EmptyList_AnySort_ReturnsEmpty()
        {
            _roomList.Clear();

            Assert.AreEqual(0, _roomList.GetSortedFilteredRooms(null,   RoomSortMode.None).Count);
            Assert.AreEqual(0, _roomList.GetSortedFilteredRooms("AB",   RoomSortMode.ByPlayerCountDesc).Count);
            Assert.AreEqual(0, _roomList.GetSortedFilteredRooms(string.Empty, RoomSortMode.ByRoomCodeAsc).Count);
        }

        // ── [10] No prefix match → empty result for any sort mode ─────────────

        [Test]
        public void NoMatchPrefix_AnySort_ReturnsEmpty()
        {
            // "ZZ" matches none of CDAB, ABEF, XYZW, ABCD, XY12.
            Assert.AreEqual(0, _roomList.GetSortedFilteredRooms("ZZ", RoomSortMode.None).Count);
            Assert.AreEqual(0, _roomList.GetSortedFilteredRooms("ZZ", RoomSortMode.ByPlayerCountDesc).Count);
            Assert.AreEqual(0, _roomList.GetSortedFilteredRooms("ZZ", RoomSortMode.ByRoomCodeAsc).Count);
        }
    }
}

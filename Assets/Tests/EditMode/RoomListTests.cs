using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T042 — Network room-list browser.
    ///
    /// Coverage (12 cases):
    ///
    /// RoomListSO — initial state
    ///   [01] DefaultState_Count_IsZero
    ///   [02] DefaultState_Rooms_IsEmpty
    ///
    /// RoomListSO — SetRooms
    ///   [03] SetRooms_WithEntries_UpdatesCount
    ///   [04] SetRooms_StoresRoomCode
    ///   [05] SetRooms_StoresPlayerCount
    ///   [06] SetRooms_ReplacesExistingEntries
    ///   [07] SetRooms_WithNull_ClearsList
    ///   [08] SetRooms_WithEmptyList_CountIsZero
    ///
    /// RoomListSO — Clear
    ///   [09] Clear_AfterSetRooms_CountIsZero
    ///   [10] Clear_OnEmptyList_DoesNotThrow
    ///
    /// RoomEntry — struct contract
    ///   [11] RoomEntry_Constructor_SetsRoomCode
    ///   [12] RoomEntry_Constructor_SetsPlayerCount
    /// </summary>
    [TestFixture]
    public sealed class RoomListTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private RoomListSO _roomList;

        [SetUp]
        public void SetUp()
        {
            _roomList = ScriptableObject.CreateInstance<RoomListSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_roomList);
        }

        // ── [01] Default state — Count ────────────────────────────────────────

        [Test]
        public void DefaultState_Count_IsZero()
        {
            Assert.AreEqual(0, _roomList.Count);
        }

        // ── [02] Default state — Rooms ────────────────────────────────────────

        [Test]
        public void DefaultState_Rooms_IsEmpty()
        {
            Assert.IsNotNull(_roomList.Rooms);
            Assert.AreEqual(0, _roomList.Rooms.Count);
        }

        // ── [03] SetRooms — Count updates ────────────────────────────────────

        [Test]
        public void SetRooms_WithEntries_UpdatesCount()
        {
            var rooms = new List<RoomEntry>
            {
                new RoomEntry("ABCD", 1),
                new RoomEntry("EFGH", 2),
            };

            _roomList.SetRooms(rooms);

            Assert.AreEqual(2, _roomList.Count);
        }

        // ── [04] SetRooms — room code stored ─────────────────────────────────

        [Test]
        public void SetRooms_StoresRoomCode()
        {
            var rooms = new List<RoomEntry> { new RoomEntry("XYZW", 1) };
            _roomList.SetRooms(rooms);

            Assert.AreEqual("XYZW", _roomList.Rooms[0].roomCode);
        }

        // ── [05] SetRooms — player count stored ──────────────────────────────

        [Test]
        public void SetRooms_StoresPlayerCount()
        {
            var rooms = new List<RoomEntry> { new RoomEntry("AAAA", 2) };
            _roomList.SetRooms(rooms);

            Assert.AreEqual(2, _roomList.Rooms[0].playerCount);
        }

        // ── [06] SetRooms — replaces existing entries ─────────────────────────

        [Test]
        public void SetRooms_ReplacesExistingEntries()
        {
            _roomList.SetRooms(new List<RoomEntry> { new RoomEntry("OLD1", 1) });
            Assert.AreEqual(1, _roomList.Count);

            var newRooms = new List<RoomEntry>
            {
                new RoomEntry("NEW1", 2),
                new RoomEntry("NEW2", 1),
                new RoomEntry("NEW3", 2),
            };
            _roomList.SetRooms(newRooms);

            Assert.AreEqual(3, _roomList.Count);
            Assert.AreEqual("NEW1", _roomList.Rooms[0].roomCode);
            Assert.AreEqual("NEW3", _roomList.Rooms[2].roomCode);
        }

        // ── [07] SetRooms — null input ────────────────────────────────────────

        [Test]
        public void SetRooms_WithNull_ClearsList()
        {
            _roomList.SetRooms(new List<RoomEntry> { new RoomEntry("AAAA", 1) });
            Assert.AreEqual(1, _roomList.Count, "Pre-condition: list is not empty.");

            _roomList.SetRooms(null);

            Assert.AreEqual(0, _roomList.Count);
        }

        // ── [08] SetRooms — empty list ────────────────────────────────────────

        [Test]
        public void SetRooms_WithEmptyList_CountIsZero()
        {
            _roomList.SetRooms(new List<RoomEntry> { new RoomEntry("AAAA", 1) });
            _roomList.SetRooms(new List<RoomEntry>());

            Assert.AreEqual(0, _roomList.Count);
        }

        // ── [09] Clear — after SetRooms ───────────────────────────────────────

        [Test]
        public void Clear_AfterSetRooms_CountIsZero()
        {
            _roomList.SetRooms(new List<RoomEntry>
            {
                new RoomEntry("BBBB", 2),
                new RoomEntry("CCCC", 1),
            });
            Assert.AreEqual(2, _roomList.Count, "Pre-condition: list has entries.");

            _roomList.Clear();

            Assert.AreEqual(0, _roomList.Count);
        }

        // ── [10] Clear — on empty list ────────────────────────────────────────

        [Test]
        public void Clear_OnEmptyList_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _roomList.Clear());
            Assert.AreEqual(0, _roomList.Count);
        }

        // ── [11] RoomEntry — roomCode via constructor ─────────────────────────

        [Test]
        public void RoomEntry_Constructor_SetsRoomCode()
        {
            var entry = new RoomEntry("ZZZZ", 0);
            Assert.AreEqual("ZZZZ", entry.roomCode);
        }

        // ── [12] RoomEntry — playerCount via constructor ──────────────────────

        [Test]
        public void RoomEntry_Constructor_SetsPlayerCount()
        {
            var entry = new RoomEntry("QQQQ", 3);
            Assert.AreEqual(3, entry.playerCount);
        }
    }
}

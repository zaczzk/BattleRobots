using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode integration tests for T051 — Room browser pipeline.
    ///
    /// These tests exercise the full read path from adapter to SO to
    /// filter/sort query, verifying that each stage composes correctly.
    ///
    /// Coverage (12 cases):
    ///
    /// Group A — Adapter → RoomListSO → Query
    ///   [01] FullPipeline_FilterByPrefix_ReturnsMatchingRoomsOnly
    ///   [02] FullPipeline_SortByPlayerCountDesc_HighestFirst
    ///   [03] FullPipeline_FilterAndSort_Combined
    ///   [04] FullPipeline_MultipleRefreshCycles_SOHasNoStaleData
    ///
    /// Group B — Room state changes reflected through the pipeline
    ///   [05] JoinThenRefresh_PlayerCountIncrementedInSO
    ///   [06] FullRoom_IsFull_TrueAfterPipeline
    ///   [07] PrivateRoom_IsPrivateFlag_PreservedThroughPipeline
    ///   [08] RoomCapacity_MaxPlayersPreserved_AfterPipeline
    ///
    /// Group C — Edge-case correctness
    ///   [09] ClearThenRefresh_SOBecomesEmpty
    ///   [10] LargeRoomList_AllEntriesPresentInSO
    ///   [11] FilterAfterPipeline_NoMatch_ReturnsEmpty
    ///   [12] SingleEntryList_SortDoesNotThrow_AndReturnsEntry
    /// </summary>
    [TestFixture]
    public sealed class RoomBrowserIntegrationTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private RoomListSO         _roomListSO;

        /// <summary>
        /// Simulates the callback that NetworkEventBridge.RegisterAdapterCallbacks wires:
        ///   adapter.OnRoomListReceived = rooms => _roomList?.SetRooms(rooms);
        /// </summary>
        private void RequestAndApply() => _stub.RequestRoomList();

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub       = new StubNetworkAdapter();
            _roomListSO = ScriptableObject.CreateInstance<RoomListSO>();

            // Wire the same callback used by NetworkEventBridge.
            _stub.OnRoomListReceived = rooms => _roomListSO.SetRooms(rooms);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_roomListSO);
        }

        // ── Group A: Adapter → RoomListSO → Query ─────────────────────────────

        // [01] Filter by prefix after pipeline push
        [Test]
        public void FullPipeline_FilterByPrefix_ReturnsMatchingRoomsOnly()
        {
            _stub.Host("ABCD");
            _stub.Host("ABEF");
            _stub.Host("XYZW");

            RequestAndApply();

            Assert.AreEqual(3, _roomListSO.Count, "Pre-condition: SO should hold all rooms.");

            IReadOnlyList<RoomEntry> filtered =
                _roomListSO.GetSortedFilteredRooms("AB", RoomSortMode.None);

            Assert.AreEqual(2, filtered.Count,
                "Prefix 'AB' should match ABCD and ABEF only.");
            Assert.IsTrue(filtered[0].roomCode.StartsWith("AB", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(filtered[1].roomCode.StartsWith("AB", System.StringComparison.OrdinalIgnoreCase));
        }

        // [02] Sort by player-count descending after pipeline push
        [Test]
        public void FullPipeline_SortByPlayerCountDesc_HighestFirst()
        {
            // Host 3 rooms; join the 3rd one (so its playerCount == 2).
            _stub.Host("LOW1");                       // playerCount = 1
            _stub.Host("MED1");                       // playerCount = 1
            _stub.Host("TOP1");                       // playerCount = 1
            _stub.OnRoomJoinFailed = _ => { };
            _stub.Join("TOP1");                       // playerCount = 2

            RequestAndApply();

            IReadOnlyList<RoomEntry> sorted =
                _roomListSO.GetSortedFilteredRooms(null, RoomSortMode.ByPlayerCountDesc);

            Assert.AreEqual(3, sorted.Count);
            Assert.AreEqual("TOP1", sorted[0].roomCode,
                "Room with highest player count should appear first.");
            Assert.AreEqual(2, sorted[0].playerCount);
        }

        // [03] Combined prefix filter + sort by room-code ascending
        [Test]
        public void FullPipeline_FilterAndSort_Combined()
        {
            _stub.Host("BXYZ");
            _stub.Host("BAAB");
            _stub.Host("CDDD");
            _stub.Host("BMMM");

            RequestAndApply();

            // Filter "B" then sort A→Z: BAAB < BMMM < BXYZ
            IReadOnlyList<RoomEntry> result =
                _roomListSO.GetSortedFilteredRooms("B", RoomSortMode.ByRoomCodeAsc);

            Assert.AreEqual(3, result.Count, "Three rooms start with 'B'.");
            Assert.AreEqual("BAAB", result[0].roomCode);
            Assert.AreEqual("BMMM", result[1].roomCode);
            Assert.AreEqual("BXYZ", result[2].roomCode);
        }

        // [04] Multiple refresh cycles leave no stale data in the SO
        [Test]
        public void FullPipeline_MultipleRefreshCycles_SOHasNoStaleData()
        {
            _stub.Host("AAA1");
            _stub.Host("BBB2");
            RequestAndApply();
            Assert.AreEqual(2, _roomListSO.Count, "First refresh: 2 rooms.");

            // Second host (different code) — static dict grows to 3.
            _stub.Host("CCC3");
            RequestAndApply();
            Assert.AreEqual(3, _roomListSO.Count,
                "Second refresh: SO should reflect 3 rooms, not retain stale entries.");

            // Simulate a scenario where the server list shrinks: clear and host only one room.
            StubNetworkAdapter.ClearRooms();
            _stub.Host("ONLY");
            RequestAndApply();
            Assert.AreEqual(1, _roomListSO.Count,
                "After rooms are cleared server-side, SO should drop to 1.");
        }

        // ── Group B: Room state changes reflected through the pipeline ─────────

        // [05] Join after host — playerCount incremented in SO on next refresh
        [Test]
        public void JoinThenRefresh_PlayerCountIncrementedInSO()
        {
            _stub.Host("JOIN");                  // playerCount = 1 (host is player 0)
            _stub.OnRoomJoinFailed = _ => { };
            _stub.Join("JOIN");                  // playerCount = 2

            RequestAndApply();

            Assert.AreEqual(1, _roomListSO.Count);
            Assert.AreEqual(2, _roomListSO.Rooms[0].playerCount,
                "Player count should reflect the join on the next refresh.");
        }

        // [06] Full room has IsFull == true after pipeline push
        [Test]
        public void FullRoom_IsFull_TrueAfterPipeline()
        {
            _stub.Host("FULL", maxPlayers: 1);  // 1/1 immediately — host counts as one player

            RequestAndApply();

            Assert.AreEqual(1, _roomListSO.Count);
            Assert.IsTrue(_roomListSO.Rooms[0].IsFull,
                "IsFull should be true for a room at capacity.");
        }

        // [07] Private room preserves its isPrivate flag through the pipeline
        [Test]
        public void PrivateRoom_IsPrivateFlag_PreservedThroughPipeline()
        {
            _stub.Host("PRIV", maxPlayers: 2, isPrivate: true, password: "secret");

            RequestAndApply();

            Assert.AreEqual(1, _roomListSO.Count);
            Assert.IsTrue(_roomListSO.Rooms[0].isPrivate,
                "isPrivate flag must survive the adapter → SO pipeline.");
        }

        // [08] maxPlayers value preserved through the pipeline
        [Test]
        public void RoomCapacity_MaxPlayersPreserved_AfterPipeline()
        {
            _stub.Host("CAP4", maxPlayers: 4);

            RequestAndApply();

            Assert.AreEqual(1, _roomListSO.Count);
            Assert.AreEqual(4, _roomListSO.Rooms[0].maxPlayers,
                "maxPlayers must be preserved through the adapter → SO pipeline.");
        }

        // ── Group C: Edge-case correctness ────────────────────────────────────

        // [09] Clear all rooms then refresh → SO becomes empty
        [Test]
        public void ClearThenRefresh_SOBecomesEmpty()
        {
            _stub.Host("AAA1");
            _stub.Host("BBB2");
            RequestAndApply();
            Assert.AreEqual(2, _roomListSO.Count, "Pre-condition: rooms in SO.");

            StubNetworkAdapter.ClearRooms();
            RequestAndApply();

            Assert.AreEqual(0, _roomListSO.Count,
                "After server rooms are cleared, SO should be empty on next refresh.");
        }

        // [10] Large room list — all entries present in SO
        [Test]
        public void LargeRoomList_AllEntriesPresentInSO()
        {
            string[] codes = { "A001","A002","A003","A004","A005",
                               "A006","A007","A008","A009","A010" };
            foreach (string code in codes)
                _stub.Host(code);

            RequestAndApply();

            Assert.AreEqual(10, _roomListSO.Count,
                "All 10 hosted rooms should be present in the SO after refresh.");
        }

        // [11] Filter returns empty when no rooms match the prefix
        [Test]
        public void FilterAfterPipeline_NoMatch_ReturnsEmpty()
        {
            _stub.Host("ABCD");
            _stub.Host("ABEF");
            RequestAndApply();

            IReadOnlyList<RoomEntry> result =
                _roomListSO.GetSortedFilteredRooms("ZZ", RoomSortMode.None);

            Assert.AreEqual(0, result.Count,
                "Prefix 'ZZ' should match no rooms from the AB set.");
        }

        // [12] Single-entry list — all sort modes return without error
        [Test]
        public void SingleEntryList_SortDoesNotThrow_AndReturnsEntry()
        {
            _stub.Host("SOLO");
            RequestAndApply();

            Assert.AreEqual(1, _roomListSO.Count, "Pre-condition.");

            IReadOnlyList<RoomEntry> none    = _roomListSO.GetSortedFilteredRooms(null, RoomSortMode.None);
            IReadOnlyList<RoomEntry> byCount = _roomListSO.GetSortedFilteredRooms(null, RoomSortMode.ByPlayerCountDesc);
            IReadOnlyList<RoomEntry> byCode  = _roomListSO.GetSortedFilteredRooms(null, RoomSortMode.ByRoomCodeAsc);

            Assert.AreEqual(1, none.Count,    "None sort on single entry.");
            Assert.AreEqual(1, byCount.Count, "PlayerCountDesc sort on single entry.");
            Assert.AreEqual(1, byCode.Count,  "RoomCodeAsc sort on single entry.");
            Assert.AreEqual("SOLO", none[0].roomCode);
        }
    }
}

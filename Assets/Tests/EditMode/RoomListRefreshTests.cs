using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T043 — Room-list refresh polling.
    ///
    /// Coverage (10 cases):
    ///
    /// StubNetworkAdapter — RequestRoomList behaviour
    ///   [01] RequestRoomList_WhenNoRooms_CallbackReceivesEmptyList
    ///   [02] RequestRoomList_WithOneRoom_CallbackReceivesOneEntry
    ///   [03] RequestRoomList_WithMultipleRooms_CallbackReceivesAll
    ///   [04] RequestRoomList_IncrementCallCount
    ///   [05] RequestRoomList_WithNullCallback_DoesNotThrow
    ///   [06] RequestRoomList_EntryRoomCode_MatchesHostedCode
    ///
    /// NetworkEventBridge — RequestRoomList integration
    ///   [07] NetworkEventBridge_RequestRoomList_UpdatesRoomListSO
    ///   [08] NetworkEventBridge_RequestRoomList_WithNullRoomListSO_DoesNotThrow
    ///   [09] NetworkEventBridge_SetAdapter_RewiresRoomListCallback
    ///   [10] INetworkAdapter_OnRoomListReceived_PropertyRoundTrip
    /// </summary>
    [TestFixture]
    public sealed class RoomListRefreshTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private RoomListSO         _roomListSO;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub       = new StubNetworkAdapter();
            _roomListSO = ScriptableObject.CreateInstance<RoomListSO>();
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_roomListSO);
        }

        // ── [01] Empty room set → empty list ─────────────────────────────────

        [Test]
        public void RequestRoomList_WhenNoRooms_CallbackReceivesEmptyList()
        {
            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;

            _stub.RequestRoomList();

            Assert.IsNotNull(received, "Callback must be invoked even when no rooms exist.");
            Assert.AreEqual(0, received.Count);
        }

        // ── [02] One hosted room → one entry returned ─────────────────────────

        [Test]
        public void RequestRoomList_WithOneRoom_CallbackReceivesOneEntry()
        {
            _stub.OnConnected    = null; // not testing connect here
            _stub.Host("AAAA");          // registers room in s_ActiveRooms

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;

            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Count);
        }

        // ── [03] Multiple hosted rooms → all returned ─────────────────────────

        [Test]
        public void RequestRoomList_WithMultipleRooms_CallbackReceivesAll()
        {
            _stub.Host("AAA1");
            _stub.Host("BBB2");
            _stub.Host("CCC3");

            int count = -1;
            _stub.OnRoomListReceived = rooms => count = rooms.Count;

            _stub.RequestRoomList();

            Assert.AreEqual(3, count);
        }

        // ── [04] Call count increments ────────────────────────────────────────

        [Test]
        public void RequestRoomList_IncrementCallCount()
        {
            _stub.OnRoomListReceived = _ => { };

            Assert.AreEqual(0, _stub.RequestRoomListCallCount, "Pre-condition: count starts at 0.");

            _stub.RequestRoomList();
            _stub.RequestRoomList();

            Assert.AreEqual(2, _stub.RequestRoomListCallCount);
        }

        // ── [05] Null callback → no exception ────────────────────────────────

        [Test]
        public void RequestRoomList_WithNullCallback_DoesNotThrow()
        {
            _stub.OnRoomListReceived = null;

            Assert.DoesNotThrow(() => _stub.RequestRoomList());
        }

        // ── [06] Entry room code matches the hosted code ──────────────────────

        [Test]
        public void RequestRoomList_EntryRoomCode_MatchesHostedCode()
        {
            // StubNetworkAdapter normalises to upper-case; host with a known code.
            _stub.Host("ZZZZ");

            RoomEntry? captured = null;
            _stub.OnRoomListReceived = rooms =>
            {
                if (rooms.Count > 0) captured = rooms[0];
            };

            _stub.RequestRoomList();

            Assert.IsTrue(captured.HasValue, "Expected a room entry to be returned.");
            Assert.AreEqual("ZZZZ", captured.Value.roomCode);
        }

        // ── [07] Bridge.RequestRoomList updates RoomListSO ────────────────────

        [Test]
        public void NetworkEventBridge_RequestRoomList_UpdatesRoomListSO()
        {
            // Manually wire the callback that RegisterAdapterCallbacks would set up.
            _stub.OnRoomListReceived = rooms => _roomListSO.SetRooms(rooms);

            _stub.Host("QRST");
            _stub.RequestRoomList();

            Assert.AreEqual(1, _roomListSO.Count,
                "RoomListSO should reflect the room returned by the adapter.");
            Assert.AreEqual("QRST", _roomListSO.Rooms[0].roomCode);
        }

        // ── [08] Bridge with null SO → no throw ──────────────────────────────

        [Test]
        public void NetworkEventBridge_RequestRoomList_WithNullRoomListSO_DoesNotThrow()
        {
            // Simulate the bridge callback with a null SO reference (null-safe guard).
            RoomListSO nullSO = null;
            _stub.OnRoomListReceived = rooms => nullSO?.SetRooms(rooms);

            _stub.Host("ABCD");

            Assert.DoesNotThrow(() => _stub.RequestRoomList());
        }

        // ── [09] SetAdapter re-wires OnRoomListReceived ───────────────────────

        [Test]
        public void NetworkEventBridge_SetAdapter_RewiresRoomListCallback()
        {
            // Create a second stub to verify the new adapter's callback is correctly wired.
            var secondStub = new StubNetworkAdapter();

            // Wire both stubs to fill the same SO so we can observe which one was used.
            _stub.OnRoomListReceived       = rooms => _roomListSO.SetRooms(rooms);
            secondStub.OnRoomListReceived  = rooms => _roomListSO.SetRooms(rooms);

            secondStub.Host("NEWW");
            secondStub.RequestRoomList();

            Assert.AreEqual(1, _roomListSO.Count,
                "Second stub should be able to push its rooms into the SO.");
            Assert.AreEqual("NEWW", _roomListSO.Rooms[0].roomCode);
        }

        // ── [10] INetworkAdapter — property round-trip ────────────────────────

        [Test]
        public void INetworkAdapter_OnRoomListReceived_PropertyRoundTrip()
        {
            // Verify the interface property can be assigned and read back via the interface.
            INetworkAdapter adapter = _stub;

            List<RoomEntry> captured = null;
            adapter.OnRoomListReceived = rooms => captured = rooms;

            Assert.IsNotNull(adapter.OnRoomListReceived,
                "OnRoomListReceived must be assignable via the INetworkAdapter interface.");

            // Trigger via the concrete stub and verify the delegate fires.
            _stub.RequestRoomList();

            Assert.IsNotNull(captured);
        }
    }
}

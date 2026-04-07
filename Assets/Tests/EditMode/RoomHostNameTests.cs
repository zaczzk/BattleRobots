using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T055 — RoomEntry host-name label.
    ///
    /// Coverage (10 cases):
    ///
    /// RoomEntry — hostName field
    ///   [01] RoomEntry_DefaultHostName_IsEmpty
    ///   [02] RoomEntry_Constructor_SetsHostName
    ///   [03] RoomEntry_NullHostName_StoredAsEmpty
    ///
    /// StubNetworkAdapter — HostPlayerName / RequestRoomList
    ///   [04] HostPlayerName_Default_IsHost
    ///   [05] Host_StoresHostPlayerName_InRoomEntry
    ///   [06] RequestRoomList_ReturnsHostName
    ///   [07] Host_DifferentNames_PerRoom_PreservedInList
    ///   [08] ClearRooms_RemovesHostNameData
    ///
    /// RoomEntryUI — _hostNameLabel wiring
    ///   [09] Setup_ShowsHostName_InLabel
    ///   [10] Setup_NullHostNameLabel_DoesNotThrow
    /// </summary>
    [TestFixture]
    public sealed class RoomHostNameTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private GameObject         _go;
        private RoomEntryUI        _ui;
        private Text               _hostLabel;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _go        = new GameObject("RoomEntryUI_HostTest");
            _hostLabel = new GameObject("HostLabel").AddComponent<Text>();
            _hostLabel.transform.SetParent(_go.transform, false);

            _ui = _go.AddComponent<RoomEntryUI>();
            InjectField(_ui, "_hostNameLabel", _hostLabel);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_go);
        }

        // ── [01] RoomEntry.hostName defaults to empty string ──────────────────

        [Test]
        public void RoomEntry_DefaultHostName_IsEmpty()
        {
            var entry = new RoomEntry("AAAA", 1, 2);
            Assert.AreEqual(string.Empty, entry.hostName,
                "hostName must default to empty string when not supplied.");
        }

        // ── [02] RoomEntry constructor stores the supplied hostName ────────────

        [Test]
        public void RoomEntry_Constructor_SetsHostName()
        {
            var entry = new RoomEntry("BBBB", 1, 2, false, 0, "Alice");
            Assert.AreEqual("Alice", entry.hostName,
                "Constructor must store the supplied hostName.");
        }

        // ── [03] Null hostName is stored as empty string ──────────────────────

        [Test]
        public void RoomEntry_NullHostName_StoredAsEmpty()
        {
            var entry = new RoomEntry("CCCC", 1, 2, false, 0, null);
            Assert.AreEqual(string.Empty, entry.hostName,
                "A null hostName argument must be stored as empty string, not null.");
        }

        // ── [04] HostPlayerName defaults to "Host" ────────────────────────────

        [Test]
        public void HostPlayerName_Default_IsHost()
        {
            Assert.AreEqual("Host", _stub.HostPlayerName,
                "HostPlayerName must default to 'Host'.");
        }

        // ── [05] Host() stores HostPlayerName in the room entry ───────────────

        [Test]
        public void Host_StoresHostPlayerName_InRoomEntry()
        {
            _stub.HostPlayerName = "Bob";
            _stub.Host("DDDD");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual("Bob", received[0].hostName,
                "RequestRoomList must return the hostName set via HostPlayerName.");
        }

        // ── [06] RequestRoomList returns the correct hostName ─────────────────

        [Test]
        public void RequestRoomList_ReturnsHostName()
        {
            _stub.HostPlayerName = "Charlie";
            _stub.Host("EEEE");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = r => received = r;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual("Charlie", received[0].hostName,
                "hostName in returned RoomEntry must match HostPlayerName at the time Host() was called.");
        }

        // ── [07] Different rooms preserve their respective host names ─────────

        [Test]
        public void Host_DifferentNames_PerRoom_PreservedInList()
        {
            _stub.HostPlayerName = "Dave";
            _stub.Host("FFFF");

            _stub.HostPlayerName = "Eve";
            _stub.Host("GGGG");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = r => received = r;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(2, received.Count, "Both rooms should appear in the list.");

            // Build a lookup by code (order is not guaranteed by dict iteration).
            var byCode = new Dictionary<string, RoomEntry>();
            foreach (RoomEntry r in received)
                byCode[r.roomCode] = r;

            Assert.AreEqual("Dave", byCode["FFFF"].hostName,
                "Room FFFF must have hostName 'Dave'.");
            Assert.AreEqual("Eve", byCode["GGGG"].hostName,
                "Room GGGG must have hostName 'Eve'.");
        }

        // ── [08] ClearRooms removes all host-name data ────────────────────────

        [Test]
        public void ClearRooms_RemovesHostNameData()
        {
            _stub.HostPlayerName = "Frank";
            _stub.Host("HHHH");

            StubNetworkAdapter.ClearRooms();

            // Re-host with default name; the old name must not bleed through.
            _stub.HostPlayerName = "Host";
            _stub.Host("HHHH");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = r => received = r;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual("Host", received[0].hostName,
                "After ClearRooms, a re-hosted room must use the new HostPlayerName.");
        }

        // ── [09] Setup() shows hostName in the label ──────────────────────────

        [Test]
        public void Setup_ShowsHostName_InLabel()
        {
            var entry = new RoomEntry("IIII", 1, 2, false, 0, "Grace");
            _ui.Setup(entry, _ => { });

            Assert.AreEqual("Grace", _hostLabel.text,
                "Setup must write entry.hostName into the _hostNameLabel Text.");
        }

        // ── [10] Setup() with null label must not throw ───────────────────────

        [Test]
        public void Setup_NullHostNameLabel_DoesNotThrow()
        {
            InjectField(_ui, "_hostNameLabel", (Text)null);

            var entry = new RoomEntry("JJJJ", 1, 2, false, 0, "Henry");

            Assert.DoesNotThrow(
                () => _ui.Setup(entry, _ => { }),
                "Setup must not throw when _hostNameLabel is not wired in the Inspector.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InjectField<TComponent, TValue>(
            TComponent target, string fieldName, TValue value)
            where TComponent : Component
        {
            System.Reflection.FieldInfo field =
                typeof(TComponent).GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(field,
                $"Reflection: field '{fieldName}' not found on {typeof(TComponent).Name}.");

            field.SetValue(target, value);
        }
    }
}

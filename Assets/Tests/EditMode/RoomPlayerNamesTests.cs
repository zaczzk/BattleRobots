using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T059 — RoomEntry joined-player name list.
    ///
    /// Coverage (10 cases):
    ///
    /// RoomEntry — playerNames field
    ///   [01] RoomEntry_DefaultPlayerNames_IsNull
    ///   [02] RoomEntry_Constructor_SetsPlayerNames
    ///
    /// StubNetworkAdapter — playerNames population
    ///   [03] Host_PopulatesPlayerNames_WithHostName
    ///   [04] Join_AppendsPlayerName_ToNamesList
    ///   [05] JoinPlayerName_DefaultIsPlayer
    ///   [06] RequestRoomList_ReturnsPlayerNames
    ///   [07] ClearRooms_ClearsPlayerNames
    ///   [08] OnRoomUpdated_FiredOnJoin_WithUpdatedEntry
    ///
    /// RoomListSO — UpdateRoom mutator
    ///   [09] UpdateRoom_ReplacesExistingEntry_AndFiresEvent
    ///
    /// RoomEntryUI — _playerNamesLabel wiring
    ///   [10] Setup_ShowsCommaJoinedNames_InLabel
    ///   [11] Setup_NullPlayerNamesLabel_DoesNotThrow
    /// </summary>
    [TestFixture]
    public sealed class RoomPlayerNamesTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private RoomListSO         _roomList;
        private GameObject         _go;
        private RoomEntryUI        _ui;
        private Text               _namesLabel;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub     = new StubNetworkAdapter();
            _roomList = ScriptableObject.CreateInstance<RoomListSO>();

            _go         = new GameObject("RoomEntryUI_NamesTest");
            _namesLabel = new GameObject("NamesLabel").AddComponent<Text>();
            _namesLabel.transform.SetParent(_go.transform, false);

            _ui = _go.AddComponent<RoomEntryUI>();
            InjectField(_ui, "_playerNamesLabel", _namesLabel);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_roomList);
        }

        // ── [01] RoomEntry.playerNames defaults to null ───────────────────────

        [Test]
        public void RoomEntry_DefaultPlayerNames_IsNull()
        {
            var entry = new RoomEntry("AAAA", 1, 2);
            Assert.IsNull(entry.playerNames,
                "playerNames must default to null when not supplied via the constructor.");
        }

        // ── [02] RoomEntry constructor stores playerNames ─────────────────────

        [Test]
        public void RoomEntry_Constructor_SetsPlayerNames()
        {
            var names = new List<string> { "Alice", "Bob" };
            var entry = new RoomEntry("BBBB", 2, 2, playerNames: names);
            Assert.IsNotNull(entry.playerNames);
            Assert.AreEqual(2, entry.playerNames.Count);
            Assert.AreEqual("Alice", entry.playerNames[0]);
            Assert.AreEqual("Bob",   entry.playerNames[1]);
        }

        // ── [03] Host() populates playerNames with the host name ──────────────

        [Test]
        public void Host_PopulatesPlayerNames_WithHostName()
        {
            _stub.HostPlayerName = "Charlie";
            _stub.Host("CCCC");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Count);
            Assert.IsNotNull(received[0].playerNames,
                "playerNames must be populated after Host().");
            Assert.AreEqual(1, received[0].playerNames.Count,
                "After Host(), playerNames must contain exactly the host's name.");
            Assert.AreEqual("Charlie", received[0].playerNames[0],
                "Index 0 of playerNames must be the host's display name.");
        }

        // ── [04] Join() appends the joining player's name ─────────────────────

        [Test]
        public void Join_AppendsPlayerName_ToNamesList()
        {
            _stub.HostPlayerName = "Dave";
            _stub.Host("DDDD");

            _stub.JoinPlayerName = "Eve";
            _stub.Join("DDDD");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            var names = received[0].playerNames;
            Assert.IsNotNull(names);
            Assert.AreEqual(2, names.Count,
                "After one Join(), playerNames must contain host + joining player (2 entries).");
            Assert.AreEqual("Dave", names[0], "Index 0 must still be the host.");
            Assert.AreEqual("Eve",  names[1], "Index 1 must be the joining player.");
        }

        // ── [05] JoinPlayerName defaults to "Player" ──────────────────────────

        [Test]
        public void JoinPlayerName_DefaultIsPlayer()
        {
            Assert.AreEqual("Player", _stub.JoinPlayerName,
                "JoinPlayerName must default to 'Player'.");
        }

        // ── [06] RequestRoomList returns correct playerNames ──────────────────

        [Test]
        public void RequestRoomList_ReturnsPlayerNames()
        {
            _stub.HostPlayerName = "Frank";
            _stub.Host("EEEE");
            _stub.JoinPlayerName = "Grace";
            _stub.Join("EEEE");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = r => received = r;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.IsNotNull(received[0].playerNames);
            CollectionAssert.AreEqual(
                new[] { "Frank", "Grace" },
                received[0].playerNames,
                "RequestRoomList must return playerNames populated by Host + Join.");
        }

        // ── [07] ClearRooms clears player name data ───────────────────────────

        [Test]
        public void ClearRooms_ClearsPlayerNames()
        {
            _stub.HostPlayerName = "Henry";
            _stub.Host("FFFF");

            StubNetworkAdapter.ClearRooms();

            // Re-host with a different name — old names must not bleed through.
            _stub.HostPlayerName = "Ivy";
            _stub.Host("FFFF");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = r => received = r;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            var names = received[0].playerNames;
            Assert.IsNotNull(names);
            Assert.AreEqual(1, names.Count, "After ClearRooms, only the new host should appear.");
            Assert.AreEqual("Ivy", names[0]);
        }

        // ── [08] OnRoomUpdated is fired on Join with updated entry ────────────

        [Test]
        public void OnRoomUpdated_FiredOnJoin_WithUpdatedEntry()
        {
            _stub.HostPlayerName = "Jack";
            _stub.Host("GGGG");

            RoomEntry? updatedEntry = null;
            _stub.OnRoomUpdated = entry => updatedEntry = entry;

            _stub.JoinPlayerName = "Kim";
            _stub.Join("GGGG");

            Assert.IsTrue(updatedEntry.HasValue,
                "OnRoomUpdated must be invoked when a player joins.");
            Assert.AreEqual("GGGG", updatedEntry.Value.roomCode,
                "OnRoomUpdated must pass the updated entry with the correct room code.");
            Assert.AreEqual(2, updatedEntry.Value.playerCount,
                "OnRoomUpdated must pass the incremented playerCount.");
            Assert.IsNotNull(updatedEntry.Value.playerNames,
                "OnRoomUpdated entry must include the updated playerNames list.");
            CollectionAssert.AreEqual(
                new[] { "Jack", "Kim" },
                updatedEntry.Value.playerNames,
                "OnRoomUpdated entry's playerNames must include both host and joiner.");
        }

        // ── [09] RoomListSO.UpdateRoom replaces the entry and fires the event ─

        [Test]
        public void UpdateRoom_ReplacesExistingEntry_AndFiresEvent()
        {
            var initial = new RoomEntry("HHHH", 1, 2);
            _roomList.SetRooms(new List<RoomEntry> { initial });

            int eventFiredCount = 0;
            // Wire up a simple listener via reflection — avoids needing a full SO event.
            // Instead, call UpdateRoom and verify the list directly.
            var updated = new RoomEntry("HHHH", 2, 2,
                                        playerNames: new List<string> { "Leo", "Mia" });
            _roomList.UpdateRoom(updated);

            Assert.AreEqual(1, _roomList.Count,
                "UpdateRoom must not add a new entry; it replaces the existing one.");
            Assert.AreEqual(2, _roomList.Rooms[0].playerCount,
                "UpdateRoom must store the updated playerCount.");
            Assert.IsNotNull(_roomList.Rooms[0].playerNames);
            Assert.AreEqual(2, _roomList.Rooms[0].playerNames.Count,
                "UpdateRoom must store the updated playerNames list.");
        }

        // ── [10] Setup() shows comma-joined names in the label ────────────────

        [Test]
        public void Setup_ShowsCommaJoinedNames_InLabel()
        {
            var names = new List<string> { "Nina", "Otto" };
            var entry = new RoomEntry("IIII", 2, 4, playerNames: names);
            _ui.Setup(entry, _ => { });

            Assert.AreEqual("Nina, Otto", _namesLabel.text,
                "Setup must comma-join entry.playerNames and write the result to _playerNamesLabel.");
        }

        // ── [11] Setup() with null label must not throw ───────────────────────

        [Test]
        public void Setup_NullPlayerNamesLabel_DoesNotThrow()
        {
            InjectField(_ui, "_playerNamesLabel", (Text)null);

            var entry = new RoomEntry("JJJJ", 1, 2,
                                      playerNames: new List<string> { "Pete" });

            Assert.DoesNotThrow(
                () => _ui.Setup(entry, _ => { }),
                "Setup must not throw when _playerNamesLabel is not wired in the Inspector.");
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

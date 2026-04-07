using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T062 — Room kick/ban.
    ///
    /// Coverage (8 cases):
    ///
    /// StubNetworkAdapter — KickPlayer
    ///   [01] KickPlayer_RemovesPlayerFromNamesList
    ///   [02] KickPlayer_DecrementsPlayerCount
    ///   [03] KickPlayer_FiresOnPlayerKickedCallback_WithPlayerName
    ///   [04] KickPlayer_UnknownRoom_DoesNotFireCallback
    ///   [05] KickPlayer_FiresOnRoomUpdated_WithUpdatedEntry
    ///
    /// StubNetworkAdapter — KickCallCount
    ///   [06] KickPlayer_IncrementsKickCallCount
    ///
    /// KickedUI
    ///   [07] KickedUI_ShowKicked_ShowsPanelAndSetsReason
    ///   [08] KickedUI_Hide_HidesPanel
    /// </summary>
    [TestFixture]
    public sealed class RoomKickTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private GameObject         _uiGo;
        private KickedUI           _kickedUI;
        private Text               _reasonLabel;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _uiGo        = new GameObject("KickedUI_Test");
            _reasonLabel = new GameObject("ReasonLabel").AddComponent<Text>();
            _reasonLabel.transform.SetParent(_uiGo.transform, false);

            _kickedUI = _uiGo.AddComponent<KickedUI>();
            InjectField(_kickedUI, "_reasonLabel", _reasonLabel);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_uiGo);
        }

        // ── [01] KickPlayer removes player from the names list ────────────────

        [Test]
        public void KickPlayer_RemovesPlayerFromNamesList()
        {
            _stub.HostPlayerName = "Alice";
            _stub.JoinPlayerName = "Bob";
            _stub.Host("AAAA");
            _stub.Join("AAAA");

            // Verify Bob joined.
            List<RoomEntry> list = null;
            _stub.OnRoomListReceived = r => list = r;
            _stub.RequestRoomList();
            Assert.IsNotNull(list);
            CollectionAssert.Contains(list[0].playerNames, "Bob",
                "Bob must be in the player list before the kick.");

            _stub.KickPlayer("AAAA", "Bob");

            list = null;
            _stub.RequestRoomList();
            Assert.IsNotNull(list);
            CollectionAssert.DoesNotContain(list[0].playerNames, "Bob",
                "Bob must be removed from the player list after the kick.");
        }

        // ── [02] KickPlayer decrements the room's playerCount ─────────────────

        [Test]
        public void KickPlayer_DecrementsPlayerCount()
        {
            _stub.HostPlayerName = "Alice";
            _stub.JoinPlayerName = "Bob";
            _stub.Host("BBBB");
            _stub.Join("BBBB");

            // Room should have 2 players now.
            List<RoomEntry> list = null;
            _stub.OnRoomListReceived = r => list = r;
            _stub.RequestRoomList();
            Assert.AreEqual(2, list[0].playerCount, "Pre-kick player count must be 2.");

            _stub.KickPlayer("BBBB", "Bob");

            list = null;
            _stub.RequestRoomList();
            Assert.AreEqual(1, list[0].playerCount,
                "Player count must decrement to 1 after Bob is kicked.");
        }

        // ── [03] KickPlayer fires OnPlayerKicked with the correct name ─────────

        [Test]
        public void KickPlayer_FiresOnPlayerKickedCallback_WithPlayerName()
        {
            _stub.Host("CCCC");
            _stub.JoinPlayerName = "Charlie";
            _stub.Join("CCCC");

            string kickedName = null;
            _stub.OnPlayerKicked = name => kickedName = name;

            _stub.KickPlayer("CCCC", "Charlie");

            Assert.AreEqual("Charlie", kickedName,
                "OnPlayerKicked must be invoked with the kicked player's display name.");
        }

        // ── [04] KickPlayer for unknown room does not fire callback ───────────

        [Test]
        public void KickPlayer_UnknownRoom_DoesNotFireCallback()
        {
            bool callbackFired = false;
            _stub.OnPlayerKicked = _ => callbackFired = true;

            _stub.KickPlayer("ZZZZ", "Ghost");

            Assert.IsFalse(callbackFired,
                "OnPlayerKicked must NOT fire when the room does not exist.");
        }

        // ── [05] KickPlayer fires OnRoomUpdated with updated entry ────────────

        [Test]
        public void KickPlayer_FiresOnRoomUpdated_WithUpdatedEntry()
        {
            _stub.HostPlayerName = "Host";
            _stub.JoinPlayerName = "Guest";
            _stub.Host("DDDD");
            _stub.Join("DDDD");

            RoomEntry? updatedEntry = null;
            _stub.OnRoomUpdated = entry => updatedEntry = entry;

            _stub.KickPlayer("DDDD", "Guest");

            Assert.IsTrue(updatedEntry.HasValue,
                "OnRoomUpdated must fire after a kick so room-browser UIs can refresh.");
            Assert.AreEqual(1, updatedEntry.Value.playerCount,
                "The updated entry must reflect the decremented player count.");
            CollectionAssert.DoesNotContain(updatedEntry.Value.playerNames, "Guest",
                "The updated entry must not contain the kicked player's name.");
        }

        // ── [06] KickCallCount increments on each KickPlayer call ─────────────

        [Test]
        public void KickPlayer_IncrementsKickCallCount()
        {
            _stub.Host("EEEE");
            _stub.JoinPlayerName = "Player1";
            _stub.Join("EEEE");

            Assert.AreEqual(0, _stub.KickCallCount, "KickCallCount must start at 0.");

            _stub.KickPlayer("EEEE", "Player1");
            Assert.AreEqual(1, _stub.KickCallCount, "KickCallCount must be 1 after one kick.");

            _stub.KickPlayer("ZZZZ", "Nobody"); // non-existent room
            Assert.AreEqual(2, _stub.KickCallCount,
                "KickCallCount must still increment even for a no-op kick on an unknown room.");
        }

        // ── [07] KickedUI.ShowKicked shows panel and sets reason text ─────────

        [Test]
        public void KickedUI_ShowKicked_ShowsPanelAndSetsReason()
        {
            Assert.IsFalse(_kickedUI.IsVisible,
                "KickedUI must be hidden by default.");
            Assert.AreEqual(string.Empty, _kickedUI.LastReason,
                "LastReason must be empty before ShowKicked is called.");

            _kickedUI.ShowKicked("Dave");

            Assert.IsTrue(_kickedUI.IsVisible,
                "IsVisible must be true after ShowKicked.");
            Assert.AreEqual("Dave", _kickedUI.LastReason,
                "LastReason must store the kicked player's name.");
            Assert.IsNotEmpty(_reasonLabel.text,
                "Reason label must contain text after ShowKicked.");
        }

        // ── [08] KickedUI.Hide hides the panel ───────────────────────────────

        [Test]
        public void KickedUI_Hide_HidesPanel()
        {
            _kickedUI.ShowKicked("Eve");
            Assert.IsTrue(_kickedUI.IsVisible, "Precondition: panel must be visible.");

            _kickedUI.Hide();

            Assert.IsFalse(_kickedUI.IsVisible,
                "IsVisible must be false after Hide() is called.");
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

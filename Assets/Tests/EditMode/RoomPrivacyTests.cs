using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T045 — Room password / private rooms.
    ///
    /// Coverage (12 cases):
    ///
    /// RoomEntry — isPrivate field
    ///   [01] RoomEntry_IsPrivate_DefaultsFalse
    ///   [02] RoomEntry_Constructor_SetsIsPrivate
    ///
    /// StubNetworkAdapter — private-room Host / Join / RequestRoomList
    ///   [03] Host_PrivateRoom_AppearInList_WithIsPrivateFlagTrue
    ///   [04] Join_PrivateRoom_CorrectPassword_Succeeds
    ///   [05] Join_PrivateRoom_WrongPassword_InvokesOnRoomJoinFailed
    ///   [06] Join_PrivateRoom_EmptyPassword_InvokesOnRoomJoinFailed
    ///   [07] Join_PublicRoom_WithPasswordArg_PasswordIgnored_Succeeds
    ///   [08] Join_BackwardCompat_NoPassword_PublicRoom_Succeeds
    ///   [09] ClearRooms_AlsoClearsPasswords_SubsequentPrivateJoinFails
    ///   [10] RequestRoomList_PrivateAndPublicMixed_BothAppearInList
    ///
    /// RoomEntry — isPrivate preserved through RoomListSO
    ///   [11] RoomListSO_SetRooms_PrivateRoom_IsPrivateFlagPreserved
    ///
    /// INetworkAdapter — interface contract
    ///   [12] StubAdapter_Join_String_String_RespectsPasswordCheck
    /// </summary>
    [TestFixture]
    public sealed class RoomPrivacyTests
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

        // ── [01] RoomEntry — isPrivate defaults to false ──────────────────────

        [Test]
        public void RoomEntry_IsPrivate_DefaultsFalse()
        {
            // Three-arg constructor should leave isPrivate = false.
            var entry = new RoomEntry("ABCD", playerCount: 1, maxPlayers: 2);
            Assert.IsFalse(entry.isPrivate,
                "isPrivate must default to false when not specified in constructor.");
        }

        // ── [02] RoomEntry — constructor sets isPrivate ───────────────────────

        [Test]
        public void RoomEntry_Constructor_SetsIsPrivate()
        {
            var entry = new RoomEntry("ABCD", playerCount: 1, maxPlayers: 2, isPrivate: true);
            Assert.IsTrue(entry.isPrivate,
                "Four-arg constructor must set isPrivate to true when requested.");
        }

        // ── [03] Private room appears in the room list with isPrivate = true ──

        [Test]
        public void Host_PrivateRoom_AppearInList_WithIsPrivateFlagTrue()
        {
            _stub.Host("PRIV", maxPlayers: 2, isPrivate: true, password: "Secret");

            RoomEntry? captured = null;
            _stub.OnRoomListReceived = rooms =>
            {
                if (rooms.Count > 0) captured = rooms[0];
            };
            _stub.RequestRoomList();

            Assert.IsTrue(captured.HasValue, "Room must appear in the list.");
            Assert.IsTrue(captured.Value.isPrivate,
                "isPrivate must be true in the list entry for a private room.");
        }

        // ── [04] Join a private room with the correct password ────────────────

        [Test]
        public void Join_PrivateRoom_CorrectPassword_Succeeds()
        {
            _stub.Host("LOCK", maxPlayers: 2, isPrivate: true, password: "OpenSesame");

            string joinedCode = null;
            _stub.OnRoomJoined = code => joinedCode = code;

            _stub.Join("LOCK", "OpenSesame");

            Assert.IsNotNull(joinedCode,
                "OnRoomJoined must be invoked when the correct password is supplied.");
            Assert.AreEqual("LOCK", joinedCode);
        }

        // ── [05] Join a private room with the wrong password ──────────────────

        [Test]
        public void Join_PrivateRoom_WrongPassword_InvokesOnRoomJoinFailed()
        {
            _stub.Host("SAFE", maxPlayers: 2, isPrivate: true, password: "Correct");

            string failReason = null;
            _stub.OnRoomJoinFailed = reason => failReason = reason;

            _stub.Join("SAFE", "Wrong");

            Assert.IsNotNull(failReason,
                "OnRoomJoinFailed must be invoked when an incorrect password is supplied.");
            StringAssert.Contains("password", failReason,
                "Failure reason must mention that a password is required.");
        }

        // ── [06] Join a private room with an empty password ───────────────────

        [Test]
        public void Join_PrivateRoom_EmptyPassword_InvokesOnRoomJoinFailed()
        {
            _stub.Host("BOLT", maxPlayers: 2, isPrivate: true, password: "1234");

            string failReason = null;
            _stub.OnRoomJoinFailed = reason => failReason = reason;

            _stub.Join("BOLT", string.Empty);

            Assert.IsNotNull(failReason,
                "OnRoomJoinFailed must be invoked when an empty password is supplied for a private room.");
        }

        // ── [07] Join a public room — password arg is silently ignored ────────

        [Test]
        public void Join_PublicRoom_WithPasswordArg_PasswordIgnored_Succeeds()
        {
            _stub.Host("OPEN", maxPlayers: 2, isPrivate: false, password: string.Empty);

            string joinedCode = null;
            _stub.OnRoomJoined = code => joinedCode = code;

            // Passing any non-empty password to a public room must not cause failure.
            _stub.Join("OPEN", "AnyPasswordHere");

            Assert.IsNotNull(joinedCode,
                "Joining a public room must succeed regardless of the password argument.");
        }

        // ── [08] Backward-compat: Join(string) on a public room succeeds ──────

        [Test]
        public void Join_BackwardCompat_NoPassword_PublicRoom_Succeeds()
        {
            _stub.Host("BACK"); // public room, default capacity

            string joinedCode = null;
            _stub.OnRoomJoined = code => joinedCode = code;

            // Original single-arg Join must still work.
            _stub.Join("BACK");

            Assert.IsNotNull(joinedCode,
                "Single-arg Join must still succeed for public rooms (backward compatibility).");
        }

        // ── [09] ClearRooms also clears stored passwords ──────────────────────

        [Test]
        public void ClearRooms_AlsoClearsPasswords_SubsequentPrivateJoinFails()
        {
            _stub.Host("GONE", maxPlayers: 2, isPrivate: true, password: "pw");
            StubNetworkAdapter.ClearRooms();

            // A fresh stub after ClearRooms — room no longer exists.
            var freshStub = new StubNetworkAdapter();

            string failReason = null;
            freshStub.OnRoomJoinFailed = reason => failReason = reason;
            freshStub.Join("GONE", "pw");

            Assert.IsNotNull(failReason,
                "Join must fail after ClearRooms because the room no longer exists.");
        }

        // ── [10] RequestRoomList returns both private and public rooms ─────────

        [Test]
        public void RequestRoomList_PrivateAndPublicMixed_BothAppearInList()
        {
            _stub.Host("PUB1", maxPlayers: 2, isPrivate: false, password: string.Empty);
            _stub.Host("PRV2", maxPlayers: 2, isPrivate: true,  password: "secret");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = new List<RoomEntry>(rooms);
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(2, received.Count, "Both private and public rooms must appear in the list.");

            bool hasPublic  = false;
            bool hasPrivate = false;
            foreach (var r in received)
            {
                if (r.roomCode == "PUB1" && !r.isPrivate) hasPublic  = true;
                if (r.roomCode == "PRV2" &&  r.isPrivate) hasPrivate = true;
            }

            Assert.IsTrue(hasPublic,  "Public room must appear with isPrivate = false.");
            Assert.IsTrue(hasPrivate, "Private room must appear with isPrivate = true.");
        }

        // ── [11] RoomListSO preserves isPrivate flag ──────────────────────────

        [Test]
        public void RoomListSO_SetRooms_PrivateRoom_IsPrivateFlagPreserved()
        {
            var rooms = new List<RoomEntry>
            {
                new RoomEntry("PRSO", playerCount: 1, maxPlayers: 2, isPrivate: true)
            };

            _roomListSO.SetRooms(rooms);

            Assert.AreEqual(1, _roomListSO.Count, "Pre-condition: one room in list.");
            Assert.IsTrue(_roomListSO.Rooms[0].isPrivate,
                "isPrivate flag must be preserved when stored in and retrieved from RoomListSO.");
        }

        // ── [12] Interface contract — Join(string, string) routes to password check

        [Test]
        public void StubAdapter_Join_String_String_RespectsPasswordCheck()
        {
            // Verify INetworkAdapter reference also dispatches correctly.
            INetworkAdapter adapter = new StubNetworkAdapter();
            adapter.OnRoomJoined = _ => { };

            // Host a private room via the interface (cast needed for the 4-arg overload).
            ((StubNetworkAdapter)adapter).Host("IFACE", 2, true, "iface_pw");

            string failReason = null;
            adapter.OnRoomJoinFailed = reason => failReason = reason;

            // Wrong password via interface.
            adapter.Join("IFACE", "wrong");

            Assert.IsNotNull(failReason,
                "INetworkAdapter.Join(string,string) must enforce the password check.");
        }
    }
}

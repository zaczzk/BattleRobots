using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="NetworkSessionSO"/> and
    /// <see cref="StubNetworkAdapter"/> / <see cref="NetworkEventBridge"/> integration.
    ///
    /// Coverage (18 cases):
    ///
    /// NetworkSessionSO — initial state
    ///   [01] DefaultState_ConnectionState_IsDisconnected
    ///   [02] DefaultState_Role_IsNone
    ///   [03] DefaultState_RoomCode_IsEmpty
    ///   [04] DefaultState_IsConnected_IsFalse
    ///   [05] DefaultState_IsInMatch_IsFalse
    ///
    /// NetworkSessionSO — Connect / SetConnected
    ///   [06] Connect_AsHost_SetsStateToConnecting
    ///   [07] Connect_AsClient_SetsRoleToClient
    ///   [08] Connect_WhenConnecting_SecondCall_IsIgnored
    ///   [09] SetConnected_FromConnecting_SetsStateToConnected
    ///   [10] SetConnected_FromDisconnected_IsIgnored
    ///
    /// NetworkSessionSO — JoinRoom
    ///   [11] JoinRoom_FromConnected_SetsStateToInMatch
    ///   [12] JoinRoom_StoresNormalisedRoomCode
    ///   [13] JoinRoom_FromDisconnected_IsIgnored
    ///   [14] JoinRoom_EmptyCode_IsIgnored
    ///
    /// NetworkSessionSO — Disconnect
    ///   [15] Disconnect_FromInMatch_ResetsState
    ///   [16] Disconnect_ClearsRoleAndRoomCode
    ///   [17] IsConnected_TrueWhenConnected
    ///   [18] IsConnected_TrueWhenInMatch
    ///
    /// StubNetworkAdapter — lifecycle
    ///   [19] Stub_Connect_InvokesOnConnected
    ///   [20] Stub_Disconnect_InvokesOnDisconnected
    ///   [21] Stub_Host_RegistersRoomAndInvokesOnRoomJoined
    ///   [22] Stub_Join_ExistingRoom_InvokesOnRoomJoined
    ///   [23] Stub_Join_MissingRoom_InvokesOnRoomJoinFailed
    ///   [24] Stub_SendMatchState_RecordsPayload
    ///   [25] Stub_SendMatchState_NullPayload_DoesNotThrow
    /// </summary>
    [TestFixture]
    public sealed class NetworkSessionSOTests
    {
        private NetworkSessionSO _session;

        [SetUp]
        public void SetUp()
        {
            _session = ScriptableObject.CreateInstance<NetworkSessionSO>();
            StubNetworkAdapter.ClearRooms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_session);
            StubNetworkAdapter.ClearRooms();
        }

        // ── [01–05] Initial state ─────────────────────────────────────────────

        [Test]
        public void DefaultState_ConnectionState_IsDisconnected()
        {
            Assert.AreEqual(NetworkConnectionState.Disconnected, _session.ConnectionState);
        }

        [Test]
        public void DefaultState_Role_IsNone()
        {
            Assert.AreEqual(NetworkRole.None, _session.Role);
        }

        [Test]
        public void DefaultState_RoomCode_IsEmpty()
        {
            Assert.AreEqual(string.Empty, _session.RoomCode);
        }

        [Test]
        public void DefaultState_IsConnected_IsFalse()
        {
            Assert.IsFalse(_session.IsConnected);
        }

        [Test]
        public void DefaultState_IsInMatch_IsFalse()
        {
            Assert.IsFalse(_session.IsInMatch);
        }

        // ── [06–10] Connect / SetConnected ────────────────────────────────────

        [Test]
        public void Connect_AsHost_SetsStateToConnecting()
        {
            _session.Connect(NetworkRole.Host);
            Assert.AreEqual(NetworkConnectionState.Connecting, _session.ConnectionState);
        }

        [Test]
        public void Connect_AsClient_SetsRoleToClient()
        {
            _session.Connect(NetworkRole.Client);
            Assert.AreEqual(NetworkRole.Client, _session.Role);
        }

        [Test]
        public void Connect_WhenConnecting_SecondCall_IsIgnored()
        {
            _session.Connect(NetworkRole.Host);
            // Second call with a different role should be ignored.
            _session.Connect(NetworkRole.Client);
            Assert.AreEqual(NetworkRole.Host, _session.Role);
        }

        [Test]
        public void SetConnected_FromConnecting_SetsStateToConnected()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            Assert.AreEqual(NetworkConnectionState.Connected, _session.ConnectionState);
        }

        [Test]
        public void SetConnected_FromDisconnected_IsIgnored()
        {
            // State is Disconnected — SetConnected should be a no-op.
            _session.SetConnected();
            Assert.AreEqual(NetworkConnectionState.Disconnected, _session.ConnectionState);
        }

        // ── [11–14] JoinRoom ──────────────────────────────────────────────────

        [Test]
        public void JoinRoom_FromConnected_SetsStateToInMatch()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            _session.JoinRoom("AAAA");
            Assert.AreEqual(NetworkConnectionState.InMatch, _session.ConnectionState);
        }

        [Test]
        public void JoinRoom_StoresNormalisedRoomCode()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            _session.JoinRoom(" abcd ");
            Assert.AreEqual("ABCD", _session.RoomCode);
        }

        [Test]
        public void JoinRoom_FromDisconnected_IsIgnored()
        {
            _session.JoinRoom("AAAA");
            Assert.AreEqual(NetworkConnectionState.Disconnected, _session.ConnectionState);
        }

        [Test]
        public void JoinRoom_EmptyCode_IsIgnored()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            _session.JoinRoom("   ");
            Assert.AreEqual(NetworkConnectionState.Connected, _session.ConnectionState);
        }

        // ── [15–18] Disconnect ────────────────────────────────────────────────

        [Test]
        public void Disconnect_FromInMatch_ResetsState()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            _session.JoinRoom("ZZZZ");
            _session.Disconnect();
            Assert.AreEqual(NetworkConnectionState.Disconnected, _session.ConnectionState);
        }

        [Test]
        public void Disconnect_ClearsRoleAndRoomCode()
        {
            _session.Connect(NetworkRole.Client);
            _session.SetConnected();
            _session.JoinRoom("XXXX");
            _session.Disconnect();

            Assert.AreEqual(NetworkRole.None,  _session.Role);
            Assert.AreEqual(string.Empty,      _session.RoomCode);
        }

        [Test]
        public void IsConnected_TrueWhenConnected()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            Assert.IsTrue(_session.IsConnected);
        }

        [Test]
        public void IsConnected_TrueWhenInMatch()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            _session.JoinRoom("TTTT");
            Assert.IsTrue(_session.IsConnected);
        }

        // ── [19–25] StubNetworkAdapter ────────────────────────────────────────

        [Test]
        public void Stub_Connect_InvokesOnConnected()
        {
            var stub = new StubNetworkAdapter();
            bool fired = false;
            stub.OnConnected = () => fired = true;
            stub.Connect();
            Assert.IsTrue(fired);
        }

        [Test]
        public void Stub_Disconnect_InvokesOnDisconnected()
        {
            var stub = new StubNetworkAdapter();
            bool fired = false;
            stub.OnDisconnected = () => fired = true;
            stub.Disconnect();
            Assert.IsTrue(fired);
        }

        [Test]
        public void Stub_Host_RegistersRoomAndInvokesOnRoomJoined()
        {
            var stub = new StubNetworkAdapter();
            string joinedCode = null;
            stub.OnRoomJoined = code => joinedCode = code;
            stub.Host("BETA");
            Assert.AreEqual("BETA", joinedCode);
        }

        [Test]
        public void Stub_Join_ExistingRoom_InvokesOnRoomJoined()
        {
            var host = new StubNetworkAdapter();
            host.Host("ABCD"); // registers room

            var client = new StubNetworkAdapter();
            string joinedCode = null;
            client.OnRoomJoined = code => joinedCode = code;
            client.Join("ABCD");

            Assert.AreEqual("ABCD", joinedCode);
        }

        [Test]
        public void Stub_Join_MissingRoom_InvokesOnRoomJoinFailed()
        {
            var stub = new StubNetworkAdapter();
            string failReason = null;
            stub.OnRoomJoinFailed = reason => failReason = reason;
            stub.Join("ZZZZ");
            Assert.IsNotNull(failReason);
            Assert.IsNotEmpty(failReason);
        }

        [Test]
        public void Stub_SendMatchState_RecordsPayload()
        {
            var stub = new StubNetworkAdapter();
            byte[] payload = { 1, 2, 3 };
            stub.SendMatchState(payload);
            Assert.AreEqual(1,        stub.SentPayloads.Count);
            Assert.AreEqual(payload,  stub.SentPayloads[0]);
        }

        [Test]
        public void Stub_SendMatchState_NullPayload_DoesNotThrow()
        {
            var stub = new StubNetworkAdapter();
            Assert.DoesNotThrow(() => stub.SendMatchState(null));
            Assert.AreEqual(0, stub.SentPayloads.Count);
        }
    }
}

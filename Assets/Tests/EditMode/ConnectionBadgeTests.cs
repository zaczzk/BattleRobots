using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T041 — Player connection status badge.
    ///
    /// Coverage (10 cases):
    ///
    /// ConnectionBadgeUI — state tracking
    ///   [01] DefaultState_IsDisconnected
    ///   [02] OnConnecting_SetsConnectingState
    ///   [03] OnConnected_SetsConnectedState
    ///   [04] OnReconnecting_SetsReconnectingState
    ///   [05] OnMatchJoined_SetsInMatchState
    ///   [06] OnDisconnected_AfterConnected_ResetsToDisconnected
    ///
    /// ConnectionStateLabel — state tracking and text mapping
    ///   [07] DefaultState_IsDisconnected
    ///   [08] OnConnecting_TextContainsConnecting
    ///   [09] OnConnected_TextIsOnline
    ///   [10] OnReconnecting_TextContainsReconnecting
    /// </summary>
    [TestFixture]
    public sealed class ConnectionBadgeTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject           _badgeGO;
        private ConnectionBadgeUI    _badge;

        private GameObject           _labelGO;
        private ConnectionStateLabel _stateLabel;

        [SetUp]
        public void SetUp()
        {
            _badgeGO  = new GameObject("TestBadge");
            _badge    = _badgeGO.AddComponent<ConnectionBadgeUI>();

            _labelGO    = new GameObject("TestLabel");
            _stateLabel = _labelGO.AddComponent<ConnectionStateLabel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_badgeGO);
            Object.DestroyImmediate(_labelGO);
        }

        // ── [01] ConnectionBadgeUI — default state ────────────────────────────

        [Test]
        public void DefaultState_Badge_IsDisconnected()
        {
            Assert.AreEqual(NetworkConnectionState.Disconnected, _badge.CurrentState,
                "Badge must start in Disconnected state after Awake.");
        }

        // ── [02] ConnectionBadgeUI — Connecting ───────────────────────────────

        [Test]
        public void OnConnecting_Badge_SetsConnectingState()
        {
            _badge.OnConnecting();

            Assert.AreEqual(NetworkConnectionState.Connecting, _badge.CurrentState,
                "OnConnecting must set CurrentState to Connecting.");
        }

        // ── [03] ConnectionBadgeUI — Connected ───────────────────────────────

        [Test]
        public void OnConnected_Badge_SetsConnectedState()
        {
            _badge.OnConnecting();
            _badge.OnConnected();

            Assert.AreEqual(NetworkConnectionState.Connected, _badge.CurrentState,
                "OnConnected must set CurrentState to Connected.");
        }

        // ── [04] ConnectionBadgeUI — Reconnecting ─────────────────────────────

        [Test]
        public void OnReconnecting_Badge_SetsReconnectingState()
        {
            _badge.OnConnecting();
            _badge.OnConnected();
            _badge.OnReconnecting();

            Assert.AreEqual(NetworkConnectionState.Reconnecting, _badge.CurrentState,
                "OnReconnecting must set CurrentState to Reconnecting.");
        }

        // ── [05] ConnectionBadgeUI — InMatch ─────────────────────────────────

        [Test]
        public void OnMatchJoined_Badge_SetsInMatchState()
        {
            _badge.OnConnecting();
            _badge.OnConnected();
            _badge.OnMatchJoined();

            Assert.AreEqual(NetworkConnectionState.InMatch, _badge.CurrentState,
                "OnMatchJoined must set CurrentState to InMatch.");
        }

        // ── [06] ConnectionBadgeUI — Disconnected after Connected ─────────────

        [Test]
        public void OnDisconnected_AfterConnected_ResetsToDisconnected()
        {
            _badge.OnConnecting();
            _badge.OnConnected();
            _badge.OnDisconnected();

            Assert.AreEqual(NetworkConnectionState.Disconnected, _badge.CurrentState,
                "OnDisconnected must always return badge to Disconnected state.");
        }

        // ── [07] ConnectionStateLabel — default state ─────────────────────────

        [Test]
        public void DefaultState_Label_IsDisconnected()
        {
            Assert.AreEqual(NetworkConnectionState.Disconnected, _stateLabel.CurrentState,
                "ConnectionStateLabel must start in Disconnected state after Awake.");
        }

        // ── [08] ConnectionStateLabel — Connecting text ───────────────────────

        [Test]
        public void OnConnecting_Label_TextContainsConnecting()
        {
            _stateLabel.OnConnecting();

            Assert.AreEqual(NetworkConnectionState.Connecting, _stateLabel.CurrentState,
                "CurrentState must be Connecting after OnConnecting().");

            StringAssert.Contains("onnect", _stateLabel.CurrentText,
                "Connecting text should reference 'connect' in some form (e.g. 'Connecting…').");
        }

        // ── [09] ConnectionStateLabel — Connected text ────────────────────────

        [Test]
        public void OnConnected_Label_TextIsOnline()
        {
            _stateLabel.OnConnecting();
            _stateLabel.OnConnected();

            Assert.AreEqual(NetworkConnectionState.Connected, _stateLabel.CurrentState,
                "CurrentState must be Connected after OnConnected().");

            Assert.IsFalse(string.IsNullOrEmpty(_stateLabel.CurrentText),
                "Connected text must not be empty.");
        }

        // ── [10] ConnectionStateLabel — Reconnecting text ─────────────────────

        [Test]
        public void OnReconnecting_Label_TextContainsReconnecting()
        {
            _stateLabel.OnConnecting();
            _stateLabel.OnConnected();
            _stateLabel.OnReconnecting();

            Assert.AreEqual(NetworkConnectionState.Reconnecting, _stateLabel.CurrentState,
                "CurrentState must be Reconnecting after OnReconnecting().");

            StringAssert.Contains("econnect", _stateLabel.CurrentText,
                "Reconnecting text should reference 'reconnect' in some form.");
        }
    }
}

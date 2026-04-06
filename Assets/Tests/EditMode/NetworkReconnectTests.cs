using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T039 — Network reconnection flow.
    ///
    /// Coverage (15 cases):
    ///
    /// SessionTimeoutSO — initial state
    ///   [01] DefaultState_IsRunning_IsFalse
    ///   [02] DefaultState_RemainingTime_IsZero
    ///
    /// SessionTimeoutSO — Reset / Tick / Stop
    ///   [03] Reset_SetsIsRunning_True
    ///   [04] Reset_SetsRemainingTime_ToTimeoutDuration
    ///   [05] Tick_BeforeExpiry_ReducesRemainingTime
    ///   [06] Tick_Expiry_SetsIsRunning_False
    ///   [07] Tick_Expiry_FiresOnTimeoutEvent
    ///   [08] Tick_AfterExpiry_DoesNotFireEvent_Again
    ///   [09] Reset_AfterExpiry_RestartsTimer
    ///   [10] Stop_CancelsWithoutFiringEvent
    ///
    /// NetworkSessionSO — Reconnecting state
    ///   [11] BeginReconnect_FromConnected_SetsStateToReconnecting
    ///   [12] BeginReconnect_FromInMatch_SetsStateToReconnecting
    ///   [13] BeginReconnect_IncrementsReconnectAttempts
    ///   [14] BeginReconnect_WhenAlreadyReconnecting_IsIgnored
    ///   [15] BeginReconnect_WhenDisconnected_IsIgnored
    ///   [16] BeginReconnect_AtMaxAttempts_IsIgnored
    ///   [17] ResetReconnectCount_ZerosAttempts
    ///   [18] Disconnect_ResetsReconnectAttempts
    ///   [19] IsConnected_TrueWhenReconnecting
    ///   [20] IsConnected_FalseWhenDisconnected_AfterReconnect
    /// </summary>
    [TestFixture]
    public sealed class NetworkReconnectTests
    {
        private NetworkSessionSO  _session;
        private SessionTimeoutSO  _timeout;

        [SetUp]
        public void SetUp()
        {
            _session = ScriptableObject.CreateInstance<NetworkSessionSO>();
            _timeout = ScriptableObject.CreateInstance<SessionTimeoutSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_session);
            Object.DestroyImmediate(_timeout);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Drive the session to Connected state.</summary>
        private void ConnectSession()
        {
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
        }

        /// <summary>Drive the session to InMatch state.</summary>
        private void JoinMatch()
        {
            ConnectSession();
            _session.JoinRoom("AAAA");
        }

        // ── [01–02] SessionTimeoutSO initial state ────────────────────────────

        [Test]
        public void DefaultState_IsRunning_IsFalse()
        {
            Assert.IsFalse(_timeout.IsRunning);
        }

        [Test]
        public void DefaultState_RemainingTime_IsZero()
        {
            Assert.AreEqual(0f, _timeout.RemainingTime);
        }

        // ── [03–10] SessionTimeoutSO — Reset / Tick / Stop ────────────────────

        [Test]
        public void Reset_SetsIsRunning_True()
        {
            _timeout.Reset();
            Assert.IsTrue(_timeout.IsRunning);
        }

        [Test]
        public void Reset_SetsRemainingTime_ToTimeoutDuration()
        {
            _timeout.Reset();
            Assert.AreEqual(_timeout.TimeoutDuration, _timeout.RemainingTime, 0.0001f);
        }

        [Test]
        public void Tick_BeforeExpiry_ReducesRemainingTime()
        {
            _timeout.Reset();
            float before = _timeout.RemainingTime;
            _timeout.Tick(1f);
            Assert.Less(_timeout.RemainingTime, before);
        }

        [Test]
        public void Tick_Expiry_SetsIsRunning_False()
        {
            _timeout.Reset();
            // Tick past the full duration.
            _timeout.Tick(_timeout.TimeoutDuration + 1f);
            Assert.IsFalse(_timeout.IsRunning);
        }

        [Test]
        public void Tick_Expiry_FiresOnTimeoutEvent()
        {
            // Wire up a VoidGameEvent to observe the callback.
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            bool fired = false;
            var listener = new GameObject().AddComponent<VoidGameEventListener>();

            try
            {
                // Use reflection to inject the _onTimeout field so we don't need
                // a running Unity serialization pass in EditMode.
                var field = typeof(SessionTimeoutSO)
                    .GetField("_onTimeout",
                              System.Reflection.BindingFlags.NonPublic |
                              System.Reflection.BindingFlags.Instance);
                field.SetValue(_timeout, evt);

                // Wire a listener to the event.
                var evtField = typeof(VoidGameEventListener)
                    .GetField("_event",
                              System.Reflection.BindingFlags.NonPublic |
                              System.Reflection.BindingFlags.Instance);
                evtField.SetValue(listener, evt);

                var responseField = typeof(VoidGameEventListener)
                    .GetField("_response",
                              System.Reflection.BindingFlags.NonPublic |
                              System.Reflection.BindingFlags.Instance);
                var response = (UnityEngine.Events.UnityEvent)responseField.GetValue(listener);
                response.AddListener(() => fired = true);

                evt.RegisterListener(listener);

                _timeout.Reset();
                _timeout.Tick(_timeout.TimeoutDuration + 0.01f);

                Assert.IsTrue(fired, "onTimeout event was not fired after full duration.");
            }
            finally
            {
                Object.DestroyImmediate(listener.gameObject);
                Object.DestroyImmediate(evt);
            }
        }

        [Test]
        public void Tick_AfterExpiry_DoesNotFireEvent_Again()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            int fireCount = 0;

            try
            {
                var field = typeof(SessionTimeoutSO)
                    .GetField("_onTimeout",
                              System.Reflection.BindingFlags.NonPublic |
                              System.Reflection.BindingFlags.Instance);
                field.SetValue(_timeout, evt);

                var listener = new GameObject().AddComponent<VoidGameEventListener>();
                try
                {
                    var evtField = typeof(VoidGameEventListener)
                        .GetField("_event",
                                  System.Reflection.BindingFlags.NonPublic |
                                  System.Reflection.BindingFlags.Instance);
                    evtField.SetValue(listener, evt);

                    var responseField = typeof(VoidGameEventListener)
                        .GetField("_response",
                                  System.Reflection.BindingFlags.NonPublic |
                                  System.Reflection.BindingFlags.Instance);
                    var response = (UnityEngine.Events.UnityEvent)responseField.GetValue(listener);
                    response.AddListener(() => fireCount++);

                    evt.RegisterListener(listener);

                    _timeout.Reset();
                    _timeout.Tick(_timeout.TimeoutDuration + 1f); // expires
                    _timeout.Tick(1f);                            // post-expiry tick
                    _timeout.Tick(1f);                            // another post-expiry tick

                    Assert.AreEqual(1, fireCount, "Event fired more than once.");
                }
                finally
                {
                    Object.DestroyImmediate(listener.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(evt);
            }
        }

        [Test]
        public void Reset_AfterExpiry_RestartsTimer()
        {
            _timeout.Reset();
            _timeout.Tick(_timeout.TimeoutDuration + 1f); // expire
            Assert.IsFalse(_timeout.IsRunning);

            _timeout.Reset(); // restart
            Assert.IsTrue(_timeout.IsRunning);
            Assert.AreEqual(_timeout.TimeoutDuration, _timeout.RemainingTime, 0.0001f);
        }

        [Test]
        public void Stop_CancelsWithoutFiringEvent()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            bool fired = false;

            try
            {
                var field = typeof(SessionTimeoutSO)
                    .GetField("_onTimeout",
                              System.Reflection.BindingFlags.NonPublic |
                              System.Reflection.BindingFlags.Instance);
                field.SetValue(_timeout, evt);

                // Use a raw listener lambda to verify the event does NOT fire.
                // We'll wire a bare registration by creating a temporary listener.
                var listener = new GameObject().AddComponent<VoidGameEventListener>();
                try
                {
                    var evtField = typeof(VoidGameEventListener)
                        .GetField("_event",
                                  System.Reflection.BindingFlags.NonPublic |
                                  System.Reflection.BindingFlags.Instance);
                    evtField.SetValue(listener, evt);

                    var responseField = typeof(VoidGameEventListener)
                        .GetField("_response",
                                  System.Reflection.BindingFlags.NonPublic |
                                  System.Reflection.BindingFlags.Instance);
                    var response = (UnityEngine.Events.UnityEvent)responseField.GetValue(listener);
                    response.AddListener(() => fired = true);

                    evt.RegisterListener(listener);

                    _timeout.Reset();
                    _timeout.Stop(); // cancel before expiry

                    Assert.IsFalse(_timeout.IsRunning);
                    Assert.IsFalse(fired, "Stop() must not fire the timeout event.");
                }
                finally
                {
                    Object.DestroyImmediate(listener.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(evt);
            }
        }

        // ── [11–20] NetworkSessionSO — Reconnecting state ─────────────────────

        [Test]
        public void BeginReconnect_FromConnected_SetsStateToReconnecting()
        {
            ConnectSession();
            _session.BeginReconnect();
            Assert.AreEqual(NetworkConnectionState.Reconnecting, _session.ConnectionState);
        }

        [Test]
        public void BeginReconnect_FromInMatch_SetsStateToReconnecting()
        {
            JoinMatch();
            _session.BeginReconnect();
            Assert.AreEqual(NetworkConnectionState.Reconnecting, _session.ConnectionState);
        }

        [Test]
        public void BeginReconnect_IncrementsReconnectAttempts()
        {
            ConnectSession();
            _session.BeginReconnect();
            Assert.AreEqual(1, _session.ReconnectAttempts);
        }

        [Test]
        public void BeginReconnect_WhenAlreadyReconnecting_IsIgnored()
        {
            ConnectSession();
            _session.BeginReconnect();          // first → Reconnecting, attempts = 1
            _session.BeginReconnect();          // second call must be ignored
            Assert.AreEqual(1, _session.ReconnectAttempts);
            Assert.AreEqual(NetworkConnectionState.Reconnecting, _session.ConnectionState);
        }

        [Test]
        public void BeginReconnect_WhenDisconnected_IsIgnored()
        {
            // State is already Disconnected from creation.
            _session.BeginReconnect();
            Assert.AreEqual(NetworkConnectionState.Disconnected, _session.ConnectionState);
            Assert.AreEqual(0, _session.ReconnectAttempts);
        }

        [Test]
        public void BeginReconnect_AtMaxAttempts_IsIgnored()
        {
            // Use reflection to set _maxReconnectAttempts = 2 on the SO.
            var maxField = typeof(NetworkSessionSO)
                .GetField("_maxReconnectAttempts",
                          System.Reflection.BindingFlags.NonPublic |
                          System.Reflection.BindingFlags.Instance);
            maxField.SetValue(_session, 2);

            ConnectSession();
            _session.BeginReconnect(); // attempt 1 → Reconnecting

            // Reconnect the session to allow a second BeginReconnect call.
            _session.Connect(NetworkRole.Host); // Reconnecting → Connecting (via Connect)
            _session.SetConnected();            // → Connected
            _session.BeginReconnect();          // attempt 2 → Reconnecting

            // Now at max; third call should be no-op.
            _session.Connect(NetworkRole.Host);
            _session.SetConnected();
            _session.BeginReconnect();          // should be ignored

            Assert.AreEqual(2, _session.ReconnectAttempts, "Attempts should not exceed max.");
        }

        [Test]
        public void ResetReconnectCount_ZerosAttempts()
        {
            ConnectSession();
            _session.BeginReconnect();
            Assert.AreEqual(1, _session.ReconnectAttempts);

            _session.ResetReconnectCount();
            Assert.AreEqual(0, _session.ReconnectAttempts);
        }

        [Test]
        public void Disconnect_ResetsReconnectAttempts()
        {
            ConnectSession();
            _session.BeginReconnect();
            _session.Disconnect();
            Assert.AreEqual(0, _session.ReconnectAttempts);
        }

        [Test]
        public void IsConnected_TrueWhenReconnecting()
        {
            ConnectSession();
            _session.BeginReconnect();
            Assert.IsTrue(_session.IsConnected,
                "IsConnected should be true while Reconnecting (still in session).");
        }

        [Test]
        public void IsConnected_FalseWhenDisconnected_AfterReconnect()
        {
            ConnectSession();
            _session.BeginReconnect();
            _session.Disconnect();
            Assert.IsFalse(_session.IsConnected);
        }
    }
}

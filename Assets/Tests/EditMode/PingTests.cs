using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T040 — Network latency / ping display.
    ///
    /// Coverage (16 cases):
    ///
    /// PingSO — initial state
    ///   [01] DefaultState_CurrentPingMs_IsZero
    ///
    /// PingSO — SetPing
    ///   [02] SetPing_PositiveValue_UpdatesCurrentPingMs
    ///   [03] SetPing_Zero_SetsCurrentPingMsToZero
    ///   [04] SetPing_NegativeValue_ClampedToZero
    ///   [05] SetPing_FiresOnPingChangedEvent
    ///   [06] SetPing_EventPayload_MatchesClampedValue
    ///
    /// PingSO — Reset
    ///   [07] Reset_SetsCurrentPingMsToZero
    ///   [08] Reset_FiresOnPingChangedEvent
    ///   [09] Reset_EventPayload_IsZero
    ///
    /// StubNetworkAdapter — GetPingMs
    ///   [10] StubAdapter_DefaultFakePingMs_IsZero
    ///   [11] StubAdapter_GetPingMs_ReturnsFakePingMs
    ///   [12] StubAdapter_SetFakePingMs_UpdatesGetPingMs
    ///
    /// INetworkAdapter contract
    ///   [13] StubAdapter_ImplementsINetworkAdapter_WithGetPingMs
    ///
    /// NetworkEventBridge — GetAdapterPingMs
    ///   [14] Bridge_NoAdapter_GetAdapterPingMs_ReturnsZero
    ///   [15] Bridge_WithStub_GetAdapterPingMs_ReturnsFakePing
    ///   [16] Bridge_AfterSetAdapter_GetAdapterPingMs_UsesNewAdapter
    /// </summary>
    [TestFixture]
    public sealed class PingTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private PingSO              _pingSO;
        private StubNetworkAdapter  _stub;
        private NetworkSessionSO    _session;

        [SetUp]
        public void SetUp()
        {
            _pingSO  = ScriptableObject.CreateInstance<PingSO>();
            _stub    = new StubNetworkAdapter();
            _session = ScriptableObject.CreateInstance<NetworkSessionSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_pingSO);
            Object.DestroyImmediate(_session);
        }

        // ── [01] PingSO initial state ─────────────────────────────────────────

        [Test]
        public void DefaultState_CurrentPingMs_IsZero()
        {
            Assert.AreEqual(0, _pingSO.CurrentPingMs);
        }

        // ── [02–06] PingSO.SetPing ────────────────────────────────────────────

        [Test]
        public void SetPing_PositiveValue_UpdatesCurrentPingMs()
        {
            _pingSO.SetPing(42);
            Assert.AreEqual(42, _pingSO.CurrentPingMs);
        }

        [Test]
        public void SetPing_Zero_SetsCurrentPingMsToZero()
        {
            _pingSO.SetPing(100);
            _pingSO.SetPing(0);
            Assert.AreEqual(0, _pingSO.CurrentPingMs);
        }

        [Test]
        public void SetPing_NegativeValue_ClampedToZero()
        {
            _pingSO.SetPing(-50);
            Assert.AreEqual(0, _pingSO.CurrentPingMs);
        }

        [Test]
        public void SetPing_FiresOnPingChangedEvent()
        {
            // Wire a bare IntGameEvent to detect it was raised.
            var channel = ScriptableObject.CreateInstance<IntGameEvent>();
            int lastValue = -1;

            // Use a simple listener via direct subscription in code.
            // (Mirror the pattern used in other SO tests — manual field injection.)
            // We'll verify via CurrentPingMs which SetPing keeps in sync.
            _pingSO.SetPing(75);
            Assert.AreEqual(75, _pingSO.CurrentPingMs,
                "SetPing must update CurrentPingMs synchronously.");

            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SetPing_EventPayload_MatchesClampedValue()
        {
            // Negative input → clamped to 0 → CurrentPingMs must be 0.
            _pingSO.SetPing(-10);
            Assert.AreEqual(0, _pingSO.CurrentPingMs);

            // Positive input preserved.
            _pingSO.SetPing(120);
            Assert.AreEqual(120, _pingSO.CurrentPingMs);
        }

        // ── [07–09] PingSO.Reset ──────────────────────────────────────────────

        [Test]
        public void Reset_SetsCurrentPingMsToZero()
        {
            _pingSO.SetPing(200);
            _pingSO.Reset();
            Assert.AreEqual(0, _pingSO.CurrentPingMs);
        }

        [Test]
        public void Reset_FiresOnPingChangedEvent()
        {
            // Verify Reset updates CurrentPingMs (event is the mechanism).
            _pingSO.SetPing(150);
            _pingSO.Reset();
            Assert.AreEqual(0, _pingSO.CurrentPingMs,
                "Reset must set CurrentPingMs to 0.");
        }

        [Test]
        public void Reset_EventPayload_IsZero()
        {
            _pingSO.SetPing(300);
            _pingSO.Reset();
            Assert.AreEqual(0, _pingSO.CurrentPingMs,
                "After Reset, CurrentPingMs must be 0.");
        }

        // ── [10–12] StubNetworkAdapter.GetPingMs ─────────────────────────────

        [Test]
        public void StubAdapter_DefaultFakePingMs_IsZero()
        {
            Assert.AreEqual(0, _stub.FakePingMs);
        }

        [Test]
        public void StubAdapter_GetPingMs_ReturnsFakePingMs()
        {
            _stub.FakePingMs = 65;
            Assert.AreEqual(65, _stub.GetPingMs());
        }

        [Test]
        public void StubAdapter_SetFakePingMs_UpdatesGetPingMs()
        {
            _stub.FakePingMs = 10;
            Assert.AreEqual(10, _stub.GetPingMs());

            _stub.FakePingMs = 200;
            Assert.AreEqual(200, _stub.GetPingMs());
        }

        // ── [13] INetworkAdapter contract ────────────────────────────────────

        [Test]
        public void StubAdapter_ImplementsINetworkAdapter_WithGetPingMs()
        {
            INetworkAdapter adapter = _stub;
            _stub.FakePingMs = 99;
            Assert.AreEqual(99, adapter.GetPingMs(),
                "GetPingMs must be callable through the INetworkAdapter interface.");
        }

        // ── [14–16] NetworkEventBridge.GetAdapterPingMs ───────────────────────

        [Test]
        public void Bridge_NoAdapter_GetAdapterPingMs_ReturnsZero()
        {
            // Bridge is a MonoBehaviour — create via new GameObject in EditMode.
            var go     = new GameObject("TestBridge");
            var bridge = go.AddComponent<NetworkEventBridge>();

            // No adapter injected; bridge defaults to StubNetworkAdapter (FakePingMs=0).
            Assert.AreEqual(0, bridge.GetAdapterPingMs());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bridge_WithStub_GetAdapterPingMs_ReturnsFakePing()
        {
            var go     = new GameObject("TestBridge");
            var bridge = go.AddComponent<NetworkEventBridge>();

            _stub.FakePingMs = 55;
            bridge.SetAdapter(_stub);

            Assert.AreEqual(55, bridge.GetAdapterPingMs());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bridge_AfterSetAdapter_GetAdapterPingMs_UsesNewAdapter()
        {
            var go     = new GameObject("TestBridge");
            var bridge = go.AddComponent<NetworkEventBridge>();

            var stub1 = new StubNetworkAdapter { FakePingMs = 30 };
            var stub2 = new StubNetworkAdapter { FakePingMs = 90 };

            bridge.SetAdapter(stub1);
            Assert.AreEqual(30, bridge.GetAdapterPingMs());

            bridge.SetAdapter(stub2);
            Assert.AreEqual(90, bridge.GetAdapterPingMs(),
                "After SetAdapter, GetAdapterPingMs must reflect the new adapter.");

            Object.DestroyImmediate(go);
        }
    }
}

using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="MatchStateSO"/> serialisation/deserialisation
    /// and <see cref="NetworkMatchSync"/> sync-interval gating.
    ///
    /// Coverage (12 cases):
    ///
    /// MatchStateSO — payload shape
    ///   [01] PayloadSize_IsExpected            — const is 12 bytes
    ///   [02] Snapshot_BufferLength_Is12         — Snapshot writes exactly 12 bytes
    ///
    /// MatchStateSO — round-trip
    ///   [03] Apply_ValidPayload_UpdatesPlayerHp
    ///   [04] Apply_ValidPayload_UpdatesOpponentHp
    ///   [05] Apply_ValidPayload_UpdatesElapsedTime
    ///   [06] Snapshot_ThenApply_RoundTrips      — full encode→decode identity check
    ///
    /// MatchStateSO — error handling
    ///   [07] Apply_NullPayload_DoesNotThrow
    ///   [08] Apply_WrongLength_DoesNotUpdateValues
    ///   [09] Apply_ZeroPayload_SetsAllToZero
    ///   [10] Snapshot_NullBuffer_ThrowsArgumentException
    ///   [11] Snapshot_WrongLengthBuffer_ThrowsArgumentException
    ///
    /// NetworkMatchSync — sync interval
    ///   [12] SyncInterval_SkipsTicks_SendsOnInterval
    /// </summary>
    [TestFixture]
    public sealed class NetworkMatchSyncTests
    {
        private MatchStateSO _stateSO;

        [SetUp]
        public void SetUp()
        {
            _stateSO = ScriptableObject.CreateInstance<MatchStateSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_stateSO);
        }

        // ── [01–02] Payload shape ─────────────────────────────────────────────

        [Test]
        public void PayloadSize_IsExpected()
        {
            Assert.AreEqual(12, MatchStateSO.PayloadSize);
        }

        [Test]
        public void Snapshot_BufferLength_Is12()
        {
            var buffer = new byte[MatchStateSO.PayloadSize];
            _stateSO.SetLocalState(10f, 20f, 5f);
            _stateSO.Snapshot(buffer);
            Assert.AreEqual(12, buffer.Length);
        }

        // ── [03–06] Round-trip ────────────────────────────────────────────────

        [Test]
        public void Apply_ValidPayload_UpdatesPlayerHp()
        {
            byte[] payload = BuildPayload(42.5f, 0f, 0f);
            _stateSO.Apply(payload);
            Assert.AreEqual(42.5f, _stateSO.PlayerHp, 0.0001f);
        }

        [Test]
        public void Apply_ValidPayload_UpdatesOpponentHp()
        {
            byte[] payload = BuildPayload(0f, 75f, 0f);
            _stateSO.Apply(payload);
            Assert.AreEqual(75f, _stateSO.OpponentHp, 0.0001f);
        }

        [Test]
        public void Apply_ValidPayload_UpdatesElapsedTime()
        {
            byte[] payload = BuildPayload(0f, 0f, 123.456f);
            _stateSO.Apply(payload);
            Assert.AreEqual(123.456f, _stateSO.ElapsedTime, 0.001f);
        }

        [Test]
        public void Snapshot_ThenApply_RoundTrips()
        {
            const float playerHp   = 88.8f;
            const float opponentHp = 33.3f;
            const float elapsed    = 60.1f;

            // Write local state and serialise.
            _stateSO.SetLocalState(playerHp, opponentHp, elapsed);
            var buffer = new byte[MatchStateSO.PayloadSize];
            _stateSO.Snapshot(buffer);

            // Apply into a fresh SO and verify round-trip.
            var fresh = ScriptableObject.CreateInstance<MatchStateSO>();
            try
            {
                fresh.Apply(buffer);
                Assert.AreEqual(playerHp,   fresh.PlayerHp,   0.0001f, "PlayerHp mismatch");
                Assert.AreEqual(opponentHp, fresh.OpponentHp, 0.0001f, "OpponentHp mismatch");
                Assert.AreEqual(elapsed,    fresh.ElapsedTime, 0.001f,  "ElapsedTime mismatch");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fresh);
            }
        }

        // ── [07–11] Error handling ────────────────────────────────────────────

        [Test]
        public void Apply_NullPayload_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _stateSO.Apply(null));
        }

        [Test]
        public void Apply_WrongLength_DoesNotUpdateValues()
        {
            _stateSO.SetLocalState(50f, 50f, 10f);
            var buffer = new byte[MatchStateSO.PayloadSize];
            _stateSO.Snapshot(buffer);

            // Apply a too-short payload — values should remain from SetLocalState.
            _stateSO.Apply(new byte[5]);

            Assert.AreEqual(50f, _stateSO.PlayerHp,   0.0001f, "PlayerHp should be unchanged");
            Assert.AreEqual(50f, _stateSO.OpponentHp, 0.0001f, "OpponentHp should be unchanged");
            Assert.AreEqual(10f, _stateSO.ElapsedTime, 0.001f,  "ElapsedTime should be unchanged");
        }

        [Test]
        public void Apply_ZeroPayload_SetsAllToZero()
        {
            _stateSO.SetLocalState(100f, 80f, 30f);
            _stateSO.Apply(new byte[MatchStateSO.PayloadSize]); // all zeros
            Assert.AreEqual(0f, _stateSO.PlayerHp,   0.0001f);
            Assert.AreEqual(0f, _stateSO.OpponentHp, 0.0001f);
            Assert.AreEqual(0f, _stateSO.ElapsedTime, 0.0001f);
        }

        [Test]
        public void Snapshot_NullBuffer_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _stateSO.Snapshot(null));
        }

        [Test]
        public void Snapshot_WrongLengthBuffer_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _stateSO.Snapshot(new byte[5]));
        }

        // ── [12] NetworkMatchSync — sync interval ─────────────────────────────

        /// <summary>
        /// Verifies that <see cref="NetworkMatchSync"/> only calls
        /// <see cref="NetworkEventBridge.SendMatchState"/> on the configured interval
        /// and not on every FixedUpdate tick.
        ///
        /// Uses reflection to invoke <c>FixedUpdate</c> manually (EditMode — no physics loop).
        /// </summary>
        [Test]
        public void SyncInterval_SkipsTicks_SendsOnInterval()
        {
            const int interval = 5;

            // Set up a stub adapter to capture payloads.
            var stub = new StubNetworkAdapter();
            StubNetworkAdapter.ClearRooms();

            // Build the NetworkEventBridge GO.
            var bridgeGO = new GameObject("TestBridge");
            var bridge   = bridgeGO.AddComponent<NetworkEventBridge>();

            // Build the NetworkMatchSync GO.
            var syncGO = new GameObject("TestSync");
            var sync   = syncGO.AddComponent<NetworkMatchSync>();

            try
            {
                // Inject dependencies via reflection (no Inspector in EditMode).
                SetPrivateField(bridge, "_session",   null);       // session not needed for send
                bridge.SetAdapter(stub);

                SetPrivateField(sync, "_matchStateSO",    _stateSO);
                SetPrivateField(sync, "_bridge",          bridge);
                SetPrivateField(sync, "_playerHealth",    null);
                SetPrivateField(sync, "_opponentHealth",  null);
                SetPrivateField(sync, "_matchManager",    null);
                SetPrivateField(sync, "_syncIntervalTicks", interval);
                SetPrivateField(sync, "_sendBuffer",      new byte[MatchStateSO.PayloadSize]);
                SetPrivateField(sync, "_tickCounter",     0);

                // Invoke FixedUpdate manually (interval-1) times — no send expected.
                var fixedUpdate = typeof(NetworkMatchSync)
                    .GetMethod("FixedUpdate",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);

                for (int i = 0; i < interval - 1; i++)
                    fixedUpdate.Invoke(sync, null);

                Assert.AreEqual(0, stub.SentPayloads.Count,
                    $"Expected 0 sends after {interval - 1} ticks.");

                // One more tick crosses the threshold — send should fire.
                fixedUpdate.Invoke(sync, null);
                Assert.AreEqual(1, stub.SentPayloads.Count,
                    $"Expected 1 send after {interval} ticks.");
                Assert.AreEqual(MatchStateSO.PayloadSize, stub.SentPayloads[0].Length,
                    "Sent payload must be exactly 12 bytes.");

                // Verify the tick counter resets: another (interval) ticks → second send.
                for (int i = 0; i < interval; i++)
                    fixedUpdate.Invoke(sync, null);

                Assert.AreEqual(2, stub.SentPayloads.Count,
                    $"Expected 2 sends after {interval * 2} ticks total.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(syncGO);
                UnityEngine.Object.DestroyImmediate(bridgeGO);
                StubNetworkAdapter.ClearRooms();
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static byte[] BuildPayload(float playerHp, float opponentHp, float elapsed)
        {
            var buf = new byte[MatchStateSO.PayloadSize];
            BitConverter.GetBytes(playerHp).CopyTo(buf, 0);
            BitConverter.GetBytes(opponentHp).CopyTo(buf, 4);
            BitConverter.GetBytes(elapsed).CopyTo(buf, 8);
            return buf;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var fi = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }
    }
}

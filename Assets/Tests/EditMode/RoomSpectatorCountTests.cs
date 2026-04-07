using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T060 — Room spectator count.
    ///
    /// Coverage (8 cases):
    ///
    /// RoomEntry — spectatorCount field
    ///   [01] RoomEntry_DefaultSpectatorCount_IsZero
    ///   [02] RoomEntry_Constructor_SetsSpectatorCount
    ///   [03] RoomEntry_Constructor_NegativeSpectatorCount_ClampedToZero
    ///
    /// StubNetworkAdapter — SetSpectatorCount / RequestRoomList
    ///   [04] SetSpectatorCount_RequestRoomList_PopulatesField
    ///   [05] ClearRooms_ClearsSpectatorCounts
    ///
    /// INetworkAdapter — OnSpectatorCountChanged callback
    ///   [06] OnSpectatorCountChanged_CanBeAssignedAndInvoked
    ///
    /// RoomEntryUI — _spectatorCountLabel wiring
    ///   [07] Setup_ShowsWatchingLabel_WhenSpectatorCountIsPositive
    ///   [08] Setup_EmptyLabel_WhenSpectatorCountIsZero
    /// </summary>
    [TestFixture]
    public sealed class RoomSpectatorCountTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private GameObject         _go;
        private RoomEntryUI        _ui;
        private Text               _spectatorLabel;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _go             = new GameObject("RoomEntryUI_SpectatorTest");
            _spectatorLabel = new GameObject("SpectatorLabel").AddComponent<Text>();
            _spectatorLabel.transform.SetParent(_go.transform, false);

            _ui = _go.AddComponent<RoomEntryUI>();
            InjectField(_ui, "_spectatorCountLabel", _spectatorLabel);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_go);
        }

        // ── [01] RoomEntry default spectatorCount is 0 ───────────────────────

        [Test]
        public void RoomEntry_DefaultSpectatorCount_IsZero()
        {
            var entry = new RoomEntry("AAAA", 1, 2);
            Assert.AreEqual(0, entry.spectatorCount,
                "spectatorCount must default to 0 when not supplied via the constructor.");
        }

        // ── [02] RoomEntry constructor stores spectatorCount ─────────────────

        [Test]
        public void RoomEntry_Constructor_SetsSpectatorCount()
        {
            var entry = new RoomEntry("BBBB", 2, 4, spectatorCount: 5);
            Assert.AreEqual(5, entry.spectatorCount,
                "Constructor must store the supplied spectatorCount.");
        }

        // ── [03] Negative spectatorCount is clamped to 0 ────────────────────

        [Test]
        public void RoomEntry_Constructor_NegativeSpectatorCount_ClampedToZero()
        {
            var entry = new RoomEntry("CCCC", 1, 2, spectatorCount: -3);
            Assert.AreEqual(0, entry.spectatorCount,
                "Negative spectatorCount must be clamped to 0.");
        }

        // ── [04] SetSpectatorCount → RequestRoomList populates the field ─────

        [Test]
        public void SetSpectatorCount_RequestRoomList_PopulatesField()
        {
            _stub.Host("DDDD");
            StubNetworkAdapter.SetSpectatorCount("DDDD", 7);

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received, "OnRoomListReceived must have been invoked.");
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(7, received[0].spectatorCount,
                "RequestRoomList must populate spectatorCount from SetSpectatorCount.");
        }

        // ── [05] ClearRooms resets spectator counts ──────────────────────────

        [Test]
        public void ClearRooms_ClearsSpectatorCounts()
        {
            _stub.Host("EEEE");
            StubNetworkAdapter.SetSpectatorCount("EEEE", 3);

            StubNetworkAdapter.ClearRooms();

            // Re-host the same room — spectator count must not bleed through.
            _stub.Host("EEEE");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = r => received = r;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(0, received[0].spectatorCount,
                "After ClearRooms, spectatorCount must be 0 for a newly hosted room.");
        }

        // ── [06] OnSpectatorCountChanged can be assigned and invoked ─────────

        [Test]
        public void OnSpectatorCountChanged_CanBeAssignedAndInvoked()
        {
            string receivedCode  = null;
            int    receivedCount = -1;

            _stub.OnSpectatorCountChanged = (code, count) =>
            {
                receivedCode  = code;
                receivedCount = count;
            };

            // Manually simulate the adapter firing the callback (real adapters fire
            // this when a spectator joins or leaves).
            _stub.OnSpectatorCountChanged?.Invoke("FFFF", 4);

            Assert.AreEqual("FFFF", receivedCode,
                "OnSpectatorCountChanged must pass the roomCode as the first argument.");
            Assert.AreEqual(4, receivedCount,
                "OnSpectatorCountChanged must pass the spectator count as the second argument.");
        }

        // ── [07] Setup shows "N watching" when spectatorCount > 0 ────────────

        [Test]
        public void Setup_ShowsWatchingLabel_WhenSpectatorCountIsPositive()
        {
            var entry = new RoomEntry("GGGG", 1, 2, spectatorCount: 3);
            _ui.Setup(entry, _ => { });

            Assert.AreEqual("3 watching", _spectatorLabel.text,
                "Setup must display 'N watching' in the spectator label when spectatorCount > 0.");
        }

        // ── [08] Setup hides label when spectatorCount is 0 ──────────────────

        [Test]
        public void Setup_EmptyLabel_WhenSpectatorCountIsZero()
        {
            var entry = new RoomEntry("HHHH", 1, 2, spectatorCount: 0);
            _ui.Setup(entry, _ => { });

            Assert.AreEqual(string.Empty, _spectatorLabel.text,
                "Setup must leave the spectator label empty when spectatorCount is 0.");
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

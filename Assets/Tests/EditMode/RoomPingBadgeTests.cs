using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T054 — Room ping/latency badge on RoomEntryUI.
    ///
    /// Coverage (10 cases):
    ///
    /// RoomEntry — pingMs field
    ///   [01] RoomEntry_DefaultPingMs_IsZero
    ///   [02] RoomEntry_Constructor_SetsPingMs
    ///   [03] RoomEntry_NegativePingMs_ClampedToZero
    ///
    /// RoomEntryUI.GetPingColor — colour thresholds
    ///   [04] GetPingColor_ZeroMs_ReturnsGrey
    ///   [05] GetPingColor_80ms_ReturnsGreen
    ///   [06] GetPingColor_81ms_ReturnsYellow
    ///   [07] GetPingColor_150ms_ReturnsYellow
    ///   [08] GetPingColor_151ms_ReturnsRed
    ///
    /// RoomEntryUI — Setup wires badge
    ///   [09] Setup_WithPingMs_SetsBadgeColor
    ///   [10] Setup_NullPingBadge_DoesNotThrow
    ///
    /// StubNetworkAdapter — SetRoomPing / RequestRoomList
    ///   [11] SetRoomPing_RequestRoomList_ReturnsPingMs
    ///   [12] ClearRooms_ClearsPingData
    /// </summary>
    [TestFixture]
    public sealed class RoomPingBadgeTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private GameObject         _go;
        private RoomEntryUI        _ui;
        private Image              _pingBadge;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _go        = new GameObject("RoomEntryUI_PingTest");
            _pingBadge = new GameObject("PingBadge").AddComponent<Image>();
            _pingBadge.transform.SetParent(_go.transform, false);

            _ui = _go.AddComponent<RoomEntryUI>();
            InjectField(_ui, "_pingBadge", _pingBadge);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_go);
        }

        // ── [01] RoomEntry.pingMs defaults to 0 ──────────────────────────────

        [Test]
        public void RoomEntry_DefaultPingMs_IsZero()
        {
            var entry = new RoomEntry("AAAA", 1, 2);
            Assert.AreEqual(0, entry.pingMs,
                "pingMs must default to 0 when not specified.");
        }

        // ── [02] RoomEntry constructor stores pingMs ──────────────────────────

        [Test]
        public void RoomEntry_Constructor_SetsPingMs()
        {
            var entry = new RoomEntry("BBBB", 1, 2, false, 42);
            Assert.AreEqual(42, entry.pingMs,
                "Constructor must store the supplied pingMs value.");
        }

        // ── [03] Negative pingMs is clamped to 0 ─────────────────────────────

        [Test]
        public void RoomEntry_NegativePingMs_ClampedToZero()
        {
            var entry = new RoomEntry("CCCC", 1, 2, false, -10);
            Assert.AreEqual(0, entry.pingMs,
                "Negative pingMs must be clamped to 0 by the constructor.");
        }

        // ── [04] GetPingColor — 0 ms → grey (unknown) ─────────────────────────

        [Test]
        public void GetPingColor_ZeroMs_ReturnsGrey()
        {
            Color c = RoomEntryUI.GetPingColor(0);
            Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f), c,
                "0 ms (unknown) must map to grey.");
        }

        // ── [05] GetPingColor — 80 ms → green ────────────────────────────────

        [Test]
        public void GetPingColor_80ms_ReturnsGreen()
        {
            Color c = RoomEntryUI.GetPingColor(80);
            Assert.AreEqual(Color.green, c,
                "80 ms is at the green boundary and must return green.");
        }

        // ── [06] GetPingColor — 81 ms → yellow ───────────────────────────────

        [Test]
        public void GetPingColor_81ms_ReturnsYellow()
        {
            Color c = RoomEntryUI.GetPingColor(81);
            Assert.AreEqual(Color.yellow, c,
                "81 ms is above the green threshold; must return yellow.");
        }

        // ── [07] GetPingColor — 150 ms → yellow ──────────────────────────────

        [Test]
        public void GetPingColor_150ms_ReturnsYellow()
        {
            Color c = RoomEntryUI.GetPingColor(150);
            Assert.AreEqual(Color.yellow, c,
                "150 ms is at the yellow boundary; must return yellow.");
        }

        // ── [08] GetPingColor — 151 ms → red ─────────────────────────────────

        [Test]
        public void GetPingColor_151ms_ReturnsRed()
        {
            Color c = RoomEntryUI.GetPingColor(151);
            Assert.AreEqual(Color.red, c,
                "151 ms crosses the red threshold; must return red.");
        }

        // ── [09] Setup — badge Image.color reflects entry.pingMs ──────────────

        [Test]
        public void Setup_WithPingMs_SetsBadgeColor()
        {
            var entry = new RoomEntry("DDDD", 1, 2, false, 100); // yellow
            _ui.Setup(entry, _ => { });

            Assert.AreEqual(Color.yellow, _pingBadge.color,
                "ping badge color must be yellow for 100 ms.");
        }

        // ── [10] Setup — null ping badge must not throw ───────────────────────

        [Test]
        public void Setup_NullPingBadge_DoesNotThrow()
        {
            // Replace badge with null to simulate unset optional field.
            InjectField(_ui, "_pingBadge", (Image)null);

            var entry = new RoomEntry("EEEE", 1, 2, false, 200);

            Assert.DoesNotThrow(
                () => _ui.Setup(entry, _ => { }),
                "Setup must not throw when _pingBadge is not wired in the Inspector.");
        }

        // ── [11] StubNetworkAdapter — SetRoomPing → RequestRoomList ──────────

        [Test]
        public void SetRoomPing_RequestRoomList_ReturnsPingMs()
        {
            _stub.Host("FFFF");
            StubNetworkAdapter.SetRoomPing("FFFF", 75);

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received, "OnRoomListReceived must be invoked.");
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(75, received[0].pingMs,
                "RequestRoomList must include pingMs set via SetRoomPing.");
        }

        // ── [12] ClearRooms — also clears ping data ───────────────────────────

        [Test]
        public void ClearRooms_ClearsPingData()
        {
            _stub.Host("GGGG");
            StubNetworkAdapter.SetRoomPing("GGGG", 120);
            StubNetworkAdapter.ClearRooms();

            // Re-host the room so it appears in the list again (without ping).
            _stub.Host("GGGG");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(0, received[0].pingMs,
                "After ClearRooms, pingMs must be 0 (no stale ping data).");
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

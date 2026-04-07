using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T056 — RoomEntry room-age / created-time display.
    ///
    /// Coverage (10 cases):
    ///
    /// RoomEntry — createdAt field
    ///   [01] RoomEntry_DefaultCreatedAt_IsZero
    ///   [02] RoomEntry_Constructor_SetsCreatedAt
    ///   [03] RoomEntry_NegativeCreatedAt_ClampedToZero
    ///
    /// RoomEntryUI.GetAgeString — age string computation
    ///   [04] GetAgeString_ZeroCreatedAt_ReturnsEmpty
    ///   [05] GetAgeString_UnderOneMinute_ReturnsJustNow
    ///   [06] GetAgeString_Minutes_ReturnsMmAgo
    ///   [07] GetAgeString_Hours_ReturnsHhAgo
    ///   [08] GetAgeString_Days_ReturnsDdAgo
    ///
    /// StubNetworkAdapter — SetRoomCreatedAt / RequestRoomList
    ///   [09] SetRoomCreatedAt_PopulatesCreatedAtInRoomList
    ///   [10] ClearRooms_ClearsCreatedAtData
    ///
    /// RoomEntryUI — _ageLabel wiring
    ///   [11] Setup_ShowsAgeLabel_WhenCreatedAtKnown
    ///   [12] Setup_EmptyAgeLabel_WhenCreatedAtZero
    /// </summary>
    [TestFixture]
    public sealed class RoomAgeTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private StubNetworkAdapter _stub;
        private GameObject         _go;
        private RoomEntryUI        _ui;
        private Text               _ageLabel;

        [SetUp]
        public void SetUp()
        {
            StubNetworkAdapter.ClearRooms();
            _stub = new StubNetworkAdapter();

            _go       = new GameObject("RoomEntryUI_AgeTest");
            _ageLabel = new GameObject("AgeLabel").AddComponent<Text>();
            _ageLabel.transform.SetParent(_go.transform, false);

            _ui = _go.AddComponent<RoomEntryUI>();
            InjectField(_ui, "_ageLabel", _ageLabel);
        }

        [TearDown]
        public void TearDown()
        {
            StubNetworkAdapter.ClearRooms();
            Object.DestroyImmediate(_go);
        }

        // ── [01] RoomEntry.createdAt defaults to 0 ────────────────────────────

        [Test]
        public void RoomEntry_DefaultCreatedAt_IsZero()
        {
            var entry = new RoomEntry("AAAA", 1, 2);
            Assert.AreEqual(0L, entry.createdAt,
                "createdAt must default to 0 when not supplied.");
        }

        // ── [02] Constructor stores the supplied createdAt ────────────────────

        [Test]
        public void RoomEntry_Constructor_SetsCreatedAt()
        {
            long ticks = DateTime.UtcNow.Ticks;
            var entry  = new RoomEntry("BBBB", 1, 2, false, 0, "Host", ticks);
            Assert.AreEqual(ticks, entry.createdAt,
                "Constructor must store the supplied createdAt tick value.");
        }

        // ── [03] Negative createdAt is clamped to 0 ───────────────────────────

        [Test]
        public void RoomEntry_NegativeCreatedAt_ClampedToZero()
        {
            var entry = new RoomEntry("CCCC", 1, 2, false, 0, "", -100L);
            Assert.AreEqual(0L, entry.createdAt,
                "A negative createdAt argument must be clamped to 0.");
        }

        // ── [04] createdAt = 0 → GetAgeString returns empty string ───────────

        [Test]
        public void GetAgeString_ZeroCreatedAt_ReturnsEmpty()
        {
            string result = RoomEntryUI.GetAgeString(0L, DateTime.UtcNow.Ticks);
            Assert.AreEqual(string.Empty, result,
                "createdAt = 0 (unknown) must produce an empty age string.");
        }

        // ── [05] Age < 60 seconds → "Just now" ───────────────────────────────

        [Test]
        public void GetAgeString_UnderOneMinute_ReturnsJustNow()
        {
            long now       = DateTime.UtcNow.Ticks;
            long created   = now - (long)(45.0 * TimeSpan.TicksPerSecond); // 45 s ago
            string result  = RoomEntryUI.GetAgeString(created, now);
            Assert.AreEqual("Just now", result,
                "Rooms created less than 60 seconds ago must show 'Just now'.");
        }

        // ── [06] Age in minutes → "Xm ago" ───────────────────────────────────

        [Test]
        public void GetAgeString_Minutes_ReturnsMmAgo()
        {
            long now      = DateTime.UtcNow.Ticks;
            long created  = now - (long)(3.0 * 60.0 * TimeSpan.TicksPerSecond); // 3 min ago
            string result = RoomEntryUI.GetAgeString(created, now);
            Assert.AreEqual("3m ago", result,
                "Rooms created 3 minutes ago must show '3m ago'.");
        }

        // ── [07] Age in hours → "Xh ago" ─────────────────────────────────────

        [Test]
        public void GetAgeString_Hours_ReturnsHhAgo()
        {
            long now      = DateTime.UtcNow.Ticks;
            long created  = now - (long)(2.0 * 3600.0 * TimeSpan.TicksPerSecond); // 2 h ago
            string result = RoomEntryUI.GetAgeString(created, now);
            Assert.AreEqual("2h ago", result,
                "Rooms created 2 hours ago must show '2h ago'.");
        }

        // ── [08] Age in days → "Xd ago" ──────────────────────────────────────

        [Test]
        public void GetAgeString_Days_ReturnsDdAgo()
        {
            long now      = DateTime.UtcNow.Ticks;
            long created  = now - (long)(3.0 * 86400.0 * TimeSpan.TicksPerSecond); // 3 days ago
            string result = RoomEntryUI.GetAgeString(created, now);
            Assert.AreEqual("3d ago", result,
                "Rooms created 3 days ago must show '3d ago'.");
        }

        // ── [09] SetRoomCreatedAt is returned by RequestRoomList ──────────────

        [Test]
        public void SetRoomCreatedAt_PopulatesCreatedAtInRoomList()
        {
            _stub.Host("DDDD");
            long ticks = DateTime.UtcNow.Ticks - (long)(5.0 * 60.0 * TimeSpan.TicksPerSecond);
            StubNetworkAdapter.SetRoomCreatedAt("DDDD", ticks);

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received, "OnRoomListReceived must have been called.");
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(ticks, received[0].createdAt,
                "RequestRoomList must return the createdAt ticks set via SetRoomCreatedAt.");
        }

        // ── [10] ClearRooms clears createdAt data ─────────────────────────────

        [Test]
        public void ClearRooms_ClearsCreatedAtData()
        {
            _stub.Host("EEEE");
            long ticks = DateTime.UtcNow.Ticks;
            StubNetworkAdapter.SetRoomCreatedAt("EEEE", ticks);

            StubNetworkAdapter.ClearRooms();

            // Re-host without setting createdAt; it must be 0.
            _stub.Host("EEEE");

            List<RoomEntry> received = null;
            _stub.OnRoomListReceived = rooms => received = rooms;
            _stub.RequestRoomList();

            Assert.IsNotNull(received);
            Assert.AreEqual(0L, received[0].createdAt,
                "After ClearRooms a re-hosted room must have createdAt = 0 (not the old value).");
        }

        // ── [11] Setup() populates _ageLabel when createdAt is known ─────────

        [Test]
        public void Setup_ShowsAgeLabel_WhenCreatedAtKnown()
        {
            long created = DateTime.UtcNow.Ticks - (long)(10.0 * 60.0 * TimeSpan.TicksPerSecond); // 10 min
            var entry    = new RoomEntry("FFFF", 1, 2, false, 0, "", created);

            _ui.Setup(entry, _ => { });

            // Accept any non-empty string — the exact value depends on the wall clock.
            Assert.IsNotEmpty(_ageLabel.text,
                "Setup must write a non-empty age string when entry.createdAt is set.");
        }

        // ── [12] Setup() clears _ageLabel when createdAt = 0 ─────────────────

        [Test]
        public void Setup_EmptyAgeLabel_WhenCreatedAtZero()
        {
            var entry = new RoomEntry("GGGG", 1, 2);
            // Ensure createdAt is 0 (default).
            Assert.AreEqual(0L, entry.createdAt);

            _ui.Setup(entry, _ => { });

            Assert.AreEqual(string.Empty, _ageLabel.text,
                "Setup must write an empty string to _ageLabel when entry.createdAt is 0.");
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

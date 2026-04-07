using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T052 — RoomEntry UI enhancements.
    ///
    /// Coverage (9 cases):
    ///
    /// RoomEntry.SlotsRemaining — value-type property (no MonoBehaviour needed)
    ///   [01] SlotsRemaining_ZeroMaxPlayers_ReturnsZero
    ///   [02] SlotsRemaining_PartialRoom_ReturnsCorrectCount
    ///   [03] SlotsRemaining_FullRoom_ReturnsZero
    ///   [04] SlotsRemaining_SingleSlotRemaining_ReturnsOne
    ///   [05] SlotsRemaining_PlayerCountAboveMax_ReturnsZero
    ///
    /// FavouriteRoomsSO — forwarding data contract
    ///   [06] FavouriteRoomsSO_DefaultState_IsFavourite_ReturnsFalse
    ///   [07] FavouriteRoomsSO_AfterAddFavourite_IsFavourite_ReturnsTrue
    ///   [08] FavouriteRoomsSO_AddFavourite_Twice_DoesNotDuplicate
    ///
    /// SlotsRemaining coherence with IsFull
    ///   [09] SlotsRemaining_And_IsFull_AreCoherent
    /// </summary>
    [TestFixture]
    public sealed class RoomEntryEnhancementsTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private FavouriteRoomsSO _favourites;

        [SetUp]
        public void SetUp()
        {
            _favourites = ScriptableObject.CreateInstance<FavouriteRoomsSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_favourites);
        }

        // ── [01] SlotsRemaining — maxPlayers not configured ───────────────────

        [Test]
        public void SlotsRemaining_ZeroMaxPlayers_ReturnsZero()
        {
            // Manually construct a RoomEntry with maxPlayers = 0
            // (struct allows direct field assignment bypassing the constructor guard).
            var entry = new RoomEntry { roomCode = "AAAA", playerCount = 0, maxPlayers = 0 };
            Assert.AreEqual(0, entry.SlotsRemaining);
        }

        // ── [02] SlotsRemaining — partial room ────────────────────────────────

        [Test]
        public void SlotsRemaining_PartialRoom_ReturnsCorrectCount()
        {
            var entry = new RoomEntry("BBBB", playerCount: 1, maxPlayers: 4);
            Assert.AreEqual(3, entry.SlotsRemaining);
        }

        // ── [03] SlotsRemaining — full room ──────────────────────────────────

        [Test]
        public void SlotsRemaining_FullRoom_ReturnsZero()
        {
            var entry = new RoomEntry("CCCC", playerCount: 2, maxPlayers: 2);
            Assert.IsTrue(entry.IsFull,           "Pre-condition: room must report IsFull.");
            Assert.AreEqual(0, entry.SlotsRemaining, "Full room must have 0 slots remaining.");
        }

        // ── [04] SlotsRemaining — exactly one slot open ───────────────────────

        [Test]
        public void SlotsRemaining_SingleSlotRemaining_ReturnsOne()
        {
            var entry = new RoomEntry("DDDD", playerCount: 3, maxPlayers: 4);
            Assert.AreEqual(1, entry.SlotsRemaining);
        }

        // ── [05] SlotsRemaining — playerCount exceeds maxPlayers ─────────────

        [Test]
        public void SlotsRemaining_PlayerCountAboveMax_ReturnsZero()
        {
            // Edge case: stub could theoretically overfill a room during fast updates.
            var entry = new RoomEntry { roomCode = "EEEE", playerCount = 5, maxPlayers = 2 };
            Assert.AreEqual(0, entry.SlotsRemaining,
                "SlotsRemaining must never return negative even when count exceeds max.");
        }

        // ── [06] FavouriteRoomsSO — default state ────────────────────────────

        [Test]
        public void FavouriteRoomsSO_DefaultState_IsFavourite_ReturnsFalse()
        {
            Assert.IsFalse(_favourites.IsFavourite("FFFF"),
                "A freshly created FavouriteRoomsSO should contain no favourites.");
        }

        // ── [07] FavouriteRoomsSO — after AddFavourite ────────────────────────

        [Test]
        public void FavouriteRoomsSO_AfterAddFavourite_IsFavourite_ReturnsTrue()
        {
            _favourites.AddFavourite("GGGG");
            Assert.IsTrue(_favourites.IsFavourite("GGGG"),
                "IsFavourite must return true immediately after AddFavourite.");
        }

        // ── [08] FavouriteRoomsSO — idempotent AddFavourite ──────────────────

        [Test]
        public void FavouriteRoomsSO_AddFavouriteTwice_DoesNotDuplicate()
        {
            _favourites.AddFavourite("HHHH");
            _favourites.AddFavourite("HHHH");

            // The Favourites list must not contain the code twice.
            int occurrences = 0;
            foreach (string code in _favourites.Favourites)
            {
                if (code == "HHHH") occurrences++;
            }

            Assert.AreEqual(1, occurrences,
                "AddFavourite must be idempotent — duplicate entries are not allowed.");
        }

        // ── [09] SlotsRemaining / IsFull coherence ────────────────────────────

        [Test]
        public void SlotsRemaining_And_IsFull_AreCoherent()
        {
            // When a room is full, SlotsRemaining must be 0.
            // When a room is not full (and maxPlayers > 0), SlotsRemaining must be > 0.
            var full    = new RoomEntry("IIII", playerCount: 2, maxPlayers: 2);
            var partial = new RoomEntry("JJJJ", playerCount: 1, maxPlayers: 2);

            Assert.IsTrue (full.IsFull,    "full.IsFull should be true.");
            Assert.IsFalse(partial.IsFull, "partial.IsFull should be false.");

            Assert.AreEqual(0, full.SlotsRemaining,
                "SlotsRemaining must be 0 when IsFull is true.");
            Assert.Greater(partial.SlotsRemaining, 0,
                "SlotsRemaining must be positive when IsFull is false and maxPlayers > 0.");
        }
    }
}

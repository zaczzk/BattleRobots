using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T047 — Room bookmarking / favourites.
    ///
    /// Coverage (16 cases):
    ///
    /// FavouriteRoomsSO — default state
    ///   [01] DefaultState_Count_IsZero
    ///   [02] DefaultState_IsFavourite_ReturnsFalse
    ///   [03] DefaultState_Favourites_IsEmpty
    ///
    /// FavouriteRoomsSO — AddFavourite
    ///   [04] AddFavourite_SingleCode_IsFavourite_True
    ///   [05] AddFavourite_SingleCode_CountIsOne
    ///   [06] AddFavourite_Duplicate_DoesNotIncreaseCount
    ///   [07] AddFavourite_NullCode_DoesNotThrow
    ///   [08] AddFavourite_EmptyCode_DoesNotThrow
    ///   [09] AddFavourite_MultipleDistinctCodes_PreservesOrder
    ///
    /// FavouriteRoomsSO — RemoveFavourite
    ///   [10] RemoveFavourite_ExistingCode_IsFavourite_False
    ///   [11] RemoveFavourite_ExistingCode_DecreasesCount
    ///   [12] RemoveFavourite_NonExistingCode_DoesNotThrow
    ///   [13] RemoveFavourite_NullCode_DoesNotThrow
    ///
    /// FavouriteRoomsSO — Clear
    ///   [14] Clear_AfterAdd_CountIsZero
    ///   [15] Clear_OnEmpty_DoesNotThrow
    ///
    /// FavouriteRoomsSO — LoadFromData / BuildData
    ///   [16] LoadFromData_PopulatesFavourites
    ///   [17] LoadFromData_Null_EmptiesList
    ///   [18] LoadFromData_DeduplicatesCodes
    ///   [19] LoadFromData_SkipsNullAndEmptyEntries
    ///   [20] BuildData_RoundTripThroughLoadFromData
    ///
    /// SaveData — field presence
    ///   [21] SaveData_FavouriteRoomCodes_DefaultIsEmptyList
    /// </summary>
    [TestFixture]
    public sealed class FavouriteRoomsTests
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

        // ── [01] Default state — Count ────────────────────────────────────────

        [Test]
        public void DefaultState_Count_IsZero()
        {
            Assert.AreEqual(0, _favourites.Count);
        }

        // ── [02] Default state — IsFavourite ─────────────────────────────────

        [Test]
        public void DefaultState_IsFavourite_ReturnsFalse()
        {
            Assert.IsFalse(_favourites.IsFavourite("ABCD"));
        }

        // ── [03] Default state — Favourites list ──────────────────────────────

        [Test]
        public void DefaultState_Favourites_IsEmpty()
        {
            Assert.IsNotNull(_favourites.Favourites);
            Assert.AreEqual(0, _favourites.Favourites.Count);
        }

        // ── [04] AddFavourite — IsFavourite true ──────────────────────────────

        [Test]
        public void AddFavourite_SingleCode_IsFavourite_True()
        {
            _favourites.AddFavourite("ABCD");
            Assert.IsTrue(_favourites.IsFavourite("ABCD"));
        }

        // ── [05] AddFavourite — Count increments ──────────────────────────────

        [Test]
        public void AddFavourite_SingleCode_CountIsOne()
        {
            _favourites.AddFavourite("XYZW");
            Assert.AreEqual(1, _favourites.Count);
        }

        // ── [06] AddFavourite — duplicate is idempotent ───────────────────────

        [Test]
        public void AddFavourite_Duplicate_DoesNotIncreaseCount()
        {
            _favourites.AddFavourite("AAAA");
            _favourites.AddFavourite("AAAA");
            Assert.AreEqual(1, _favourites.Count);
        }

        // ── [07] AddFavourite — null code ─────────────────────────────────────

        [Test]
        public void AddFavourite_NullCode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _favourites.AddFavourite(null));
            Assert.AreEqual(0, _favourites.Count);
        }

        // ── [08] AddFavourite — empty code ────────────────────────────────────

        [Test]
        public void AddFavourite_EmptyCode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _favourites.AddFavourite(string.Empty));
            Assert.AreEqual(0, _favourites.Count);
        }

        // ── [09] AddFavourite — insertion order preserved ─────────────────────

        [Test]
        public void AddFavourite_MultipleDistinctCodes_PreservesOrder()
        {
            _favourites.AddFavourite("FIRST");
            _favourites.AddFavourite("SECOND");
            _favourites.AddFavourite("THIRD");

            Assert.AreEqual(3, _favourites.Count);
            Assert.AreEqual("FIRST",  _favourites.Favourites[0]);
            Assert.AreEqual("SECOND", _favourites.Favourites[1]);
            Assert.AreEqual("THIRD",  _favourites.Favourites[2]);
        }

        // ── [10] RemoveFavourite — IsFavourite false ──────────────────────────

        [Test]
        public void RemoveFavourite_ExistingCode_IsFavourite_False()
        {
            _favourites.AddFavourite("ABCD");
            _favourites.RemoveFavourite("ABCD");
            Assert.IsFalse(_favourites.IsFavourite("ABCD"));
        }

        // ── [11] RemoveFavourite — Count decreases ────────────────────────────

        [Test]
        public void RemoveFavourite_ExistingCode_DecreasesCount()
        {
            _favourites.AddFavourite("ABCD");
            _favourites.AddFavourite("EFGH");
            _favourites.RemoveFavourite("ABCD");
            Assert.AreEqual(1, _favourites.Count);
        }

        // ── [12] RemoveFavourite — non-existing code is no-op ────────────────

        [Test]
        public void RemoveFavourite_NonExistingCode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _favourites.RemoveFavourite("ZZZZ"));
            Assert.AreEqual(0, _favourites.Count);
        }

        // ── [13] RemoveFavourite — null code ─────────────────────────────────

        [Test]
        public void RemoveFavourite_NullCode_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _favourites.RemoveFavourite(null));
        }

        // ── [14] Clear — empties the list ────────────────────────────────────

        [Test]
        public void Clear_AfterAdd_CountIsZero()
        {
            _favourites.AddFavourite("AAAA");
            _favourites.AddFavourite("BBBB");
            _favourites.Clear();
            Assert.AreEqual(0, _favourites.Count);
        }

        // ── [15] Clear — on empty list is safe ───────────────────────────────

        [Test]
        public void Clear_OnEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _favourites.Clear());
        }

        // ── [16] LoadFromData — populates list ───────────────────────────────

        [Test]
        public void LoadFromData_PopulatesFavourites()
        {
            var codes = new List<string> { "AAAA", "BBBB", "CCCC" };
            _favourites.LoadFromData(codes);

            Assert.AreEqual(3, _favourites.Count);
            Assert.IsTrue(_favourites.IsFavourite("AAAA"));
            Assert.IsTrue(_favourites.IsFavourite("BBBB"));
            Assert.IsTrue(_favourites.IsFavourite("CCCC"));
        }

        // ── [17] LoadFromData — null input empties list ───────────────────────

        [Test]
        public void LoadFromData_Null_EmptiesList()
        {
            _favourites.AddFavourite("AAAA");
            _favourites.LoadFromData(null);
            Assert.AreEqual(0, _favourites.Count);
        }

        // ── [18] LoadFromData — duplicates are de-duplicated ─────────────────

        [Test]
        public void LoadFromData_DeduplicatesCodes()
        {
            var codes = new List<string> { "AAAA", "AAAA", "BBBB" };
            _favourites.LoadFromData(codes);

            Assert.AreEqual(2, _favourites.Count);
            Assert.IsTrue(_favourites.IsFavourite("AAAA"));
            Assert.IsTrue(_favourites.IsFavourite("BBBB"));
        }

        // ── [19] LoadFromData — null/empty entries are skipped ────────────────

        [Test]
        public void LoadFromData_SkipsNullAndEmptyEntries()
        {
            var codes = new List<string> { null, string.Empty, "CCCC", null };
            _favourites.LoadFromData(codes);

            Assert.AreEqual(1, _favourites.Count);
            Assert.IsTrue(_favourites.IsFavourite("CCCC"));
        }

        // ── [20] BuildData / LoadFromData round-trip ──────────────────────────

        [Test]
        public void BuildData_RoundTripThroughLoadFromData()
        {
            _favourites.AddFavourite("ROOM");
            _favourites.AddFavourite("TEST");

            List<string> snapshot = _favourites.BuildData();

            var second = ScriptableObject.CreateInstance<FavouriteRoomsSO>();
            try
            {
                second.LoadFromData(snapshot);

                Assert.AreEqual(2, second.Count);
                Assert.IsTrue(second.IsFavourite("ROOM"));
                Assert.IsTrue(second.IsFavourite("TEST"));
            }
            finally
            {
                Object.DestroyImmediate(second);
            }
        }

        // ── [21] SaveData — field exists and defaults to empty list ───────────

        [Test]
        public void SaveData_FavouriteRoomCodes_DefaultIsEmptyList()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.favouriteRoomCodes);
            Assert.AreEqual(0, save.favouriteRoomCodes.Count);
        }
    }
}

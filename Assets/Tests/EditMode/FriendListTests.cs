using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T065 — FriendListSO + FriendEntryUI + FriendListUI.
    ///
    /// Coverage (22 cases):
    ///
    /// FriendListSO — default state
    ///   [01] DefaultState_FriendCount_IsZero
    ///   [02] DefaultState_BlockedCount_IsZero
    ///   [03] DefaultState_IsFriend_ReturnsFalse
    ///   [04] DefaultState_IsBlocked_ReturnsFalse
    ///
    /// FriendListSO — AddFriend
    ///   [05] AddFriend_ValidName_AddsToList
    ///   [06] AddFriend_DuplicateName_IsIdempotent
    ///   [07] AddFriend_NullName_IsIgnored
    ///   [08] AddFriend_BlockedPlayer_IsRejected
    ///
    /// FriendListSO — RemoveFriend
    ///   [09] RemoveFriend_ExistingFriend_RemovesFromList
    ///   [10] RemoveFriend_NonExistentName_IsNoOp
    ///
    /// FriendListSO — BlockPlayer
    ///   [11] BlockPlayer_NewPlayer_AddsToBlockedList
    ///   [12] BlockPlayer_ExistingFriend_RemovesFromFriendsAndAddsToBlocked
    ///   [13] BlockPlayer_AlreadyBlocked_IsIdempotent
    ///
    /// FriendListSO — UnblockPlayer
    ///   [14] UnblockPlayer_BlockedPlayer_RemovesFromBlockedList
    ///   [15] UnblockPlayer_NotBlockedPlayer_IsNoOp
    ///   [16] UnblockPlayer_DoesNotAutoAddToFriends
    ///
    /// FriendListSO — ClearAll
    ///   [17] ClearAll_RemovesBothLists
    ///
    /// FriendListSO — LoadFromData / BuildData
    ///   [18] LoadFromData_PopulatesLists
    ///   [19] BuildData_RoundTripThroughLoadFromData
    ///   [20] LoadFromData_EnforcesXorInvariant
    ///
    /// SaveData — friendList field
    ///   [21] SaveData_FriendList_DefaultIsNotNull
    ///
    /// FriendEntryUI — Setup
    ///   [22] FriendEntryUI_SetupAsFriend_SetsNameLabel
    /// </summary>
    [TestFixture]
    public sealed class FriendListTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private FriendListSO _so;

        [SetUp]
        public void SetUp()
        {
            _so = ScriptableObject.CreateInstance<FriendListSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
        }

        // ── [01] Default state — FriendCount ─────────────────────────────────

        [Test]
        public void DefaultState_FriendCount_IsZero()
        {
            Assert.AreEqual(0, _so.FriendCount);
        }

        // ── [02] Default state — BlockedCount ────────────────────────────────

        [Test]
        public void DefaultState_BlockedCount_IsZero()
        {
            Assert.AreEqual(0, _so.BlockedCount);
        }

        // ── [03] Default state — IsFriend ─────────────────────────────────────

        [Test]
        public void DefaultState_IsFriend_ReturnsFalse()
        {
            Assert.IsFalse(_so.IsFriend("Alice"));
        }

        // ── [04] Default state — IsBlocked ────────────────────────────────────

        [Test]
        public void DefaultState_IsBlocked_ReturnsFalse()
        {
            Assert.IsFalse(_so.IsBlocked("Alice"));
        }

        // ── [05] AddFriend — adds to list ─────────────────────────────────────

        [Test]
        public void AddFriend_ValidName_AddsToList()
        {
            _so.AddFriend("Alice");
            Assert.IsTrue(_so.IsFriend("Alice"));
            Assert.AreEqual(1, _so.FriendCount);
        }

        // ── [06] AddFriend — duplicate is idempotent ──────────────────────────

        [Test]
        public void AddFriend_DuplicateName_IsIdempotent()
        {
            _so.AddFriend("Alice");
            _so.AddFriend("Alice");
            Assert.AreEqual(1, _so.FriendCount);
        }

        // ── [07] AddFriend — null name is ignored ─────────────────────────────

        [Test]
        public void AddFriend_NullName_IsIgnored()
        {
            _so.AddFriend(null);
            _so.AddFriend(string.Empty);
            _so.AddFriend("   ");
            Assert.AreEqual(0, _so.FriendCount);
        }

        // ── [08] AddFriend — blocked player is rejected ───────────────────────

        [Test]
        public void AddFriend_BlockedPlayer_IsRejected()
        {
            _so.BlockPlayer("Alice");
            _so.AddFriend("Alice");
            Assert.IsFalse(_so.IsFriend("Alice"));
            Assert.AreEqual(0, _so.FriendCount);
        }

        // ── [09] RemoveFriend — existing friend ───────────────────────────────

        [Test]
        public void RemoveFriend_ExistingFriend_RemovesFromList()
        {
            _so.AddFriend("Alice");
            _so.RemoveFriend("Alice");
            Assert.IsFalse(_so.IsFriend("Alice"));
            Assert.AreEqual(0, _so.FriendCount);
        }

        // ── [10] RemoveFriend — non-existent is no-op ─────────────────────────

        [Test]
        public void RemoveFriend_NonExistentName_IsNoOp()
        {
            Assert.DoesNotThrow(() => _so.RemoveFriend("Bob"));
            Assert.AreEqual(0, _so.FriendCount);
        }

        // ── [11] BlockPlayer — adds to blocked list ───────────────────────────

        [Test]
        public void BlockPlayer_NewPlayer_AddsToBlockedList()
        {
            _so.BlockPlayer("Eve");
            Assert.IsTrue(_so.IsBlocked("Eve"));
            Assert.AreEqual(1, _so.BlockedCount);
        }

        // ── [12] BlockPlayer — removes from friends first ─────────────────────

        [Test]
        public void BlockPlayer_ExistingFriend_RemovesFromFriendsAndAddsToBlocked()
        {
            _so.AddFriend("Alice");
            _so.BlockPlayer("Alice");

            Assert.IsFalse(_so.IsFriend("Alice"),  "Should no longer be a friend.");
            Assert.IsTrue(_so.IsBlocked("Alice"),   "Should be blocked.");
            Assert.AreEqual(0, _so.FriendCount);
            Assert.AreEqual(1, _so.BlockedCount);
        }

        // ── [13] BlockPlayer — already blocked is idempotent ─────────────────

        [Test]
        public void BlockPlayer_AlreadyBlocked_IsIdempotent()
        {
            _so.BlockPlayer("Eve");
            _so.BlockPlayer("Eve");
            Assert.AreEqual(1, _so.BlockedCount);
        }

        // ── [14] UnblockPlayer — removes from blocked list ────────────────────

        [Test]
        public void UnblockPlayer_BlockedPlayer_RemovesFromBlockedList()
        {
            _so.BlockPlayer("Eve");
            _so.UnblockPlayer("Eve");
            Assert.IsFalse(_so.IsBlocked("Eve"));
            Assert.AreEqual(0, _so.BlockedCount);
        }

        // ── [15] UnblockPlayer — not-blocked is no-op ─────────────────────────

        [Test]
        public void UnblockPlayer_NotBlockedPlayer_IsNoOp()
        {
            Assert.DoesNotThrow(() => _so.UnblockPlayer("Nobody"));
            Assert.AreEqual(0, _so.BlockedCount);
        }

        // ── [16] UnblockPlayer — does not re-add to friends ───────────────────

        [Test]
        public void UnblockPlayer_DoesNotAutoAddToFriends()
        {
            _so.AddFriend("Alice");
            _so.BlockPlayer("Alice");    // removed from friends, added to blocked
            _so.UnblockPlayer("Alice");  // removed from blocked only
            Assert.IsFalse(_so.IsFriend("Alice"),  "Unblock should not auto-re-friend.");
            Assert.IsFalse(_so.IsBlocked("Alice"), "Should no longer be blocked.");
        }

        // ── [17] ClearAll ─────────────────────────────────────────────────────

        [Test]
        public void ClearAll_RemovesBothLists()
        {
            _so.AddFriend("Alice");
            _so.AddFriend("Bob");
            _so.BlockPlayer("Eve");
            _so.ClearAll();
            Assert.AreEqual(0, _so.FriendCount);
            Assert.AreEqual(0, _so.BlockedCount);
        }

        // ── [18] LoadFromData — populates both lists ───────────────────────────

        [Test]
        public void LoadFromData_PopulatesLists()
        {
            var data = new FriendListData
            {
                friendNames  = new List<string> { "Alice", "Bob" },
                blockedNames = new List<string> { "Eve" },
            };

            _so.LoadFromData(data);

            Assert.AreEqual(2, _so.FriendCount);
            Assert.AreEqual(1, _so.BlockedCount);
            Assert.IsTrue(_so.IsFriend("Alice"));
            Assert.IsTrue(_so.IsFriend("Bob"));
            Assert.IsTrue(_so.IsBlocked("Eve"));
        }

        // ── [19] BuildData — round-trips through LoadFromData ─────────────────

        [Test]
        public void BuildData_RoundTripThroughLoadFromData()
        {
            _so.AddFriend("Alice");
            _so.AddFriend("Bob");
            _so.BlockPlayer("Eve");

            FriendListData snapshot = _so.BuildData();

            var so2 = ScriptableObject.CreateInstance<FriendListSO>();
            so2.LoadFromData(snapshot);

            Assert.AreEqual(2, so2.FriendCount);
            Assert.AreEqual(1, so2.BlockedCount);
            Assert.IsTrue(so2.IsFriend("Alice"));
            Assert.IsTrue(so2.IsFriend("Bob"));
            Assert.IsTrue(so2.IsBlocked("Eve"));

            Object.DestroyImmediate(so2);
        }

        // ── [20] LoadFromData — enforces XOR invariant ────────────────────────

        [Test]
        public void LoadFromData_EnforcesXorInvariant()
        {
            // Provide corrupt data where "Alice" is in both lists.
            var data = new FriendListData
            {
                friendNames  = new List<string> { "Alice" },
                blockedNames = new List<string> { "Alice" },
            };

            _so.LoadFromData(data);

            // Alice should be blocked (blocked takes precedence) and not a friend.
            Assert.IsFalse(_so.IsFriend("Alice"),  "Alice should not be a friend.");
            Assert.IsTrue(_so.IsBlocked("Alice"),   "Alice should be blocked.");
        }

        // ── [21] SaveData — friendList default not null ───────────────────────

        [Test]
        public void SaveData_FriendList_DefaultIsNotNull()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.friendList);
            Assert.IsNotNull(save.friendList.friendNames);
            Assert.IsNotNull(save.friendList.blockedNames);
        }

        // ── [22] FriendEntryUI — SetupAsFriend sets name label ────────────────

        [Test]
        public void FriendEntryUI_SetupAsFriend_SetsNameLabel()
        {
            var go    = new GameObject("FriendEntry");
            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(go.transform, false);

            var entry = go.AddComponent<FriendEntryUI>();
            InjectField(entry, "_nameLabel", label);

            entry.SetupAsFriend("Alice", _so);

            Assert.AreEqual("Alice", label.text);
            Assert.AreEqual("Alice", entry.PlayerName);

            Object.DestroyImmediate(go);
        }

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void InjectField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}

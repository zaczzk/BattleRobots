using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerInventory"/>.
    ///
    /// Tests use <see cref="ScriptableObject.CreateInstance{T}"/> so no scene or
    /// asset database is required. All tests are independent and allocation-light.
    /// </summary>
    public class PlayerInventoryTests
    {
        private PlayerInventory _inventory;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _inventory = ScriptableObject.CreateInstance<PlayerInventory>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_inventory);
        }

        // ── UnlockPart ────────────────────────────────────────────────────────

        [Test]
        public void UnlockPart_ValidId_AddsToPart_List()
        {
            _inventory.UnlockPart("arm_heavy");

            Assert.AreEqual(1, _inventory.UnlockedPartIds.Count);
            Assert.AreEqual("arm_heavy", _inventory.UnlockedPartIds[0]);
        }

        [Test]
        public void UnlockPart_DuplicateId_DoesNotAddAgain()
        {
            _inventory.UnlockPart("arm_heavy");
            _inventory.UnlockPart("arm_heavy");

            Assert.AreEqual(1, _inventory.UnlockedPartIds.Count);
        }

        [Test]
        public void UnlockPart_MultipleDistinct_AllPresent()
        {
            _inventory.UnlockPart("arm_heavy");
            _inventory.UnlockPart("wheel_fast");
            _inventory.UnlockPart("chassis_tank");

            Assert.AreEqual(3, _inventory.UnlockedPartIds.Count);
        }

        [Test]
        public void UnlockPart_NullId_IsIgnored()
        {
            _inventory.UnlockPart(null);

            Assert.AreEqual(0, _inventory.UnlockedPartIds.Count);
        }

        [Test]
        public void UnlockPart_EmptyId_IsIgnored()
        {
            _inventory.UnlockPart(string.Empty);

            Assert.AreEqual(0, _inventory.UnlockedPartIds.Count);
        }

        [Test]
        public void UnlockPart_WhitespaceId_IsIgnored()
        {
            _inventory.UnlockPart("   ");

            Assert.AreEqual(0, _inventory.UnlockedPartIds.Count);
        }

        // ── HasPart ───────────────────────────────────────────────────────────

        [Test]
        public void HasPart_AfterUnlock_ReturnsTrue()
        {
            _inventory.UnlockPart("arm_heavy");

            Assert.IsTrue(_inventory.HasPart("arm_heavy"));
        }

        [Test]
        public void HasPart_NotUnlocked_ReturnsFalse()
        {
            Assert.IsFalse(_inventory.HasPart("arm_heavy"));
        }

        [Test]
        public void HasPart_NullId_ReturnsFalse()
        {
            _inventory.UnlockPart("arm_heavy");

            Assert.IsFalse(_inventory.HasPart(null));
        }

        [Test]
        public void HasPart_EmptyId_ReturnsFalse()
        {
            Assert.IsFalse(_inventory.HasPart(string.Empty));
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_PopulatesListAndSet()
        {
            _inventory.LoadSnapshot(new List<string> { "arm_heavy", "wheel_fast" });

            Assert.AreEqual(2, _inventory.UnlockedPartIds.Count);
            Assert.IsTrue(_inventory.HasPart("arm_heavy"));
            Assert.IsTrue(_inventory.HasPart("wheel_fast"));
        }

        [Test]
        public void LoadSnapshot_Null_ClearsState()
        {
            _inventory.UnlockPart("arm_heavy");

            _inventory.LoadSnapshot(null);

            Assert.AreEqual(0, _inventory.UnlockedPartIds.Count);
            Assert.IsFalse(_inventory.HasPart("arm_heavy"));
        }

        [Test]
        public void LoadSnapshot_Empty_ClearsExistingState()
        {
            _inventory.UnlockPart("arm_heavy");

            _inventory.LoadSnapshot(new List<string>());

            Assert.AreEqual(0, _inventory.UnlockedPartIds.Count);
        }

        [Test]
        public void LoadSnapshot_DeduplicatesIds()
        {
            _inventory.LoadSnapshot(new List<string> { "arm_heavy", "arm_heavy", "wheel_fast" });

            Assert.AreEqual(2, _inventory.UnlockedPartIds.Count);
        }

        [Test]
        public void LoadSnapshot_ReplacesExistingState()
        {
            _inventory.UnlockPart("old_part");

            _inventory.LoadSnapshot(new List<string> { "new_part" });

            Assert.IsFalse(_inventory.HasPart("old_part"));
            Assert.IsTrue(_inventory.HasPart("new_part"));
            Assert.AreEqual(1, _inventory.UnlockedPartIds.Count);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllParts()
        {
            _inventory.UnlockPart("arm_heavy");
            _inventory.UnlockPart("wheel_fast");

            _inventory.Reset();

            Assert.AreEqual(0, _inventory.UnlockedPartIds.Count);
            Assert.IsFalse(_inventory.HasPart("arm_heavy"));
        }

        [Test]
        public void Reset_ThenUnlock_WorksCorrectly()
        {
            _inventory.UnlockPart("arm_heavy");
            _inventory.Reset();
            _inventory.UnlockPart("wheel_fast");

            Assert.AreEqual(1, _inventory.UnlockedPartIds.Count);
            Assert.IsTrue(_inventory.HasPart("wheel_fast"));
            Assert.IsFalse(_inventory.HasPart("arm_heavy"));
        }
    }
}

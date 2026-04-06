using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotLoadoutSO"/>.
    ///
    /// Each test creates a fresh SO instance. The optional <c>_onLoadoutChanged</c>
    /// VoidGameEvent field is left null — all raise calls use the null-conditional
    /// operator so this is safe.
    /// </summary>
    [TestFixture]
    public sealed class RobotLoadoutSOTests
    {
        private RobotLoadoutSO _loadout;

        [SetUp]
        public void SetUp()
        {
            _loadout = ScriptableObject.CreateInstance<RobotLoadoutSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loadout);
        }

        // ── Initial state ─────────────────────────────────────────────────────

        [Test]
        public void Default_EquippedCountIsZero()
        {
            Assert.AreEqual(0, _loadout.EquippedCount,
                "A fresh RobotLoadoutSO should have no equipped parts.");
        }

        [Test]
        public void Default_GetEquippedPartId_ReturnsNull()
        {
            Assert.IsNull(_loadout.GetEquippedPartId("weapon_left"),
                "GetEquippedPartId for any slot on empty loadout should return null.");
        }

        [Test]
        public void Default_IsEquipped_ReturnsFalse()
        {
            Assert.IsFalse(_loadout.IsEquipped("leg_front"),
                "IsEquipped should return false for any slot on an empty loadout.");
        }

        // ── EquipPart ─────────────────────────────────────────────────────────

        [Test]
        public void EquipPart_ValidIds_StoresEntry()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");

            Assert.AreEqual(1, _loadout.EquippedCount);
            Assert.AreEqual("blade_mk1", _loadout.GetEquippedPartId("weapon_left"));
            Assert.IsTrue(_loadout.IsEquipped("weapon_left"));
        }

        [Test]
        public void EquipPart_ReplacesExistingPart()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.EquipPart("weapon_left", "saw_mk2");

            Assert.AreEqual(1, _loadout.EquippedCount,
                "Replacing a part in the same slot must not increase EquippedCount.");
            Assert.AreEqual("saw_mk2", _loadout.GetEquippedPartId("weapon_left"),
                "After replacement the new partId must be returned.");
        }

        [Test]
        public void EquipPart_MultipleSlots_AllStored()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.EquipPart("leg_front",   "wheel_heavy");
            _loadout.EquipPart("armor_back",  "titanium_plate");

            Assert.AreEqual(3, _loadout.EquippedCount);
            Assert.AreEqual("blade_mk1",     _loadout.GetEquippedPartId("weapon_left"));
            Assert.AreEqual("wheel_heavy",   _loadout.GetEquippedPartId("leg_front"));
            Assert.AreEqual("titanium_plate",_loadout.GetEquippedPartId("armor_back"));
        }

        [Test]
        public void EquipPart_NullSlotId_DoesNothing()
        {
            _loadout.EquipPart(null, "some_part");
            Assert.AreEqual(0, _loadout.EquippedCount,
                "EquipPart with a null slotId must be silently ignored.");
        }

        [Test]
        public void EquipPart_EmptyPartId_ActsAsUnequip()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.EquipPart("weapon_left", ""); // empty partId → unequip

            Assert.AreEqual(0, _loadout.EquippedCount,
                "EquipPart with an empty partId should remove the slot entry.");
            Assert.IsFalse(_loadout.IsEquipped("weapon_left"));
        }

        // ── UnequipPart ───────────────────────────────────────────────────────

        [Test]
        public void UnequipPart_ExistingSlot_RemovesEntry()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.UnequipPart("weapon_left");

            Assert.AreEqual(0, _loadout.EquippedCount);
            Assert.IsFalse(_loadout.IsEquipped("weapon_left"));
            Assert.IsNull(_loadout.GetEquippedPartId("weapon_left"));
        }

        [Test]
        public void UnequipPart_NonExistentSlot_DoesNothing()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.UnequipPart("leg_front"); // slot not occupied

            Assert.AreEqual(1, _loadout.EquippedCount,
                "Unequipping a slot that was never set must not affect other entries.");
        }

        [Test]
        public void UnequipPart_EmptySlotId_DoesNothing()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.UnequipPart(""); // should be ignored

            Assert.AreEqual(1, _loadout.EquippedCount,
                "UnequipPart with empty slotId must be silently ignored.");
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [Test]
        public void Clear_RemovesAllEntries()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.EquipPart("leg_front",   "wheel_heavy");
            _loadout.Clear();

            Assert.AreEqual(0, _loadout.EquippedCount);
            Assert.IsFalse(_loadout.IsEquipped("weapon_left"));
            Assert.IsFalse(_loadout.IsEquipped("leg_front"));
        }

        [Test]
        public void Clear_OnEmptyLoadout_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _loadout.Clear(),
                "Clear() on an already-empty loadout must not throw.");
        }

        // ── LoadFromData / BuildData round-trip ───────────────────────────────

        [Test]
        public void LoadFromData_PopulatesLoadout()
        {
            var data = new RobotLoadoutData();
            data.entries.Add(new LoadoutEntry { slotId = "weapon_left", partId = "blade_mk1" });
            data.entries.Add(new LoadoutEntry { slotId = "leg_front",   partId = "wheel_heavy" });

            _loadout.LoadFromData(data);

            Assert.AreEqual(2, _loadout.EquippedCount);
            Assert.AreEqual("blade_mk1",   _loadout.GetEquippedPartId("weapon_left"));
            Assert.AreEqual("wheel_heavy", _loadout.GetEquippedPartId("leg_front"));
        }

        [Test]
        public void LoadFromData_NullData_LeavesLoadoutEmpty()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1"); // put something in first
            _loadout.LoadFromData(null);

            Assert.AreEqual(0, _loadout.EquippedCount,
                "LoadFromData(null) should clear the loadout.");
        }

        [Test]
        public void LoadFromData_SkipsEntriesWithEmptyIds()
        {
            var data = new RobotLoadoutData();
            data.entries.Add(new LoadoutEntry { slotId = "",            partId = "blade_mk1" });
            data.entries.Add(new LoadoutEntry { slotId = "weapon_left", partId = "" });
            data.entries.Add(new LoadoutEntry { slotId = "leg_front",   partId = "wheel_heavy" });

            _loadout.LoadFromData(data);

            Assert.AreEqual(1, _loadout.EquippedCount,
                "Entries with empty slotId or partId must be skipped.");
            Assert.AreEqual("wheel_heavy", _loadout.GetEquippedPartId("leg_front"));
        }

        [Test]
        public void BuildData_RoundTrips()
        {
            _loadout.EquipPart("weapon_left", "blade_mk1");
            _loadout.EquipPart("leg_front",   "wheel_heavy");

            RobotLoadoutData snapshot = _loadout.BuildData();

            // Load into a second instance and verify equality.
            RobotLoadoutSO copy = ScriptableObject.CreateInstance<RobotLoadoutSO>();
            try
            {
                copy.LoadFromData(snapshot);

                Assert.AreEqual(_loadout.EquippedCount, copy.EquippedCount,
                    "Round-tripped loadout must have the same entry count.");
                Assert.AreEqual("blade_mk1",   copy.GetEquippedPartId("weapon_left"));
                Assert.AreEqual("wheel_heavy", copy.GetEquippedPartId("leg_front"));
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void BuildData_EmptyLoadout_ReturnsEmptyData()
        {
            RobotLoadoutData data = _loadout.BuildData();

            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.entries.Count,
                "BuildData on an empty loadout must return an empty entries list.");
        }

        // ── Overwrite guard ───────────────────────────────────────────────────

        [Test]
        public void LoadFromData_OverwritesExistingState()
        {
            _loadout.EquipPart("weapon_left", "old_blade");

            var data = new RobotLoadoutData();
            data.entries.Add(new LoadoutEntry { slotId = "leg_front", partId = "new_wheel" });

            _loadout.LoadFromData(data);

            Assert.AreEqual(1, _loadout.EquippedCount,
                "LoadFromData must clear previous entries before loading new ones.");
            Assert.IsNull(_loadout.GetEquippedPartId("weapon_left"),
                "Old entry must be gone after LoadFromData.");
            Assert.AreEqual("new_wheel", _loadout.GetEquippedPartId("leg_front"));
        }
    }
}

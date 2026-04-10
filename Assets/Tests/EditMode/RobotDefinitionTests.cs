using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotDefinition"/> ScriptableObject.
    ///
    /// Covers:
    ///   • Fresh-instance property defaults (name, stats, slot list).
    ///   • <see cref="RobotDefinition.ValidateSlots"/> error paths
    ///     (null slot, empty slotId, whitespace slotId, duplicate slotId).
    ///   • <see cref="RobotDefinition.ValidateSlots"/> passing paths
    ///     (single slot, two unique slots).
    ///
    /// The private serialised <c>_slots</c> field is injected via reflection so
    /// that validation logic can be exercised without requiring the Unity Editor.
    /// </summary>
    public class RobotDefinitionTests
    {
        private RobotDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<RobotDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
            _def = null;
        }

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Injects a slot list into the private serialised <c>_slots</c> backing
        /// field so ValidateSlots can be exercised without the Unity Inspector.
        /// </summary>
        private void SetSlots(List<PartSlot> slots)
        {
            FieldInfo field = typeof(RobotDefinition).GetField(
                "_slots",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(field, "Reflection: '_slots' field not found on RobotDefinition. " +
                                    "Check field name has not been renamed.");
            field.SetValue(_def, slots);
        }

        // ── Fresh-instance defaults ────────────────────────────────────────────

        [Test]
        public void FreshInstance_RobotName_IsUnnamedRobot()
        {
            Assert.AreEqual("Unnamed Robot", _def.RobotName);
        }

        [Test]
        public void FreshInstance_MaxHitPoints_IsPositive()
        {
            // Inspector: Min(1f); default 100f.
            Assert.Greater(_def.MaxHitPoints, 0f);
        }

        [Test]
        public void FreshInstance_MoveSpeed_IsPositive()
        {
            // Inspector: Min(0.1f); default 5f.
            Assert.Greater(_def.MoveSpeed, 0f);
        }

        [Test]
        public void FreshInstance_TorqueMultiplier_IsPositive()
        {
            // Inspector: Min(0.1f); default 1f.
            Assert.Greater(_def.TorqueMultiplier, 0f);
        }

        [Test]
        public void FreshInstance_Slots_IsNotNull()
        {
            Assert.IsNotNull(_def.Slots);
        }

        [Test]
        public void FreshInstance_Slots_IsEmpty()
        {
            Assert.AreEqual(0, _def.Slots.Count);
        }

        // ── ValidateSlots — failing cases ─────────────────────────────────────

        [Test]
        public void ValidateSlots_EmptyList_ReturnsFalse_WithErrorMessage()
        {
            SetSlots(new List<PartSlot>());

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsFalse(valid);
            Assert.IsNotNull(err);
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void ValidateSlots_NullSlotEntry_ReturnsFalse_WithErrorMessage()
        {
            SetSlots(new List<PartSlot> { null });

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsFalse(valid);
            Assert.IsNotNull(err);
        }

        [Test]
        public void ValidateSlots_EmptySlotId_ReturnsFalse_WithErrorMessage()
        {
            SetSlots(new List<PartSlot>
            {
                new PartSlot { slotId = "", category = PartCategory.Weapon }
            });

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsFalse(valid);
            Assert.IsNotNull(err);
        }

        [Test]
        public void ValidateSlots_WhitespaceSlotId_ReturnsFalse_WithErrorMessage()
        {
            SetSlots(new List<PartSlot>
            {
                new PartSlot { slotId = "   ", category = PartCategory.Weapon }
            });

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsFalse(valid);
            Assert.IsNotNull(err);
        }

        [Test]
        public void ValidateSlots_DuplicateSlotId_ReturnsFalse_ErrorContainsDuplicateId()
        {
            SetSlots(new List<PartSlot>
            {
                new PartSlot { slotId = "slot_front", category = PartCategory.Weapon },
                new PartSlot { slotId = "slot_front", category = PartCategory.Armor  }   // duplicate
            });

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsFalse(valid);
            StringAssert.Contains("slot_front", err);
        }

        // ── ValidateSlots — passing cases ─────────────────────────────────────

        [Test]
        public void ValidateSlots_SingleSlot_ReturnsTrue_ErrorIsNull()
        {
            SetSlots(new List<PartSlot>
            {
                new PartSlot { slotId = "chassis", category = PartCategory.Chassis }
            });

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsTrue(valid);
            Assert.IsNull(err);
        }

        [Test]
        public void ValidateSlots_TwoUniqueSlots_ReturnsTrue_ErrorIsNull()
        {
            SetSlots(new List<PartSlot>
            {
                new PartSlot { slotId = "weapon_front", category = PartCategory.Weapon },
                new PartSlot { slotId = "leg_left",     category = PartCategory.Leg    }
            });

            bool valid = _def.ValidateSlots(out string err);

            Assert.IsTrue(valid);
            Assert.IsNull(err);
        }
    }
}

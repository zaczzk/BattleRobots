using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests covering:
    ///   • <see cref="StarterInventoryConfig"/> SO read-only API.
    ///   • The starter-application logic executed by <see cref="GameBootstrapper"/>
    ///     (simulated directly on <see cref="PlayerInventory"/> so no MonoBehaviour
    ///     instantiation is required).
    ///
    /// All tests use <see cref="ScriptableObject.CreateInstance{T}"/> and are
    /// independent — no scene or AssetDatabase required.
    /// </summary>
    public class StarterInventoryConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a <see cref="StarterInventoryConfig"/> with <paramref name="ids"/>
        /// set via SerializedObject (Unity's canonical way to mutate serialized fields
        /// from Editor code without reflection).
        /// </summary>
        private static StarterInventoryConfig CreateConfig(params string[] ids)
        {
            var config = ScriptableObject.CreateInstance<StarterInventoryConfig>();
            var so   = new SerializedObject(config);
            var prop = so.FindProperty("_starterPartIds");
            prop.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = ids[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        /// <summary>
        /// Simulates the core of <c>GameBootstrapper.ApplyStarterInventory</c>:
        /// iterates the config list and calls <see cref="PlayerInventory.UnlockPart"/>.
        /// </summary>
        private static void SimulateApply(StarterInventoryConfig config, PlayerInventory inventory)
        {
            IReadOnlyList<string> starters = config.StarterPartIds;
            for (int i = 0; i < starters.Count; i++)
                inventory.UnlockPart(starters[i]);
        }

        // ── StarterInventoryConfig SO ─────────────────────────────────────────

        [Test]
        public void FreshInstance_StarterPartIds_IsEmpty()
        {
            var config = ScriptableObject.CreateInstance<StarterInventoryConfig>();

            Assert.AreEqual(0, config.StarterPartIds.Count);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void StarterPartIds_WithConfiguredEntries_ReturnsAllIds()
        {
            var config = CreateConfig("arm_heavy", "wheel_fast", "chassis_tank");

            Assert.AreEqual(3, config.StarterPartIds.Count);
            Assert.AreEqual("arm_heavy",    config.StarterPartIds[0]);
            Assert.AreEqual("wheel_fast",   config.StarterPartIds[1]);
            Assert.AreEqual("chassis_tank", config.StarterPartIds[2]);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void StarterPartIds_IsIReadOnlyList()
        {
            var config = CreateConfig("arm_heavy");

            // Verify the public API exposes IReadOnlyList<string>, not a mutable type.
            Assert.IsInstanceOf<IReadOnlyList<string>>(config.StarterPartIds);

            Object.DestroyImmediate(config);
        }

        // ── Starter-application logic ─────────────────────────────────────────

        [Test]
        public void ApplyStarters_EmptyConfig_InventoryRemainsEmpty()
        {
            var config    = CreateConfig(/* no ids */);
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            SimulateApply(config, inventory);

            Assert.AreEqual(0, inventory.UnlockedPartIds.Count);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(inventory);
        }

        [Test]
        public void ApplyStarters_TwoParts_BothUnlockedInInventory()
        {
            var config    = CreateConfig("arm_heavy", "wheel_fast");
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            SimulateApply(config, inventory);

            Assert.AreEqual(2, inventory.UnlockedPartIds.Count);
            Assert.IsTrue(inventory.HasPart("arm_heavy"));
            Assert.IsTrue(inventory.HasPart("wheel_fast"));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(inventory);
        }

        [Test]
        public void ApplyStarters_WithDuplicateIds_DeduplicatedByInventory()
        {
            var config    = CreateConfig("arm_heavy", "arm_heavy", "wheel_fast");
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            SimulateApply(config, inventory);

            // PlayerInventory.UnlockPart is idempotent — duplicates are silently dropped.
            Assert.AreEqual(2, inventory.UnlockedPartIds.Count);
            Assert.IsTrue(inventory.HasPart("arm_heavy"));
            Assert.IsTrue(inventory.HasPart("wheel_fast"));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(inventory);
        }

        [Test]
        public void ApplyStarters_WithNullEntry_NullIgnoredByInventory()
        {
            // SerializedObject sets the string to null when the array element is empty.
            var config    = CreateConfig("arm_heavy", null, "wheel_fast");
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            SimulateApply(config, inventory);

            // null entry is silently skipped by PlayerInventory.UnlockPart.
            Assert.AreEqual(2, inventory.UnlockedPartIds.Count);
            Assert.IsTrue(inventory.HasPart("arm_heavy"));
            Assert.IsTrue(inventory.HasPart("wheel_fast"));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(inventory);
        }

        [Test]
        public void ApplyStarters_GuardCondition_NonEmptyInventory_NotReapplied()
        {
            // Simulates GameBootstrapper guard: starters are NOT applied when
            // the inventory already has parts (returning player, not a new game).
            var config    = CreateConfig("arm_heavy", "wheel_fast");
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            inventory.UnlockPart("existing_part"); // simulates a returning player

            // Guard: only apply if inventory is empty.
            if (inventory.UnlockedPartIds.Count == 0)
                SimulateApply(config, inventory);

            // Starters must NOT have been added.
            Assert.AreEqual(1, inventory.UnlockedPartIds.Count);
            Assert.IsTrue(inventory.HasPart("existing_part"));
            Assert.IsFalse(inventory.HasPart("arm_heavy"));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(inventory);
        }

        [Test]
        public void ApplyStarters_AfterReset_StartersReapplied()
        {
            // After Reset() the inventory is empty again — starters would be
            // re-applied on next load (new-game behaviour).
            var config    = CreateConfig("arm_heavy");
            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            SimulateApply(config, inventory);
            Assert.AreEqual(1, inventory.UnlockedPartIds.Count);

            inventory.Reset();
            Assert.AreEqual(0, inventory.UnlockedPartIds.Count);

            // Re-apply (simulates next session after Reset/new-game).
            SimulateApply(config, inventory);
            Assert.AreEqual(1, inventory.UnlockedPartIds.Count);
            Assert.IsTrue(inventory.HasPart("arm_heavy"));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(inventory);
        }
    }
}

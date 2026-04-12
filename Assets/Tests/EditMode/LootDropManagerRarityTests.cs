using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests verifying the T106 rarity-config patch on <see cref="LootDropManager"/>.
    ///
    /// After the patch, <see cref="LootDropManager.AttemptDrop"/> calls
    /// <see cref="LootTableSO.RollDrop(int, PartRarityConfig)"/> rather than the
    /// base overload.  These tests confirm:
    ///
    ///   • The <c>_rarityConfig</c> inspector field exists (reflection guard).
    ///   • Passing a null config falls back to normal unscaled behaviour (no crash).
    ///   • Passing a valid config still produces a correct drop (rarity-aware path).
    ///   • Rarity-aware path: valid drop adds the part to inventory.
    ///   • Rarity-aware path: player-lost guard still suppresses drops.
    ///
    /// The base-path behaviour (null table, already-owned, notification queue, etc.) is
    /// covered by the original <see cref="LootDropManagerTests"/>; this file only tests
    /// the new rarity-config surface.
    ///
    /// Uses the inactive-GO pattern. SaveSystem.Delete() called in SetUp and TearDown.
    /// </summary>
    public class LootDropManagerRarityTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject       _go;
        private LootDropManager  _manager;
        private LootTableSO      _lootTable;
        private MatchResultSO    _matchResult;
        private PlayerInventory  _inventory;
        private PartRarityConfig _rarityConfig;
        private PartDefinition   _part;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static LootEntry MakeEntry(PartDefinition part, float weight)
            => new LootEntry { part = part, weight = weight };

        private void ActivateWithRarity(PartRarityConfig rarityConfig)
        {
            SetField(_manager, "_lootTable",    _lootTable);
            SetField(_manager, "_matchResult",  _matchResult);
            SetField(_manager, "_inventory",    _inventory);
            SetField(_manager, "_rarityConfig", rarityConfig);
            _go.SetActive(true);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            _lootTable    = ScriptableObject.CreateInstance<LootTableSO>();
            _matchResult  = ScriptableObject.CreateInstance<MatchResultSO>();
            _inventory    = ScriptableObject.CreateInstance<PlayerInventory>();
            _rarityConfig = ScriptableObject.CreateInstance<PartRarityConfig>();
            _part         = ScriptableObject.CreateInstance<PartDefinition>();

            SetField(_part, "_partId",      "part_rarity_01");
            SetField(_part, "_displayName", "Rarity Test Part");

            // Single Common-rarity entry; WinDropChance = 1 (always attempt).
            SetField(_lootTable, "_entries",
                new List<LootEntry> { MakeEntry(_part, 1f) });
            SetField(_lootTable, "_winDropChance", 1f);

            // Default: player won.
            _matchResult.Write(playerWon: true, durationSeconds: 60f,
                               currencyEarned: 100, newWalletBalance: 200);

            _go      = new GameObject("LootDropManagerRarity");
            _go.SetActive(false);
            _manager = _go.AddComponent<LootDropManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_lootTable);
            Object.DestroyImmediate(_matchResult);
            Object.DestroyImmediate(_inventory);
            Object.DestroyImmediate(_rarityConfig);
            Object.DestroyImmediate(_part);
            SaveSystem.Delete();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void RarityConfigField_ExistsOnLootDropManager()
        {
            // Reflection guard: ensures the patch added the inspector field.
            FieldInfo fi = typeof(LootDropManager)
                .GetField("_rarityConfig", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi,
                "_rarityConfig field must exist on LootDropManager after the T106 rarity patch.");
            Assert.AreEqual(typeof(PartRarityConfig), fi.FieldType,
                "_rarityConfig must be of type PartRarityConfig.");
        }

        [Test]
        public void AttemptDrop_NullRarityConfig_StillDropsSuccessfully()
        {
            // Null rarityConfig → base RollDrop overload (backwards-compatible).
            ActivateWithRarity(null);

            _manager.AttemptDrop(0f, 42);

            Assert.IsTrue(_inventory.HasPart("part_rarity_01"),
                "Drop should succeed when _rarityConfig is null (uses base overload).");
        }

        [Test]
        public void AttemptDrop_WithRarityConfig_StillDropsSuccessfully()
        {
            // Non-null rarityConfig → rarity-aware RollDrop overload.
            // Empty tier list → GetLootWeightMultiplier returns 1f fallback for all
            // rarities, so the distribution is identical to the base overload.
            ActivateWithRarity(_rarityConfig);

            _manager.AttemptDrop(0f, 42);

            Assert.IsTrue(_inventory.HasPart("part_rarity_01"),
                "Drop should succeed when _rarityConfig is assigned (uses rarity-aware overload).");
        }

        [Test]
        public void AttemptDrop_WithRarityConfig_ValidDrop_PartPersistedToSave()
        {
            ActivateWithRarity(_rarityConfig);

            _manager.AttemptDrop(0f, 42);

            SaveData save = SaveSystem.Load();
            Assert.IsTrue(save.unlockedPartIds.Contains("part_rarity_01"),
                "Part should be persisted in SaveData after a rarity-config-aware drop.");
        }

        [Test]
        public void AttemptDrop_WithRarityConfig_PlayerLost_InventoryUnchanged()
        {
            // Loss guard must still suppress the drop even with rarityConfig assigned.
            _matchResult.Write(playerWon: false, durationSeconds: 60f,
                               currencyEarned: 0, newWalletBalance: 0);
            ActivateWithRarity(_rarityConfig);

            _manager.AttemptDrop(0f, 42);

            Assert.IsFalse(_inventory.HasPart("part_rarity_01"),
                "No drop should occur on a loss even when _rarityConfig is assigned.");
        }

        [Test]
        public void AttemptDrop_WithRarityConfig_NullTable_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",    null);
            SetField(_manager, "_matchResult",  _matchResult);
            SetField(_manager, "_inventory",    _inventory);
            SetField(_manager, "_rarityConfig", _rarityConfig);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _manager.AttemptDrop(0f, 42),
                "Null _lootTable with a non-null _rarityConfig must not throw.");
        }
    }
}

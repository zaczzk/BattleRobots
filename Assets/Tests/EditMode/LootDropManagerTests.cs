using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LootDropManager"/>.
    ///
    /// All tests drive the system through the public
    /// <see cref="LootDropManager.AttemptDrop"/> method, which accepts explicit
    /// <c>dropRoll</c> and <c>lootSeed</c> parameters so random values can be
    /// controlled without physics or coroutine simulation.
    ///
    /// Convention:
    ///   • <c>dropRoll = 0f</c>  → always passes the WinDropChance gate (0 &lt; any positive chance).
    ///   • <c>dropRoll = 1f</c>  → always fails the WinDropChance gate (1 ≥ any chance ≤ 1).
    ///
    /// Covers:
    ///   • Null guards: null _lootTable / _matchResult → DoesNotThrow.
    ///   • Loss guard: PlayerWon = false → no inventory change.
    ///   • WinDropChance = 0 → dropRoll(0f) still fails (0 ≥ 0).
    ///   • Already-owned part → no second unlock, no persistence change.
    ///   • Valid drop → inventory unlocked.
    ///   • Valid drop → SaveSystem persisted.
    ///   • Valid drop → NotificationQueueSO enqueued.
    ///   • Valid drop with null _inventory → DoesNotThrow (persistence still runs).
    ///   • Valid drop with null _notificationQueue → DoesNotThrow.
    ///   • OnEnable / OnDisable with null _onMatchEnded → DoesNotThrow.
    ///   • OnDisable unregisters: raising event after disable does NOT trigger drop.
    ///
    /// Uses the inactive-GO pattern. SaveSystem.Delete() called in SetUp and TearDown.
    /// </summary>
    public class LootDropManagerTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject       _go;
        private LootDropManager  _manager;
        private LootTableSO      _lootTable;
        private MatchResultSO    _matchResult;
        private PlayerInventory  _inventory;
        private NotificationQueueSO _notificationQueue;
        private VoidGameEvent    _onMatchEnded;
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

        /// <summary>
        /// Wires all standard fields and activates the GO (triggering Awake + OnEnable).
        /// </summary>
        private void ActivateWithDefaults()
        {
            SetField(_manager, "_lootTable",         _lootTable);
            SetField(_manager, "_matchResult",       _matchResult);
            SetField(_manager, "_inventory",         _inventory);
            SetField(_manager, "_notificationQueue", _notificationQueue);
            SetField(_manager, "_onMatchEnded",      _onMatchEnded);
            _go.SetActive(true);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Delete();

            _lootTable         = ScriptableObject.CreateInstance<LootTableSO>();
            _matchResult       = ScriptableObject.CreateInstance<MatchResultSO>();
            _inventory         = ScriptableObject.CreateInstance<PlayerInventory>();
            _notificationQueue = ScriptableObject.CreateInstance<NotificationQueueSO>();
            _onMatchEnded      = ScriptableObject.CreateInstance<VoidGameEvent>();
            _part              = ScriptableObject.CreateInstance<PartDefinition>();

            SetField(_part, "_partId",      "part_loot_01");
            SetField(_part, "_displayName", "Loot Part");

            // Configure loot table: single entry, always-eligible (WinDropChance = 1).
            SetField(_lootTable, "_entries",       new List<LootEntry> { MakeEntry(_part, 1f) });
            SetField(_lootTable, "_winDropChance", 1f);

            // Default match result: player won.
            _matchResult.Write(playerWon: true, durationSeconds: 60f,
                               currencyEarned: 100, newWalletBalance: 300);

            _go      = new GameObject("LootDropManager");
            _go.SetActive(false);   // inactive until fields are injected
            _manager = _go.AddComponent<LootDropManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_lootTable);
            Object.DestroyImmediate(_matchResult);
            Object.DestroyImmediate(_inventory);
            Object.DestroyImmediate(_notificationQueue);
            Object.DestroyImmediate(_onMatchEnded);
            Object.DestroyImmediate(_part);
            SaveSystem.Delete();
        }

        // ── Null guard tests ──────────────────────────────────────────────────

        [Test]
        public void AttemptDrop_NullLootTable_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",   null);
            SetField(_manager, "_matchResult", _matchResult);
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _manager.AttemptDrop(0f, 42));
        }

        [Test]
        public void AttemptDrop_NullMatchResult_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",   _lootTable);
            SetField(_manager, "_matchResult", null);
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _manager.AttemptDrop(0f, 42));
        }

        [Test]
        public void OnEnable_NullOnMatchEnded_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",    _lootTable);
            SetField(_manager, "_matchResult",  _matchResult);
            SetField(_manager, "_onMatchEnded", null);
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void OnDisable_NullOnMatchEnded_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",    _lootTable);
            SetField(_manager, "_matchResult",  _matchResult);
            SetField(_manager, "_onMatchEnded", null);
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── Guard condition tests ─────────────────────────────────────────────

        [Test]
        public void AttemptDrop_PlayerLost_InventoryUnchanged()
        {
            _matchResult.Write(playerWon: false, durationSeconds: 60f,
                               currencyEarned: 0, newWalletBalance: 0);
            ActivateWithDefaults();

            _manager.AttemptDrop(0f, 42);

            Assert.IsFalse(_inventory.HasPart("part_loot_01"));
        }

        [Test]
        public void AttemptDrop_DropChanceZero_InventoryUnchanged()
        {
            // WinDropChance = 0 → dropRoll(0f) fails because 0f >= 0f.
            SetField(_lootTable, "_winDropChance", 0f);
            ActivateWithDefaults();

            _manager.AttemptDrop(0f, 42);

            Assert.IsFalse(_inventory.HasPart("part_loot_01"));
        }

        [Test]
        public void AttemptDrop_DropRollTooHigh_InventoryUnchanged()
        {
            // dropRoll(1f) >= WinDropChance(1f) → no drop.
            ActivateWithDefaults();

            _manager.AttemptDrop(1f, 42);

            Assert.IsFalse(_inventory.HasPart("part_loot_01"));
        }

        [Test]
        public void AttemptDrop_AlreadyOwned_InventoryCountUnchanged()
        {
            ActivateWithDefaults();
            _inventory.UnlockPart("part_loot_01");   // player already owns it

            int before = _inventory.UnlockedPartIds.Count;
            _manager.AttemptDrop(0f, 42);

            Assert.AreEqual(before, _inventory.UnlockedPartIds.Count,
                "Inventory count should not change for already-owned parts.");
        }

        // ── Success path tests ────────────────────────────────────────────────

        [Test]
        public void AttemptDrop_ValidDrop_AddsPartToInventory()
        {
            ActivateWithDefaults();

            _manager.AttemptDrop(0f, 42);

            Assert.IsTrue(_inventory.HasPart("part_loot_01"),
                "Inventory should contain the dropped part after a valid drop.");
        }

        [Test]
        public void AttemptDrop_ValidDrop_PartPersistedToSave()
        {
            ActivateWithDefaults();

            _manager.AttemptDrop(0f, 42);

            SaveData save = SaveSystem.Load();
            Assert.IsTrue(save.unlockedPartIds.Contains("part_loot_01"),
                "Dropped part ID should appear in SaveData.unlockedPartIds.");
        }

        [Test]
        public void AttemptDrop_ValidDrop_EnqueuesNotification()
        {
            ActivateWithDefaults();

            _manager.AttemptDrop(0f, 42);

            Assert.IsTrue(_notificationQueue.Count > 0,
                "A notification should be enqueued after a successful loot drop.");
        }

        [Test]
        public void AttemptDrop_NullInventory_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",         _lootTable);
            SetField(_manager, "_matchResult",       _matchResult);
            SetField(_manager, "_inventory",         null);
            SetField(_manager, "_notificationQueue", _notificationQueue);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _manager.AttemptDrop(0f, 42),
                "Null _inventory should be handled gracefully.");
        }

        [Test]
        public void AttemptDrop_NullNotificationQueue_DoesNotThrow()
        {
            SetField(_manager, "_lootTable",         _lootTable);
            SetField(_manager, "_matchResult",       _matchResult);
            SetField(_manager, "_inventory",         _inventory);
            SetField(_manager, "_notificationQueue", null);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _manager.AttemptDrop(0f, 42),
                "Null _notificationQueue should be handled gracefully.");
        }

        [Test]
        public void AttemptDrop_NullInventory_StillPersistsToSave()
        {
            SetField(_manager, "_lootTable",         _lootTable);
            SetField(_manager, "_matchResult",       _matchResult);
            SetField(_manager, "_inventory",         null);
            SetField(_manager, "_notificationQueue", null);
            _go.SetActive(true);

            _manager.AttemptDrop(0f, 42);

            SaveData save = SaveSystem.Load();
            Assert.IsTrue(save.unlockedPartIds.Contains("part_loot_01"),
                "Part should be persisted even when _inventory SO is null.");
        }

        // ── OnDisable unregistration ──────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersCallback_RaisingEventDoesNotDrop()
        {
            ActivateWithDefaults();
            _go.SetActive(false);    // triggers OnDisable → unregisters callback

            _onMatchEnded.Raise();

            Assert.IsFalse(_inventory.HasPart("part_loot_01"),
                "After OnDisable the manager must not react to the event channel.");
        }
    }
}

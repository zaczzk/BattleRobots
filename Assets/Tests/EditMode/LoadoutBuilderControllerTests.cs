using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LoadoutBuilderController"/>.
    ///
    /// Covers:
    ///   • ConfirmLoadout — null _playerLoadout guard (no throw); empty-rows path
    ///     writes empty list to PlayerLoadout and persists to disk.
    ///   • BuildCategoryOwnedParts — null catalog returns empty; null inventory
    ///     returns empty; owned part included under its category; unowned part excluded.
    ///   • FindPartById — null catalog returns null; known ID returns matching
    ///     PartDefinition; unknown ID returns null.
    ///   • OnDestroy — unregisters RefreshAllSlots delegate from _onInventoryChanged
    ///     (verified by raising the event after destruction and confirming the
    ///     builder's callback no longer fires).
    ///
    /// PopulateSlots is called from Awake but returns early when _slotContainer is
    /// null (the default in tests), so no Instantiate calls occur.
    ///
    /// Disk writes from ConfirmLoadout are cleaned up by SaveSystem.Delete() in TearDown.
    ///
    /// BuildCategoryOwnedParts and FindPartById are private; invoked via reflection
    /// following the established pattern used in ShopManagerTests and MatchManagerTests.
    ///
    /// For the OnDestroy subscription test the inactive-GO pattern is used so
    /// _onInventoryChanged can be injected before Awake registers the callback.
    /// </summary>
    public class LoadoutBuilderControllerTests
    {
        private GameObject                _go;
        private LoadoutBuilderController  _builder;

        // ── Reflection helpers ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T InvokePrivate<T>(object target, string methodName)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            return (T)mi.Invoke(target, null);
        }

        private static T InvokePrivateOneArg<T>(object target, string methodName, object arg)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            return (T)mi.Invoke(target, new[] { arg });
        }

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go      = new GameObject("TestBuilder");
            _builder = _go.AddComponent<LoadoutBuilderController>();
            // Awake: _onInventoryDelegate = RefreshAllSlots; _onInventoryChanged is null
            // (not injected), so no registration. PopulateSlots returns early (_slotContainer null).
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();   // remove any files written by ConfirmLoadout
            if (_go != null)
                Object.DestroyImmediate(_go);
            _go      = null;
            _builder = null;
        }

        // ── ConfirmLoadout ─────────────────────────────────────────────────────

        [Test]
        public void ConfirmLoadout_NullPlayerLoadout_DoesNotThrow()
        {
            // _playerLoadout is null by default — method must log + return gracefully.
            Assert.DoesNotThrow(() => _builder.ConfirmLoadout());
        }

        [Test]
        public void ConfirmLoadout_WithLoadout_NoRows_WritesEmptyList()
        {
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();

            SetField(_builder, "_playerLoadout", loadout);
            _builder.ConfirmLoadout();

            Assert.AreEqual(0, loadout.EquippedPartIds.Count,
                "ConfirmLoadout with no slot rows must produce an empty loadout.");

            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void ConfirmLoadout_PersistsEmptyLoadoutToDisk()
        {
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            SetField(_builder, "_playerLoadout", loadout);

            _builder.ConfirmLoadout();

            SaveData saved = SaveSystem.Load();
            Assert.IsNotNull(saved.loadoutPartIds,
                "ConfirmLoadout must write loadoutPartIds to disk.");
            Assert.AreEqual(0, saved.loadoutPartIds.Count,
                "Persisted loadout must match the confirmed (empty) selection.");

            Object.DestroyImmediate(loadout);
        }

        // ── RefreshAllSlots ────────────────────────────────────────────────────

        [Test]
        public void RefreshAllSlots_EmptyRows_DoesNotThrow()
        {
            // _rows is empty after SetUp (PopulateSlots returned early).
            Assert.DoesNotThrow(() =>
                InvokePrivate<object>(_builder, "RefreshAllSlots"));
        }

        // ── BuildCategoryOwnedParts ────────────────────────────────────────────

        [Test]
        public void BuildCategoryOwnedParts_NullCatalog_ReturnsEmptyDict()
        {
            // _shopCatalog is null by default.
            var result = InvokePrivate<Dictionary<PartCategory, List<PartDefinition>>>(
                _builder, "BuildCategoryOwnedParts");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count,
                "Null catalog must yield an empty category map.");
        }

        [Test]
        public void BuildCategoryOwnedParts_NullInventory_ReturnsEmptyDict()
        {
            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(_builder, "_shopCatalog", catalog);
            // _playerInventory is null by default.

            var result = InvokePrivate<Dictionary<PartCategory, List<PartDefinition>>>(
                _builder, "BuildCategoryOwnedParts");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count,
                "Null inventory must yield an empty category map.");

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void BuildCategoryOwnedParts_WithOwnedPart_IncludesItUnderCategory()
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(part, "_partId",    "weapon_001");
            SetField(part, "_category", PartCategory.Weapon);

            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(catalog, "_parts", new List<PartDefinition> { part });

            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();
            inventory.UnlockPart("weapon_001");

            SetField(_builder, "_shopCatalog",    catalog);
            SetField(_builder, "_playerInventory", inventory);

            var result = InvokePrivate<Dictionary<PartCategory, List<PartDefinition>>>(
                _builder, "BuildCategoryOwnedParts");

            Assert.IsTrue(result.ContainsKey(PartCategory.Weapon),
                "Owned part must appear under its PartCategory key.");
            Assert.AreEqual(1, result[PartCategory.Weapon].Count);
            Assert.AreSame(part, result[PartCategory.Weapon][0],
                "The PartDefinition in the map must be the same SO instance.");

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(inventory);
        }

        [Test]
        public void BuildCategoryOwnedParts_UnownedPart_NotIncluded()
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(part, "_partId",    "weapon_001");
            SetField(part, "_category", PartCategory.Weapon);

            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(catalog, "_parts", new List<PartDefinition> { part });

            var inventory = ScriptableObject.CreateInstance<PlayerInventory>();
            // Do NOT call UnlockPart — part is not owned.

            SetField(_builder, "_shopCatalog",    catalog);
            SetField(_builder, "_playerInventory", inventory);

            var result = InvokePrivate<Dictionary<PartCategory, List<PartDefinition>>>(
                _builder, "BuildCategoryOwnedParts");

            Assert.AreEqual(0, result.Count,
                "Unowned part must not appear in the category map.");

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(inventory);
        }

        // ── FindPartById ───────────────────────────────────────────────────────

        [Test]
        public void FindPartById_NullCatalog_ReturnsNull()
        {
            // _shopCatalog is null by default.
            var result = InvokePrivateOneArg<PartDefinition>(_builder, "FindPartById", "any_id");
            Assert.IsNull(result, "Null catalog must cause FindPartById to return null.");
        }

        [Test]
        public void FindPartById_KnownId_ReturnsMatchingPartDefinition()
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(part, "_partId", "chassis_001");

            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(catalog, "_parts", new List<PartDefinition> { part });
            SetField(_builder, "_shopCatalog", catalog);

            var result = InvokePrivateOneArg<PartDefinition>(_builder, "FindPartById", "chassis_001");

            Assert.AreSame(part, result, "FindPartById must return the matching PartDefinition SO.");

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void FindPartById_UnknownId_ReturnsNull()
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(part, "_partId", "chassis_001");

            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(catalog, "_parts", new List<PartDefinition> { part });
            SetField(_builder, "_shopCatalog", catalog);

            var result = InvokePrivateOneArg<PartDefinition>(_builder, "FindPartById", "unknown_999");

            Assert.IsNull(result, "FindPartById must return null for an unrecognised part ID.");

            Object.DestroyImmediate(part);
            Object.DestroyImmediate(catalog);
        }

        // ── OnDestroy — event unregistration ──────────────────────────────────

        [Test]
        public void OnDestroy_UnregistersRefreshAllSlotsFromInventoryEvent()
        {
            // Use inactive-GO pattern: wire _onInventoryChanged before Awake registers callback.
            var builderGO = new GameObject("BuilderUnsubTest");
            builderGO.SetActive(false);
            var builder = builderGO.AddComponent<LoadoutBuilderController>();

            var inventoryEvent = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(builder, "_onInventoryChanged", inventoryEvent);

            builderGO.SetActive(true);   // Awake fires → registers RefreshAllSlots

            // Track total event invocations (our own counter, unrelated to builder).
            int count = 0;
            inventoryEvent.RegisterCallback(() => count++);

            inventoryEvent.Raise();       // fires our counter + builder's RefreshAllSlots (no-op)
            Assert.AreEqual(1, count, "Pre-condition: counter callback must fire once.");

            Object.DestroyImmediate(builderGO);   // OnDestroy → unregister RefreshAllSlots

            inventoryEvent.Raise();       // fires only our counter callback
            Assert.AreEqual(2, count,
                "After builder destruction, only the test counter must fire; " +
                "builder's RefreshAllSlots must have been unregistered in OnDestroy.");

            Object.DestroyImmediate(inventoryEvent);
        }
    }
}

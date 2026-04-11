using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShopCatalogView"/>.
    ///
    /// Covers:
    ///   • Awake null-safety: activating ShopCatalogView with any combination of
    ///       null required refs must not throw (PopulateCatalog logs a warning and
    ///       returns early; this is the expected behaviour when the scene is not
    ///       fully wired).
    ///   • OnDestroy null-channel safety: destroying a ShopCatalogView whose
    ///       event-channel SO fields were never assigned must not throw
    ///       (the ?. operator must guard the UnregisterCallback calls).
    ///   • OnDestroy unregisters the void refresh delegate from
    ///       <c>_onInventoryChanged</c> (inactive-GO pattern; verified via external
    ///       counter that remains the sole registered callback after destruction).
    ///   • OnDestroy unregisters the int refresh delegate from
    ///       <c>_onBalanceChanged</c> (same pattern, IntGameEvent variant).
    ///
    /// ShopCatalogView is a MonoBehaviour; all tests use headless GameObjects
    /// created inline and destroyed at the end of each test.  No uGUI scene
    /// objects are required.
    /// </summary>
    public class ShopCatalogViewTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helper ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a headless inactive ShopCatalogView ready for field injection.
        /// </summary>
        private static (GameObject go, ShopCatalogView view) MakeView()
        {
            var go   = new GameObject("ShopCatalogView");
            go.SetActive(false);
            var view = go.AddComponent<ShopCatalogView>();
            return (go, view);
        }

        // ── Awake — null ref guards (PopulateCatalog early-return) ────────────

        [Test]
        public void Awake_AllNullRefs_DoesNotThrow()
        {
            // No fields set — PopulateCatalog should log a warning and exit cleanly.
            var (go, _) = MakeView();
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Activating ShopCatalogView with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Awake_NullShopManager_DoesNotThrow()
        {
            var (go, view) = MakeView();
            // Assign prefab and container but leave _shopManager null.
            var containerGO = new GameObject("Container");
            SetField(view, "_itemContainer", containerGO.transform);
            // _itemPrefab and _shopManager remain null → PopulateCatalog early-returns.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Activating ShopCatalogView with null _shopManager must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(containerGO);
        }

        [Test]
        public void Awake_NullItemPrefab_DoesNotThrow()
        {
            var (go, view) = MakeView();
            var containerGO = new GameObject("Container");
            SetField(view, "_itemContainer", containerGO.transform);
            // _itemPrefab null; _shopManager null → PopulateCatalog early-returns.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Activating ShopCatalogView with null _itemPrefab must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(containerGO);
        }

        [Test]
        public void Awake_NullItemContainer_DoesNotThrow()
        {
            var (go, _) = MakeView();
            // _itemContainer null → PopulateCatalog first check catches it.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Activating ShopCatalogView with null _itemContainer must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnDestroy — null channel safety ───────────────────────────────────

        [Test]
        public void OnDestroy_NullEventChannels_DoesNotThrow()
        {
            // Event channels not assigned. Awake registers nothing; OnDestroy must
            // guard the UnregisterCallback calls via ?. operators.
            var (go, _) = MakeView();
            go.SetActive(true); // Awake runs; channels are null → no registration.
            Assert.DoesNotThrow(() => Object.DestroyImmediate(go),
                "Destroying ShopCatalogView with null event channels must not throw.");
        }

        // ── OnDestroy — unregisters void delegate from _onInventoryChanged ────

        [Test]
        public void OnDestroy_UnregistersFromInventoryChanged_VoidCallback()
        {
            var inventoryEvent = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counter — the sole callback that should remain after destroy.
            int externalCount = 0;
            inventoryEvent.RegisterCallback(() => externalCount++);

            var (go, view) = MakeView();
            SetField(view, "_onInventoryChanged", inventoryEvent);

            go.SetActive(true);            // Awake creates _refreshAllVoid and registers it.
            Object.DestroyImmediate(go);   // OnDestroy unregisters _refreshAllVoid.

            // Raise event — only the external counter should fire.
            inventoryEvent.Raise();

            Assert.AreEqual(1, externalCount,
                "After ShopCatalogView is destroyed, only the external counter " +
                "(not _refreshAllVoid) should fire on _onInventoryChanged.");

            Object.DestroyImmediate(inventoryEvent);
        }

        // ── OnDestroy — unregisters int delegate from _onBalanceChanged ───────

        [Test]
        public void OnDestroy_UnregistersFromBalanceChanged_IntCallback()
        {
            var balanceEvent = ScriptableObject.CreateInstance<IntGameEvent>();

            int externalCount = 0;
            balanceEvent.RegisterCallback((int _) => externalCount++);

            var (go, view) = MakeView();
            SetField(view, "_onBalanceChanged", balanceEvent);

            go.SetActive(true);           // Awake creates _refreshAllInt and registers it.
            Object.DestroyImmediate(go);  // OnDestroy unregisters _refreshAllInt.

            balanceEvent.Raise(500);

            Assert.AreEqual(1, externalCount,
                "After ShopCatalogView is destroyed, only the external counter " +
                "(not _refreshAllInt) should fire on _onBalanceChanged.");

            Object.DestroyImmediate(balanceEvent);
        }
    }
}

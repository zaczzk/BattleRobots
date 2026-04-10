using System;
using System.Collections.Generic;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Populates the shop panel by instantiating one <see cref="ShopItemController"/>
    /// prefab row per <see cref="PartDefinition"/> in the assigned <see cref="ShopCatalog"/>.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Instantiates and configures item rows under _itemContainer on Awake.
    ///   • Subscribes to _onInventoryChanged (VoidGameEvent) and _onBalanceChanged
    ///     (IntGameEvent) SO channels and propagates refreshes to every row.
    ///   • Cleans up subscriptions on OnDestroy.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   • Assign _shopManager — must reference a ShopManager in the same scene.
    ///   • Assign _itemPrefab — a prefab with a <see cref="ShopItemController"/> component.
    ///   • Assign _itemContainer — the ScrollRect content Transform that rows are
    ///     parented to (typically a VerticalLayoutGroup).
    ///   • Assign _onInventoryChanged → same VoidGameEvent SO as PlayerInventory._onInventoryChanged.
    ///   • Assign _onBalanceChanged   → same IntGameEvent SO as PlayerWallet._onBalanceChanged.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update or FixedUpdate — purely event-driven after Awake.
    ///   - Delegates for SO channels are cached in Awake (one-time allocations).
    ///   - <see cref="_items"/> uses a pre-allocated List; no per-frame allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopCatalogView : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The ShopManager in this scene. Provides catalog + BuyPart API.")]
        [SerializeField] private ShopManager _shopManager;

        [Header("Layout")]
        [Tooltip("Prefab with a ShopItemController component. Instantiated once per catalog entry.")]
        [SerializeField] private GameObject _itemPrefab;

        [Tooltip("Parent Transform for instantiated rows (e.g. ScrollRect Content with VerticalLayoutGroup).")]
        [SerializeField] private Transform _itemContainer;

        [Header("Event Channels — In")]
        [Tooltip("Same SO as PlayerInventory._onInventoryChanged. Triggers a refresh of all rows.")]
        [SerializeField] private VoidGameEvent _onInventoryChanged;

        [Tooltip("Same SO as PlayerWallet._onBalanceChanged. Triggers a refresh of all rows.")]
        [SerializeField] private IntGameEvent _onBalanceChanged;

        // ── Runtime ───────────────────────────────────────────────────────────

        private readonly List<ShopItemController> _items = new List<ShopItemController>();

        // Delegates cached once in Awake — zero alloc on event fire.
        private Action     _refreshAllVoid;
        private Action<int> _refreshAllInt;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshAllVoid = RefreshAll;
            _refreshAllInt  = _ => RefreshAll();

            _onInventoryChanged?.RegisterCallback(_refreshAllVoid);
            _onBalanceChanged?.RegisterCallback(_refreshAllInt);

            PopulateCatalog();
        }

        private void OnDestroy()
        {
            _onInventoryChanged?.UnregisterCallback(_refreshAllVoid);
            _onBalanceChanged?.UnregisterCallback(_refreshAllInt);
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys any existing child rows in _itemContainer, then instantiates
        /// one row prefab per PartDefinition in _shopManager.Catalog.
        /// </summary>
        private void PopulateCatalog()
        {
            if (_itemContainer == null || _itemPrefab == null || _shopManager == null)
            {
                Debug.LogWarning("[ShopCatalogView] Missing required references — catalog not populated.", this);
                return;
            }

            // Clear design-time placeholder rows.
            for (int i = _itemContainer.childCount - 1; i >= 0; i--)
                Destroy(_itemContainer.GetChild(i).gameObject);
            _items.Clear();

            IReadOnlyList<PartDefinition> parts = _shopManager.Catalog?.Parts;
            if (parts == null || parts.Count == 0) return;

            for (int i = 0; i < parts.Count; i++)
            {
                PartDefinition part = parts[i];
                if (part == null) continue;

                GameObject row = Instantiate(_itemPrefab, _itemContainer);
                ShopItemController ctrl = row.GetComponent<ShopItemController>();

                if (ctrl == null)
                {
                    Debug.LogWarning("[ShopCatalogView] _itemPrefab is missing a " +
                                     "ShopItemController component — row skipped.", this);
                    Destroy(row);
                    continue;
                }

                ctrl.Setup(part, _shopManager);
                _items.Add(ctrl);
            }
        }

        /// <summary>
        /// Refreshes the dynamic state (owned badge, button interactability, cost label)
        /// of every instantiated row.  Called whenever the inventory or wallet changes.
        /// </summary>
        private void RefreshAll()
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i].Refresh();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_shopManager == null)
                Debug.LogWarning("[ShopCatalogView] _shopManager not assigned.", this);
            if (_itemPrefab == null)
                Debug.LogWarning("[ShopCatalogView] _itemPrefab not assigned.", this);
            if (_itemContainer == null)
                Debug.LogWarning("[ShopCatalogView] _itemContainer not assigned.", this);
            if (_onInventoryChanged == null)
                Debug.LogWarning("[ShopCatalogView] _onInventoryChanged not assigned — " +
                                 "rows will not refresh after purchases.", this);
            if (_onBalanceChanged == null)
                Debug.LogWarning("[ShopCatalogView] _onBalanceChanged not assigned — " +
                                 "buy-button state may be stale after wallet changes.", this);
        }
#endif
    }
}

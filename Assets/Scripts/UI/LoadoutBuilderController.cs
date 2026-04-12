using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match assembly UI: lets the player configure their robot loadout by
    /// selecting owned parts for each slot category before entering the arena.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Instantiates one <see cref="LoadoutSlotController"/> row per unique
    ///     <see cref="PartCategory"/> present in the <see cref="RobotDefinition.Slots"/> list.
    ///   • Filters candidates by <see cref="PlayerInventory.HasPart"/> so only owned
    ///     parts appear in the picker.
    ///   • Pre-selects parts that match the current <see cref="PlayerLoadout.EquippedPartIds"/>.
    ///   • <see cref="ConfirmLoadout"/> collects one selection per slot row, calls
    ///     <see cref="PlayerLoadout.SetLoadout"/>, and persists the selection to disk.
    ///   • Subscribes to <see cref="_onInventoryChanged"/> so newly purchased parts
    ///     appear in the picker without a scene reload.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this component to the loadout-panel root GameObject.
    ///   2. Assign data SOs: _playerInventory, _playerLoadout, _shopCatalog, _robotDefinition.
    ///   3. Assign _onInventoryChanged → same VoidGameEvent as PlayerInventory._onInventoryChanged.
    ///   4. Assign _slotRowPrefab — a prefab with a <see cref="LoadoutSlotController"/> component.
    ///   5. Assign _slotContainer — a ScrollRect Content Transform (VerticalLayoutGroup recommended).
    ///   6. Optionally assign a _confirmButton and wire its onClick → <see cref="ConfirmLoadout"/>.
    ///   7. Optionally assign the stats preview Text refs for a live combat-stats readout.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update or FixedUpdate.
    ///   - Delegates cached in Awake; zero alloc after Awake.
    ///   - Persist uses the load → mutate → Save round-trip pattern (same as ShopManager).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadoutBuilderController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Tracks which parts the player owns. Used to filter slot candidates.")]
        [SerializeField] private PlayerInventory _playerInventory;

        [Tooltip("Runtime loadout SO. ConfirmLoadout() writes the final selection here.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("All purchasable parts. Filtered by PlayerInventory to build candidate lists.")]
        [SerializeField] private ShopCatalog _shopCatalog;

        [Tooltip("Chassis definition — provides the slot list that determines which category " +
                 "rows appear in the builder.")]
        [SerializeField] private RobotDefinition _robotDefinition;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same SO as PlayerInventory._onInventoryChanged. " +
                 "Triggers a rebuild of all slot candidate lists when new parts are purchased.")]
        [SerializeField] private VoidGameEvent _onInventoryChanged;

        // ── Inspector — Layout ────────────────────────────────────────────────

        [Header("Layout")]
        [Tooltip("Prefab with a LoadoutSlotController component. One row per slot category.")]
        [SerializeField] private GameObject _slotRowPrefab;

        [Tooltip("Parent Transform for instantiated rows (VerticalLayoutGroup recommended).")]
        [SerializeField] private Transform _slotContainer;

        // ── Inspector — Controls ──────────────────────────────────────────────

        [Header("Controls (optional)")]
        [Tooltip("When clicked, calls ConfirmLoadout(). May also be wired via Inspector button.")]
        [SerializeField] private Button _confirmButton;

        [Tooltip("When assigned, displays validation warnings so the player knows why the " +
                 "confirm button is blocked. Shows an empty string when the loadout is valid.")]
        [SerializeField] private Text _validationWarningText;

        // ── Inspector — Upgrade System ────────────────────────────────────────

        [Header("Upgrade System (optional)")]
        [Tooltip("When assigned, the stats preview uses the upgrade-aware " +
                 "RobotStatsAggregator.Compute() overload so tier bonuses are reflected. " +
                 "Assign the same PlayerPartUpgrades SO as UpgradeManager and GameBootstrapper.")]
        [SerializeField] private PlayerPartUpgrades _playerPartUpgrades;

        [Tooltip("When assigned together with _playerPartUpgrades, tier stat multipliers are " +
                 "applied in the preview. Assign the same PartUpgradeConfig SO as UpgradeManager.")]
        [SerializeField] private PartUpgradeConfig _upgradeConfig;

        // ── Inspector — Stats Preview ─────────────────────────────────────────

        [Header("Stats Preview (optional)")]
        [Tooltip("Displays TotalMaxHealth after part contributions.")]
        [SerializeField] private Text _healthText;

        [Tooltip("Displays EffectiveSpeed after part contributions.")]
        [SerializeField] private Text _speedText;

        [Tooltip("Displays EffectiveDamageMultiplier after part contributions.")]
        [SerializeField] private Text _damageText;

        [Tooltip("Displays TotalArmorRating after part contributions.")]
        [SerializeField] private Text _armorText;

        // ── Runtime ───────────────────────────────────────────────────────────

        private readonly List<LoadoutSlotController> _rows = new List<LoadoutSlotController>();
        private Action _onInventoryDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onInventoryDelegate = RefreshAllSlots;

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(ConfirmLoadout);

            _onInventoryChanged?.RegisterCallback(_onInventoryDelegate);

            PopulateSlots();
        }

        private void OnDestroy()
        {
            _onInventoryChanged?.UnregisterCallback(_onInventoryDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Collects the current selection from every slot row, writes it to
        /// <see cref="_playerLoadout"/>, and persists the change to disk using the
        /// load → mutate → Save round-trip.
        ///
        /// Safe to call from a Button.onClick event.
        /// </summary>
        public void ConfirmLoadout()
        {
            if (_playerLoadout == null)
            {
                Debug.LogWarning("[LoadoutBuilderController] _playerLoadout not assigned — " +
                                 "cannot confirm loadout.", this);
                return;
            }

            // Collect one part ID per slot row (skip None selections).
            var selectedIds = new List<string>(_rows.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                PartDefinition selected = _rows[i].GetSelectedPartDef();
                if (selected != null)
                    selectedIds.Add(selected.PartId);
            }

            // Validate before committing.
            LoadoutValidationResult validation = LoadoutValidator.Validate(
                selectedIds, _robotDefinition, _playerInventory, _shopCatalog);

            if (_validationWarningText != null)
            {
                _validationWarningText.text = validation.IsValid
                    ? string.Empty
                    : validation.Errors[0];
            }

            if (!validation.IsValid)
            {
                Debug.LogWarning("[LoadoutBuilderController] Loadout is invalid — confirm blocked. " +
                                 $"First error: {validation.Errors[0]}", this);
                return;
            }

            // Write runtime state.
            _playerLoadout.SetLoadout(selectedIds);

            // Persist: load → mutate → save (preserves match history, settings, inventory).
            SaveData data = SaveSystem.Load();
            data.loadoutPartIds = selectedIds;
            SaveSystem.Save(data);

            if (_validationWarningText != null)
                _validationWarningText.text = string.Empty;

            Debug.Log($"[LoadoutBuilderController] Loadout confirmed: {selectedIds.Count} part(s) equipped.");

            // Update stats preview after confirming.
            RefreshStatsPreview();
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates one <see cref="LoadoutSlotController"/> row per unique slot category
        /// defined in <see cref="_robotDefinition"/>.
        /// </summary>
        private void PopulateSlots()
        {
            if (_slotContainer == null || _slotRowPrefab == null)
            {
                Debug.LogWarning("[LoadoutBuilderController] _slotContainer or _slotRowPrefab " +
                                 "not assigned — slots not populated.", this);
                return;
            }

            if (_robotDefinition == null)
            {
                Debug.LogWarning("[LoadoutBuilderController] _robotDefinition not assigned — " +
                                 "no slot categories to display.", this);
                return;
            }

            // Clear any design-time placeholder children.
            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
                Destroy(_slotContainer.GetChild(i).gameObject);
            _rows.Clear();

            // Determine unique categories in slot order (preserve first-occurrence ordering).
            var seenCategories = new HashSet<PartCategory>();
            var uniqueSlots    = new List<PartSlot>();
            foreach (PartSlot slot in _robotDefinition.Slots)
            {
                if (slot != null && seenCategories.Add(slot.category))
                    uniqueSlots.Add(slot);
            }

            // Build a full category → owned-parts lookup once.
            var categoryParts = BuildCategoryOwnedParts();

            // Pre-select based on the saved loadout.
            IReadOnlyList<string> savedIds = _playerLoadout != null
                ? _playerLoadout.EquippedPartIds
                : null;

            // Determine the first saved ID per category.
            var firstSavedPerCategory = new Dictionary<PartCategory, string>();
            if (savedIds != null)
            {
                foreach (string id in savedIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    PartDefinition def = FindPartById(id);
                    if (def != null && !firstSavedPerCategory.ContainsKey(def.Category))
                        firstSavedPerCategory[def.Category] = id;
                }
            }

            // Instantiate one row per unique category.
            for (int i = 0; i < uniqueSlots.Count; i++)
            {
                PartCategory cat = uniqueSlots[i].category;

                GameObject rowGo = Instantiate(_slotRowPrefab, _slotContainer);
                LoadoutSlotController ctrl = rowGo.GetComponent<LoadoutSlotController>();

                if (ctrl == null)
                {
                    Debug.LogWarning("[LoadoutBuilderController] _slotRowPrefab is missing a " +
                                     "LoadoutSlotController component — row skipped.", this);
                    Destroy(rowGo);
                    continue;
                }

                categoryParts.TryGetValue(cat, out List<PartDefinition> owned);
                firstSavedPerCategory.TryGetValue(cat, out string savedId);

                ctrl.Setup(cat, owned, savedId);
                _rows.Add(ctrl);
            }

            RefreshStatsPreview();
        }

        /// <summary>
        /// Rebuilds the candidate list for every slot row when the inventory changes
        /// (e.g. after a purchase in the shop).
        /// </summary>
        private void RefreshAllSlots()
        {
            var categoryParts = BuildCategoryOwnedParts();

            for (int i = 0; i < _rows.Count; i++)
            {
                categoryParts.TryGetValue(_rows[i].Category, out List<PartDefinition> owned);
                _rows[i].RebuildCandidates(owned);
            }

            RefreshStatsPreview();
        }

        /// <summary>
        /// Builds a dictionary mapping each <see cref="PartCategory"/> to the list of
        /// <see cref="PartDefinition"/>s the player currently owns in that category.
        /// </summary>
        private Dictionary<PartCategory, List<PartDefinition>> BuildCategoryOwnedParts()
        {
            var result = new Dictionary<PartCategory, List<PartDefinition>>();

            if (_shopCatalog == null || _playerInventory == null)
                return result;

            IReadOnlyList<PartDefinition> allParts = _shopCatalog.Parts;
            for (int i = 0; i < allParts.Count; i++)
            {
                PartDefinition def = allParts[i];
                if (def == null) continue;
                if (!_playerInventory.HasPart(def.PartId)) continue;

                if (!result.TryGetValue(def.Category, out List<PartDefinition> list))
                {
                    list = new List<PartDefinition>();
                    result[def.Category] = list;
                }
                list.Add(def);
            }

            return result;
        }

        /// <summary>
        /// Finds a <see cref="PartDefinition"/> by ID in the catalog. Returns null if not found.
        /// </summary>
        private PartDefinition FindPartById(string partId)
        {
            if (_shopCatalog == null) return null;
            IReadOnlyList<PartDefinition> parts = _shopCatalog.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].PartId == partId)
                    return parts[i];
            }
            return null;
        }

        /// <summary>
        /// Computes and displays live <see cref="RobotCombatStats"/> based on the current
        /// row selections.  All text writes happen in this cold-path method only.
        /// No-ops if <see cref="_robotDefinition"/> is null or all preview Texts are null.
        /// </summary>
        private void RefreshStatsPreview()
        {
            if (_robotDefinition == null) return;
            if (_healthText == null && _speedText == null &&
                _damageText == null && _armorText == null) return;

            // Collect currently selected PartDefinitions from all rows.
            var selectedParts = new List<PartDefinition>(_rows.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                PartDefinition sel = _rows[i].GetSelectedPartDef();
                if (sel != null)
                    selectedParts.Add(sel);
            }

            // Use upgrade-aware overload when both upgrade fields are assigned;
            // falls back to the base 2-arg overload automatically when either is null.
            RobotCombatStats stats = (_playerPartUpgrades != null && _upgradeConfig != null)
                ? RobotStatsAggregator.Compute(_robotDefinition, selectedParts,
                                               _playerPartUpgrades, _upgradeConfig)
                : RobotStatsAggregator.Compute(_robotDefinition, selectedParts);

            if (_healthText != null)
                _healthText.text = $"HP: {stats.TotalMaxHealth:F0}";
            if (_speedText  != null)
                _speedText.text  = $"Speed: {stats.EffectiveSpeed:F1}";
            if (_damageText != null)
                _damageText.text = $"Dmg×: {stats.EffectiveDamageMultiplier:F2}";
            if (_armorText  != null)
                _armorText.text  = $"Armor: {stats.TotalArmorRating}";
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_playerInventory == null)
                Debug.LogWarning("[LoadoutBuilderController] _playerInventory not assigned.", this);
            if (_playerLoadout == null)
                Debug.LogWarning("[LoadoutBuilderController] _playerLoadout not assigned.", this);
            if (_shopCatalog == null)
                Debug.LogWarning("[LoadoutBuilderController] _shopCatalog not assigned.", this);
            if (_robotDefinition == null)
                Debug.LogWarning("[LoadoutBuilderController] _robotDefinition not assigned.", this);
            if (_slotRowPrefab == null)
                Debug.LogWarning("[LoadoutBuilderController] _slotRowPrefab not assigned.", this);
            if (_slotContainer == null)
                Debug.LogWarning("[LoadoutBuilderController] _slotContainer not assigned.", this);
        }
#endif
    }
}

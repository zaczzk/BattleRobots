using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Reactive pre-match gating controller that runs the full 7-rule
    /// <see cref="LoadoutValidator.Validate"/> (including weapon-type unlock Rule 6)
    /// and gates the "Start Match" button plus an error display panel.
    ///
    /// ── Differences from <see cref="BuildValidationController"/> ──────────────────
    ///   <list type="bullet">
    ///     <item>Supports the optional <see cref="WeaponTypeUnlockConfig"/> /
    ///       <see cref="PrestigeSystemSO"/> / <see cref="WeaponPartCatalogSO"/> triple
    ///       to enforce Rule 6 (weapon-type prestige gating).</item>
    ///     <item>Subscribes to both <c>_onLoadoutChanged</c> <em>and</em>
    ///       <c>_onPrestige</c> so the display refreshes immediately after the
    ///       player earns a new prestige rank that unlocks additional weapon types.</item>
    ///     <item>Supports a scrollable row list (<c>_errorListContainer</c> +
    ///       <c>_errorRowPrefab</c>) in addition to a plain <c>_errorSummaryText</c>
    ///       fallback for scenes that use a simpler layout.</item>
    ///   </list>
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onLoadoutChanged</c> and <c>_onPrestige</c>,
    ///      then calls Refresh().
    ///   2. Refresh() calls <see cref="LoadoutValidator.Validate"/> with all seven
    ///      parameters (nulls disable their respective rules).
    ///   3. <c>_startMatchButton.interactable</c> mirrors <c>result.IsValid</c>.
    ///   4. <c>_validationPanel</c> is shown when the loadout is invalid.
    ///   5. Error rows are destroyed and rebuilt in <c>_errorListContainer</c>.
    ///   6. <c>_errorSummaryText</c> is populated as a newline-joined fallback.
    ///   7. OnDisable unsubscribes from both channels.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • <see cref="DisallowMultipleComponent"/> — one gate controller per panel.
    ///   • All inspector fields are optional; null dependencies are handled safely.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Single delegate cached in Awake; zero heap allocations after init.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   _playerLoadout      → PlayerLoadout SO (equipped part IDs).
    ///   _robotDefinition    → RobotDefinition SO (required slot categories).
    ///   _playerInventory    → PlayerInventory SO (ownership rule; optional).
    ///   _shopCatalog        → ShopCatalog SO (catalog + category rules; optional).
    ///   _unlockConfig       → WeaponTypeUnlockConfig asset (prestige requirements; optional).
    ///   _prestigeSystem     → shared PrestigeSystemSO (current prestige count; optional).
    ///   _weaponCatalog      → WeaponPartCatalogSO asset (weapon type resolution; optional).
    ///   _onLoadoutChanged   → same VoidGameEvent as PlayerLoadout._onLoadoutChanged.
    ///   _onPrestige         → same VoidGameEvent as PrestigeSystemSO._onPrestige (optional).
    ///   _startMatchButton   → Button whose interactable state is gated (optional).
    ///   _validationPanel    → GameObject shown when the loadout is invalid (optional).
    ///   _errorListContainer → ScrollRect Content Transform for per-error rows (optional).
    ///   _errorRowPrefab     → Prefab with ≥ 1 Text child for each error message (optional).
    ///   _errorSummaryText   → Text showing all errors joined by newlines (optional fallback).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreMatchLoadoutGateController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO holding the player's equipped part IDs. " +
                 "Subscribe to _onLoadoutChanged to refresh when the selection changes.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Robot definition that specifies which slot categories must be filled.")]
        [SerializeField] private RobotDefinition _robotDefinition;

        [Tooltip("Optional — when assigned, each equipped part is checked for ownership. " +
                 "Omit to skip the ownership check.")]
        [SerializeField] private PlayerInventory _playerInventory;

        [Tooltip("Optional — when assigned, catalog membership and slot coverage are checked. " +
                 "Omit to skip those rules.")]
        [SerializeField] private ShopCatalog _shopCatalog;

        // ── Inspector — Weapon Unlock (optional) ──────────────────────────────

        [Header("Weapon Unlock (optional)")]
        [Tooltip("Config SO specifying the minimum prestige rank per weapon DamageType. " +
                 "Leave null to skip weapon-type unlock enforcement (Rule 6 disabled).")]
        [SerializeField] private WeaponTypeUnlockConfig _unlockConfig;

        [Tooltip("Shared PrestigeSystemSO providing the player's current prestige count. " +
                 "Leave null to treat prestige count as 0 when Rule 6 runs.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("Catalog mapping part IDs to WeaponPartSO assets for DamageType resolution. " +
                 "Leave null to skip weapon-type unlock enforcement (Rule 6 disabled).")]
        [SerializeField] private WeaponPartCatalogSO _weaponCatalog;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same VoidGameEvent as PlayerLoadout._onLoadoutChanged. " +
                 "Triggers Refresh() on every loadout update.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        [Tooltip("Same VoidGameEvent as PrestigeSystemSO._onPrestige. " +
                 "Triggers Refresh() when the player earns a new prestige rank, " +
                 "which may unlock previously-locked weapon types. Leave null to disable.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI Refs (all optional) ────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Button whose interactable state mirrors loadout validity. " +
                 "When null, only the panel / text are updated.")]
        [SerializeField] private Button _startMatchButton;

        [Tooltip("GameObject shown while the loadout is invalid and hidden when valid. " +
                 "Useful for a warning panel or tooltip root.")]
        [SerializeField] private GameObject _validationPanel;

        [Tooltip("Parent Transform for per-error row instances. " +
                 "Requires _errorRowPrefab. Leave null to skip row generation.")]
        [SerializeField] private Transform _errorListContainer;

        [Tooltip("Row prefab instantiated once per validation error. " +
                 "The first Text component receives the error message text.")]
        [SerializeField] private GameObject _errorRowPrefab;

        [Tooltip("Fallback Text that shows all errors joined by newlines. " +
                 "Used when _errorListContainer / _errorRowPrefab are not assigned.")]
        [SerializeField] private Text _errorSummaryText;

        // ── Cached state ──────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onLoadoutChanged?.RegisterCallback(_refreshDelegate);
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLoadoutChanged?.UnregisterCallback(_refreshDelegate);
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Runs <see cref="LoadoutValidator.Validate"/> (all 7 params) and pushes
        /// the result to the wired button, panel, row list, and summary text.
        /// Safe to call at any time — fully null-safe; skips any unassigned elements.
        /// </summary>
        public void Refresh()
        {
            LoadoutValidationResult result = LoadoutValidator.Validate(
                _playerLoadout,
                _robotDefinition,
                _playerInventory,
                _shopCatalog,
                _unlockConfig,
                _prestigeSystem,
                _weaponCatalog);

            bool valid = result.IsValid;

            // Gate the Start Match button.
            if (_startMatchButton != null)
                _startMatchButton.interactable = valid;

            // Show / hide the error panel.
            if (_validationPanel != null)
                _validationPanel.SetActive(!valid);

            // Rebuild scrollable error row list.
            if (_errorListContainer != null && _errorRowPrefab != null)
            {
                // Destroy stale rows.
                for (int i = _errorListContainer.childCount - 1; i >= 0; i--)
                    Destroy(_errorListContainer.GetChild(i).gameObject);

                // Instantiate one row per error when invalid.
                if (!valid)
                {
                    for (int i = 0; i < result.Errors.Count; i++)
                    {
                        GameObject row   = Instantiate(_errorRowPrefab, _errorListContainer);
                        Text[]     texts = row.GetComponentsInChildren<Text>(true);
                        if (texts.Length > 0)
                            texts[0].text = result.Errors[i];
                    }
                }
            }

            // Populate fallback summary text.
            if (_errorSummaryText != null)
            {
                if (valid)
                {
                    _errorSummaryText.text = string.Empty;
                }
                else
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < result.Errors.Count; i++)
                    {
                        if (i > 0) sb.Append('\n');
                        sb.Append(result.Errors[i]);
                    }
                    _errorSummaryText.text = sb.ToString();
                }
            }
        }

        // ── Read-only properties (for tests) ─────────────────────────────────

        /// <summary>The currently assigned <see cref="PlayerLoadout"/>. May be null.</summary>
        public PlayerLoadout Loadout => _playerLoadout;

        /// <summary>The currently assigned <see cref="WeaponTypeUnlockConfig"/>. May be null.</summary>
        public WeaponTypeUnlockConfig UnlockConfig => _unlockConfig;

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>The currently assigned <see cref="WeaponPartCatalogSO"/>. May be null.</summary>
        public WeaponPartCatalogSO WeaponCatalog => _weaponCatalog;
    }
}

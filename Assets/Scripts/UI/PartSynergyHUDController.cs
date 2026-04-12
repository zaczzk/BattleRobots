using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's currently active build synergies in the pre-match
    /// or in-arena HUD, refreshing automatically whenever the loadout changes.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onLoadoutChanged</c> and calls Refresh().
    ///   2. Refresh() calls <see cref="PartSynergyConfig.GetActiveSynergies"/> with
    ///      the player's equipped part IDs and the shop catalog.
    ///   3. <c>_synergyPanel</c> is shown when at least one synergy is active,
    ///      hidden when none are active.
    ///   4. One row prefab is instantiated per active synergy:
    ///        Texts[0] — displayName      (e.g. "Blade Master")
    ///        Texts[1] — bonusDescription (e.g. "+10% Damage")
    ///   5. OnDisable unsubscribes so no callbacks leak when the panel is hidden.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All inspector fields are optional; null dependencies are handled safely.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Refresh delegate cached in Awake; zero heap alloc after Awake.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   • _synergyConfig    → PartSynergyConfig SO asset
    ///   • _catalog          → ShopCatalog SO asset (same as LoadoutBuilderController)
    ///   • _playerLoadout    → PlayerLoadout SO asset
    ///   • _onLoadoutChanged → same VoidGameEvent as PlayerLoadout._onLoadoutChanged
    ///   • _synergyPanel     → root GameObject toggled by synergy activity (optional)
    ///   • _listContainer    → Transform parent for instantiated row prefabs (optional)
    ///   • _rowPrefab        → prefab: Texts[0]=displayName, Texts[1]=bonusDescription (optional)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartSynergyHUDController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog of synergy definitions to evaluate against the player's loadout.")]
        [SerializeField] private PartSynergyConfig _synergyConfig;

        [Tooltip("Shop catalog used to resolve equipped part IDs to PartDefinitions.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("Runtime loadout SO whose EquippedPartIds are evaluated on each refresh.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same VoidGameEvent as PlayerLoadout._onLoadoutChanged. "
               + "Triggers Refresh() to keep the synergy display current.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Inspector — UI Refs (all optional) ────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Root panel GameObject. SetActive(true) when any synergy is active; "
               + "SetActive(false) when none are.")]
        [SerializeField] private GameObject _synergyPanel;

        [Tooltip("Parent Transform under which synergy row prefabs are instantiated.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab for one synergy row. Expected child Text layout:\n"
               + "  Texts[0] — synergy displayName\n"
               + "  Texts[1] — bonusDescription")]
        [SerializeField] private GameObject _rowPrefab;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onLoadoutChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLoadoutChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            // Resolve the current equipped IDs (empty list when loadout is null).
            IReadOnlyList<string> equipped =
                _playerLoadout != null
                    ? _playerLoadout.EquippedPartIds
                    : Array.Empty<string>();

            // Evaluate active synergies (Array.Empty when config is null).
            IReadOnlyList<PartSynergyEntry> active =
                _synergyConfig != null
                    ? _synergyConfig.GetActiveSynergies(equipped, _catalog)
                    : Array.Empty<PartSynergyEntry>();

            bool anySynergy = active.Count > 0;

            // Show / hide the synergy panel.
            if (_synergyPanel != null)
                _synergyPanel.SetActive(anySynergy);

            // Without a container or prefab there are no rows to build.
            if (_listContainer == null || _rowPrefab == null) return;

            // Destroy existing row children before rebuilding.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            // Instantiate one row per active synergy.
            for (int i = 0; i < active.Count; i++)
            {
                PartSynergyEntry entry = active[i];
                GameObject row = Instantiate(_rowPrefab, _listContainer);

                Text[] texts = row.GetComponentsInChildren<Text>();
                if (texts.Length >= 1) texts[0].text = entry.displayName;
                if (texts.Length >= 2) texts[1].text = entry.bonusDescription;
            }
        }
    }
}

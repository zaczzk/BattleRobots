using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match advisor panel that shows which build synergies are currently
    /// active and which are inactive (missing requirements) for the equipped loadout.
    ///
    /// Unlike <see cref="PartSynergyHUDController"/> (which only lists active synergies),
    /// this controller surfaces both active and inactive synergies so the player can
    /// make informed loadout decisions before entering a match.
    ///
    /// ── Row format ────────────────────────────────────────────────────────────────
    ///   Active rows   → Texts[0] = synergy displayName; Texts[1] = bonusDescription.
    ///   Inactive rows → Texts[0] = synergy displayName; Texts[1] = "(inactive)".
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onLoadoutChanged → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → evaluates synergies; partitions active / inactive; rebuilds rows.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one advisor panel per canvas.
    ///   • All UI and data fields are optional.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _synergyConfig          → shared PartSynergyConfig SO.
    ///   _catalog                → shared ShopCatalog SO (required by PartSynergyConfig).
    ///   _playerLoadout          → shared PlayerLoadout SO.
    ///   _onLoadoutChanged       → VoidGameEvent raised when the loadout changes.
    ///   _activeContainer        → Transform parent for active-synergy rows.
    ///   _inactiveContainer      → Transform parent for inactive-synergy rows.
    ///   _activeRowPrefab        → Prefab for active synergy rows.
    ///   _inactiveRowPrefab      → Prefab for inactive synergy rows.
    ///   _activeSynergyCountLbl  → Text showing "Active: N".
    ///   _totalSynergyCountLbl   → Text showing "Total: N".
    ///   _noActiveSynergiesLabel → GameObject shown when no synergies are active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartSynergyAdvisorController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Synergy catalog SO that defines all possible synergy bonuses.")]
        [SerializeField] private PartSynergyConfig _synergyConfig;

        [Tooltip("Shop catalog used to resolve equipped part IDs to PartDefinitions.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("Runtime SO holding the currently equipped part IDs.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised when the player's loadout changes. Triggers a Refresh.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI — Active Synergies (optional)")]
        [Tooltip("Parent transform for active-synergy row instances.")]
        [SerializeField] private Transform _activeContainer;

        [Tooltip("Prefab for active synergy rows. Needs ≥2 Text children.")]
        [SerializeField] private GameObject _activeRowPrefab;

        [Header("UI — Inactive Synergies (optional)")]
        [Tooltip("Parent transform for inactive-synergy row instances.")]
        [SerializeField] private Transform _inactiveContainer;

        [Tooltip("Prefab for inactive synergy rows. Needs ≥2 Text children.")]
        [SerializeField] private GameObject _inactiveRowPrefab;

        [Header("UI — Labels (optional)")]
        [Tooltip("Label showing 'Active: N'.")]
        [SerializeField] private Text _activeSynergyCountLabel;

        [Tooltip("Label showing 'Total: N'.")]
        [SerializeField] private Text _totalSynergyCountLabel;

        [Tooltip("Shown when no synergies are currently active.")]
        [SerializeField] private GameObject _noActiveSynergiesLabel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

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

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates all synergies, partitions them into active / inactive, rebuilds
        /// both row lists, and updates count labels.  Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            // Evaluate active synergies.
            IReadOnlyList<PartSynergyEntry> activeSynergies;
            if (_synergyConfig != null && _catalog != null && _playerLoadout != null)
            {
                activeSynergies = _synergyConfig.GetActiveSynergies(
                    _playerLoadout.EquippedPartIds, _catalog);
            }
            else
            {
                activeSynergies = System.Array.Empty<PartSynergyEntry>();
            }

            int activeCount = activeSynergies.Count;
            int totalCount  = (_synergyConfig != null && _synergyConfig.Entries != null)
                              ? _synergyConfig.Entries.Count
                              : 0;

            // Update count labels.
            if (_activeSynergyCountLabel != null)
                _activeSynergyCountLabel.text = string.Format("Active: {0}", activeCount);

            if (_totalSynergyCountLabel != null)
                _totalSynergyCountLabel.text = string.Format("Total: {0}", totalCount);

            _noActiveSynergiesLabel?.SetActive(activeCount == 0);

            // Rebuild active rows.
            if (_activeContainer != null)
            {
                for (int i = _activeContainer.childCount - 1; i >= 0; i--)
                    Destroy(_activeContainer.GetChild(i).gameObject);

                if (_activeRowPrefab != null)
                {
                    for (int i = 0; i < activeSynergies.Count; i++)
                    {
                        var entry = activeSynergies[i];
                        var row   = Instantiate(_activeRowPrefab, _activeContainer);
                        var texts = row.GetComponentsInChildren<Text>();
                        if (texts.Length > 0) texts[0].text = entry.displayName    ?? string.Empty;
                        if (texts.Length > 1) texts[1].text = entry.bonusDescription ?? string.Empty;
                    }
                }
            }

            // Rebuild inactive rows.
            if (_inactiveContainer != null && _synergyConfig != null)
            {
                for (int i = _inactiveContainer.childCount - 1; i >= 0; i--)
                    Destroy(_inactiveContainer.GetChild(i).gameObject);

                if (_inactiveRowPrefab != null && _synergyConfig.Entries != null)
                {
                    foreach (var entry in _synergyConfig.Entries)
                    {
                        // Skip if this entry is in the active list.
                        bool isActive = false;
                        for (int a = 0; a < activeSynergies.Count; a++)
                        {
                            if (ReferenceEquals(activeSynergies[a], entry))
                            {
                                isActive = true;
                                break;
                            }
                        }
                        if (isActive) continue;

                        var row   = Instantiate(_inactiveRowPrefab, _inactiveContainer);
                        var texts = row.GetComponentsInChildren<Text>();
                        if (texts.Length > 0) texts[0].text = entry.displayName ?? string.Empty;
                        if (texts.Length > 1) texts[1].text = "(inactive)";
                    }
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PartSynergyConfig"/>. May be null.</summary>
        public PartSynergyConfig SynergyConfig => _synergyConfig;

        /// <summary>The assigned <see cref="ShopCatalog"/>. May be null.</summary>
        public ShopCatalog Catalog => _catalog;

        /// <summary>The assigned <see cref="PlayerLoadout"/>. May be null.</summary>
        public PlayerLoadout PlayerLoadout => _playerLoadout;
    }
}

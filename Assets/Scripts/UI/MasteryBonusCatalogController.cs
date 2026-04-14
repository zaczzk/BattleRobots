using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI controller that lists all <see cref="MasteryBonusEntry"/> items from a
    /// <see cref="MasteryBonusCatalogSO"/> and highlights which are currently active
    /// (i.e. their required <see cref="DamageType"/> is mastered in
    /// <see cref="DamageTypeMasterySO"/>).
    ///
    /// ── Row layout convention ─────────────────────────────────────────────────
    ///   Each instantiated row prefab must have at least two
    ///   <see cref="UnityEngine.UI.Text"/> components (found via
    ///   <c>GetComponentsInChildren&lt;Text&gt;(true)</c>):
    ///     [0] Label       — entry.label
    ///     [1] Status      — "Active" or "Locked"
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one catalog panel per canvas.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - All inspector fields are optional; any unassigned field is silently skipped.
    ///
    /// Scene wiring:
    ///   _catalog              → MasteryBonusCatalogSO.
    ///   _mastery              → DamageTypeMasterySO.
    ///   _onMasteryUnlocked    → VoidGameEvent fired by DamageTypeMasterySO.
    ///   _listContainer        → Transform parent for row prefab instances.
    ///   _rowPrefab            → Prefab with ≥2 child Text components.
    ///   _totalMultiplierLabel → Text showing combined active multiplier (e.g. "x1.50").
    ///   _panel                → Root panel — hidden when catalog is null.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MasteryBonusCatalogController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog of mastery-gated bonuses. Leave null to hide the panel.")]
        [SerializeField] private MasteryBonusCatalogSO _catalog;

        [Tooltip("Runtime mastery SO. Provides mastered-type flags.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised by DamageTypeMasterySO when any type first reaches mastery. " +
                 "Triggers Refresh(). Leave null to disable auto-refresh.")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        // ── Inspector — UI Refs (optional) ────────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Parent transform under which row prefabs are instantiated.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab with at least 2 child Text components: [0] Label, [1] Status.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Text label displaying the combined active multiplier (e.g. 'x1.50').")]
        [SerializeField] private Text _totalMultiplierLabel;

        [Tooltip("Root panel. Shown when catalog is assigned; hidden otherwise.")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMasteryUnlocked?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMasteryUnlocked?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the bonus row list and total multiplier label from
        /// <see cref="_catalog"/> and <see cref="_mastery"/>.
        /// Hides the panel when <see cref="_catalog"/> is null.
        /// Fully null-safe on all optional refs.
        /// </summary>
        public void Refresh()
        {
            if (_catalog == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            // Rebuild rows.
            if (_listContainer != null)
            {
                for (int i = _listContainer.childCount - 1; i >= 0; i--)
                    Destroy(_listContainer.GetChild(i).gameObject);

                if (_rowPrefab != null)
                {
                    for (int i = 0; i < _catalog.Count; i++)
                    {
                        if (!_catalog.TryGetEntry(i, out MasteryBonusEntry entry)) continue;

                        GameObject row = Instantiate(_rowPrefab, _listContainer);
                        Text[] texts   = row.GetComponentsInChildren<Text>(true);

                        if (texts.Length > 0) texts[0].text = entry.label;
                        if (texts.Length > 1)
                            texts[1].text = _catalog.IsActive(entry, _mastery) ? "Active" : "Locked";
                    }
                }
            }

            // Update total multiplier label.
            if (_totalMultiplierLabel != null)
            {
                float total = _catalog.GetTotalMultiplier(_mastery);
                _totalMultiplierLabel.text = $"x{total:F2}";
            }
        }

        /// <summary>The assigned <see cref="MasteryBonusCatalogSO"/>. May be null.</summary>
        public MasteryBonusCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;
    }
}

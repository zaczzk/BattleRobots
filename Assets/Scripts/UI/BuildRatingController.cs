using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's current robot "build power" rating in a Text label
    /// and optional Slider bar, refreshing automatically whenever the loadout changes.
    ///
    /// ── Flow ──────────────────────────────────────────────────────────────────
    ///   1. OnEnable subscribes to <c>_onLoadoutChanged</c> and calls Refresh().
    ///   2. Refresh() calls <see cref="BuildRatingSO.UpdateRating"/> which
    ///      delegates to <see cref="RobotBuildRatingCalculator.Calculate"/>.
    ///   3. <c>_ratingText</c> is set to "Power: N".
    ///   4. <c>_ratingBar</c>.value is set to Clamp01(rating / MaxRatingDisplay).
    ///   5. OnDisable unsubscribes so no callbacks leak when the panel is hidden.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • All inspector fields are optional; null dependencies are handled safely.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Refresh delegate cached in Awake; zero heap alloc after Awake.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   • _buildRating       → BuildRatingSO asset
    ///   • _loadout           → PlayerLoadout SO asset
    ///   • _catalog           → ShopCatalog SO asset (same as LoadoutBuilderController)
    ///   • _upgradeRegistry   → PlayerPartUpgrades SO asset (optional)
    ///   • _synergyConfig     → PartSynergyConfig SO asset (optional)
    ///   • _ratingConfig      → RobotBuildRatingConfig SO asset
    ///   • _onLoadoutChanged  → same VoidGameEvent as PlayerLoadout._onLoadoutChanged
    ///   • _ratingText        → Text label showing "Power: N" (optional)
    ///   • _ratingBar         → Slider whose value maps to [0, MaxRatingDisplay] (optional)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildRatingController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>
        /// Rating value that maps to a full <c>_ratingBar</c> (Slider value = 1).
        /// Ratings above this are clamped to 1.
        /// </summary>
        private const int MaxRatingDisplay = 1000;

        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO that stores and exposes the current build power rating.")]
        [SerializeField] private BuildRatingSO _buildRating;

        [Tooltip("Runtime loadout SO whose EquippedPartIds are evaluated on each refresh.")]
        [SerializeField] private PlayerLoadout _loadout;

        [Tooltip("Shop catalog used to resolve equipped part IDs to PartDefinitions.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("Per-part upgrade tiers. Optional — leave null to exclude upgrade contribution.")]
        [SerializeField] private PlayerPartUpgrades _upgradeRegistry;

        [Tooltip("Synergy catalog. Optional — leave null to exclude synergy contribution.")]
        [SerializeField] private PartSynergyConfig _synergyConfig;

        [Tooltip("Weight coefficients SO. Required for a non-zero rating.")]
        [SerializeField] private RobotBuildRatingConfig _ratingConfig;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Same VoidGameEvent as PlayerLoadout._onLoadoutChanged. "
               + "Triggers Refresh() to keep the build rating current.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Inspector — UI Refs (all optional) ────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Text label updated to 'Power: N' on each refresh.")]
        [SerializeField] private Text _ratingText;

        [Tooltip("Slider whose value is set to Clamp01(rating / 1000) on each refresh.")]
        [SerializeField] private Slider _ratingBar;

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
            // Ask the SO to recalculate; it stores the result and fires its own event.
            _buildRating?.UpdateRating(
                _loadout, _catalog, _upgradeRegistry, _synergyConfig, _ratingConfig);

            int rating = _buildRating != null ? _buildRating.CurrentRating : 0;

            if (_ratingText != null)
                _ratingText.text = "Power: " + rating;

            if (_ratingBar != null)
                _ratingBar.value = Mathf.Clamp01((float)rating / MaxRatingDisplay);
        }
    }
}

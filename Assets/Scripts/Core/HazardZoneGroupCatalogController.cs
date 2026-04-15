using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Routes activate / deactivate / toggle operations to individual
    /// <see cref="HazardZoneGroupSO"/> entries within a
    /// <see cref="HazardZoneGroupCatalogSO"/>, addressed by index.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • <see cref="ActivateGroup(int)"/>   calls <see cref="HazardZoneGroupSO.Activate"/>
    ///     on the group at the given index.
    ///   • <see cref="DeactivateGroup(int)"/> calls <see cref="HazardZoneGroupSO.Deactivate"/>.
    ///   • <see cref="ToggleGroup(int)"/>     calls <see cref="HazardZoneGroupSO.Toggle"/>.
    ///   • All three methods are null-safe: no-op when <see cref="_catalog"/> is null,
    ///     the index is out of range, or the catalog entry itself is null.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics or UI references.
    ///   - Suitable for use by event-listener components, UI buttons, or timeline events.
    ///   - DisallowMultipleComponent — one catalog controller per GameObject.
    ///   - No Update / FixedUpdate — purely command-driven via public methods.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_catalog</c> → a HazardZoneGroupCatalogSO asset.
    ///   2. Call ActivateGroup / DeactivateGroup / ToggleGroup from VoidGameEvent
    ///      listeners, UI buttons, or other controllers using UnityEvents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZoneGroupCatalogController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Catalog of HazardZoneGroupSO assets, indexed for routing.")]
        [SerializeField] private HazardZoneGroupCatalogSO _catalog;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Calls <see cref="HazardZoneGroupSO.Activate"/> on the group at
        /// <paramref name="index"/>.
        /// Null-safe: no-op when catalog is null, index is out of range, or
        /// the catalog entry is null.
        /// </summary>
        public void ActivateGroup(int index) => _catalog?.GetGroup(index)?.Activate();

        /// <summary>
        /// Calls <see cref="HazardZoneGroupSO.Deactivate"/> on the group at
        /// <paramref name="index"/>.
        /// Null-safe: no-op when catalog is null, index is out of range, or
        /// the catalog entry is null.
        /// </summary>
        public void DeactivateGroup(int index) => _catalog?.GetGroup(index)?.Deactivate();

        /// <summary>
        /// Calls <see cref="HazardZoneGroupSO.Toggle"/> on the group at
        /// <paramref name="index"/>.
        /// Null-safe: no-op when catalog is null, index is out of range, or
        /// the catalog entry is null.
        /// </summary>
        public void ToggleGroup(int index) => _catalog?.GetGroup(index)?.Toggle();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="HazardZoneGroupCatalogSO"/>. May be null.</summary>
        public HazardZoneGroupCatalogSO Catalog => _catalog;

        /// <summary>
        /// Total number of entries in the catalog; 0 when <see cref="_catalog"/> is null.
        /// </summary>
        public int EntryCount => _catalog?.EntryCount ?? 0;
    }
}

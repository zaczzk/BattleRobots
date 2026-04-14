using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data catalog SO that pairs per-hazard human-readable identifiers with
    /// activation delays. Consumed by
    /// <see cref="BattleRobots.Physics.ArenaHazardActivationController"/>, which
    /// drives the actual <see cref="BattleRobots.Physics.HazardZoneController"/>
    /// references in the scene.
    ///
    /// ── Index alignment ───────────────────────────────────────────────────────
    ///   Entry index 0…(EntryCount-1) maps 1-to-1 with the parallel
    ///   <c>HazardZoneController[]</c> array on the activation controller.
    ///   If the array is shorter than <c>EntryCount</c> the extra catalog entries
    ///   are ignored; if longer, missing entries default to delay = 0.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI references.
    ///   - SO assets are immutable at runtime — all fields are read-only at play time.
    ///   - <see cref="RaiseAllActive"/> is called by the activation controller once
    ///     every managed hazard is live; wiring the catalog's own event channel is
    ///     optional (the controller has its own <c>_onAllHazardsActive</c> as well).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaHazardCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaHazardCatalog", order = 11)]
    public sealed class ArenaHazardCatalogSO : ScriptableObject
    {
        // ── Nested types ──────────────────────────────────────────────────────

        /// <summary>
        /// Per-hazard entry pairing an editor-facing identifier with an activation delay.
        /// </summary>
        [Serializable]
        public struct HazardCatalogEntry
        {
            [Tooltip("Human-readable identifier for this hazard slot (editor only).")]
            public string hazardId;

            [Tooltip("Seconds after match start before this hazard activates. " +
                     "0 = activates immediately at match start.")]
            [Min(0f)]
            public float activationDelay;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Hazard Entries")]
        [Tooltip("One entry per hazard zone. Index must align with the " +
                 "HazardZoneController[] array on ArenaHazardActivationController.")]
        [SerializeField] private HazardCatalogEntry[] _entries = Array.Empty<HazardCatalogEntry>();

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Raised by ArenaHazardActivationController.RaiseAllActive() " +
                 "when the last managed hazard becomes active.")]
        [SerializeField] private VoidGameEvent _onAllHazardsActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Number of hazard entries in the catalog.</summary>
        public int EntryCount => _entries != null ? _entries.Length : 0;

        /// <summary>
        /// Returns the activation delay in seconds for the entry at <paramref name="index"/>.
        /// Returns 0 when <paramref name="index"/> is out of range or entries is null.
        /// </summary>
        public float GetActivationDelay(int index)
        {
            if (_entries == null || index < 0 || index >= _entries.Length)
                return 0f;
            return _entries[index].activationDelay;
        }

        /// <summary>
        /// Returns the human-readable hazard identifier for the entry at
        /// <paramref name="index"/>.
        /// Returns <see cref="string.Empty"/> when out of range or entries is null.
        /// </summary>
        public string GetHazardId(int index)
        {
            if (_entries == null || index < 0 || index >= _entries.Length)
                return string.Empty;
            return _entries[index].hazardId;
        }

        /// <summary>
        /// Raises <c>_onAllHazardsActive</c>. Called by
        /// <see cref="BattleRobots.Physics.ArenaHazardActivationController"/>
        /// when every managed hazard zone has been activated.
        /// Null-safe — no-op when the event channel is unassigned.
        /// </summary>
        public void RaiseAllActive()
        {
            _onAllHazardsActive?.Raise();
        }
    }
}

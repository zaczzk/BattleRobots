using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable catalog that maps inventory <see cref="PartDefinition.PartId"/> strings
    /// to their corresponding <see cref="WeaponPartSO"/> assets.
    ///
    /// ── Purpose ──────────────────────────────────────────────────────────────────
    ///   Bridges the inventory system (which stores string PartIds) to the combat
    ///   type system (which needs a <see cref="WeaponPartSO"/> to determine the
    ///   outgoing <see cref="DamageType"/> and base damage on each shot).
    ///   Used by <see cref="BattleRobots.Physics.WeaponLoadoutApplicator"/> to resolve
    ///   the player's equipped weapon at match start, and by UI advisor panels for
    ///   pre-match type-matchup hints.
    ///
    /// ── Lookup ───────────────────────────────────────────────────────────────────
    ///   <see cref="Lookup"/> performs a linear scan and returns the first entry whose
    ///   linked <see cref="PartDefinition.PartId"/> equals the query string.
    ///   Returns null when no match is found, the catalog is empty, or the query
    ///   is null / whitespace.  Null catalog entries are silently skipped.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - <see cref="Parts"/> is read-only at runtime; asset is immutable.
    ///   - Zero allocation on the hot path: linear reference scan.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ WeaponPartCatalog.
    /// Assign to <see cref="BattleRobots.Physics.WeaponLoadoutApplicator"/> in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/WeaponPartCatalog")]
    public sealed class WeaponPartCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Weapon Entries")]
        [Tooltip("List of WeaponPartSO assets registered in this catalog. " +
                 "Each entry must have a PartDefinition link for Lookup(partId) to work. " +
                 "Null entries are silently skipped at runtime.")]
        [SerializeField] private List<WeaponPartSO> _parts = new List<WeaponPartSO>();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// All registered weapon parts in inspector order.
        /// Null slots may appear if the Inspector list has empty entries — they
        /// are skipped by <see cref="Lookup"/> at runtime.
        /// </summary>
        public IReadOnlyList<WeaponPartSO> Parts => _parts;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the first <see cref="WeaponPartSO"/> in the catalog whose linked
        /// <see cref="PartDefinition.PartId"/> equals <paramref name="partId"/>.
        ///
        /// Returns null when:
        ///   • <paramref name="partId"/> is null or whitespace.
        ///   • No entry has a matching PartDefinition.PartId.
        ///   • The catalog is empty.
        ///
        /// Null catalog entries and entries without a linked PartDefinition are
        /// skipped silently — no exceptions are thrown.
        /// Zero allocation — linear scan of reference list.
        /// </summary>
        public WeaponPartSO Lookup(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return null;

            for (int i = 0; i < _parts.Count; i++)
            {
                WeaponPartSO entry = _parts[i];
                if (entry == null) continue;
                if (entry.PartDefinition != null &&
                    entry.PartDefinition.PartId == partId)
                    return entry;
            }

            return null;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] == null)
                    Debug.LogWarning(
                        $"[WeaponPartCatalogSO] '{name}': entry [{i}] is null — " +
                        "assign a WeaponPartSO asset or remove the empty slot.");
            }
        }
#endif
    }
}

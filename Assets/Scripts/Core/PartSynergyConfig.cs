using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single condition that must be satisfied for a <see cref="PartSynergyEntry"/>
    /// to become active.  The player must have at least <see cref="requiredCount"/>
    /// equipped parts whose <see cref="PartDefinition.Category"/> equals
    /// <see cref="requiredCategory"/> and whose <see cref="PartDefinition.Rarity"/>
    /// is greater than or equal to <see cref="minimumRarity"/>.
    /// </summary>
    [Serializable]
    public struct PartSynergyRequirement
    {
        [Tooltip("The part category that equipped parts must match.")]
        public PartCategory requiredCategory;

        [Tooltip("Minimum rarity tier (inclusive). Parts at this rarity or higher qualify.")]
        public PartRarity minimumRarity;

        [Tooltip("Minimum number of qualifying parts needed to satisfy this requirement.")]
        [Min(1)] public int requiredCount;
    }

    /// <summary>
    /// One configurable synergy definition.  All requirements use AND logic:
    /// every <see cref="requirements"/> entry must be satisfied for the synergy
    /// to be considered active.
    ///
    /// Stored as a serializable class inside <see cref="PartSynergyConfig._entries"/>
    /// so designers can configure synergies entirely from the Inspector.
    /// </summary>
    [Serializable]
    public sealed class PartSynergyEntry
    {
        [Tooltip("Short player-facing name shown in the HUD, e.g. 'Blade Master'.")]
        public string displayName = "";

        [Tooltip("Description of the bonus granted when active, e.g. '+10% Damage'.")]
        [TextArea(1, 3)]
        public string bonusDescription = "";

        [Tooltip("All conditions that must be met simultaneously for this synergy to activate.")]
        public List<PartSynergyRequirement> requirements = new List<PartSynergyRequirement>();
    }

    /// <summary>
    /// Immutable ScriptableObject cataloguing all configurable build synergies.
    ///
    /// A synergy activates when every one of its <see cref="PartSynergyRequirement"/>
    /// entries is satisfied by the player's currently equipped parts.  Multiple
    /// synergies may be active simultaneously.
    ///
    /// ── Design intent ────────────────────────────────────────────────────────
    ///   Synergies reward players for building around rarity and category combos,
    ///   deepening the meta-loop (M7) by giving concrete goals beyond "buy the
    ///   most expensive part".  Example synergies:
    ///     • "Blade Master"   — 2× Weapon parts at Rare+ → "+10% Damage"
    ///     • "Iron Fortress"  — 1× Chassis + 1× Armor at Epic+ → "+20 Armor Rating"
    ///     • "Speed Demon"    — 2× Leg/Wheel parts at Uncommon+ → "+15% Speed"
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   Call <see cref="GetActiveSynergies"/> from any UI controller (e.g.
    ///   <see cref="BattleRobots.UI.PartSynergyHUDController"/>) that needs to
    ///   display the player's currently active build bonuses.
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace.  No Physics or UI references.
    ///   Never mutated at runtime — all runtime state is in the PlayerLoadout SO.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Core ▶ PartSynergyConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/PartSynergyConfig", order = 0)]
    public sealed class PartSynergyConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Synergy Definitions")]
        [Tooltip("All possible synergies.  Each entry's requirements are evaluated against "
               + "the player's equipped parts whenever the loadout changes.")]
        [SerializeField] private List<PartSynergyEntry> _entries = new List<PartSynergyEntry>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Read-only view of all configured synergy entries.</summary>
        public IReadOnlyList<PartSynergyEntry> Entries => _entries;

        /// <summary>
        /// Returns every configured synergy that is currently active for the
        /// supplied set of equipped part IDs and shop catalog.
        ///
        /// A synergy is active when every one of its <see cref="PartSynergyRequirement"/>
        /// entries is satisfied: the player must have at least
        /// <c>requiredCount</c> equipped parts whose category matches
        /// <c>requiredCategory</c> and whose rarity is ≥ <c>minimumRarity</c>.
        ///
        /// Returns <see cref="Array.Empty{T}"/> when:
        ///   • <paramref name="catalog"/> is null,
        ///   • <paramref name="equippedPartIds"/> is null or empty,
        ///   • no synergies are configured, or
        ///   • no configured synergy has all of its requirements satisfied.
        ///
        /// Alloc note: one <c>Dictionary</c>, one intermediate <c>List</c>, and
        /// one result <c>List</c> are allocated per call.  This is acceptable for
        /// UI-driven, non-Update paths.
        /// </summary>
        /// <param name="equippedPartIds">
        ///   The player's currently equipped part IDs (from
        ///   <see cref="PlayerLoadout.EquippedPartIds"/>).
        /// </param>
        /// <param name="catalog">
        ///   The shop catalog used to resolve part IDs to
        ///   <see cref="PartDefinition"/> instances.
        /// </param>
        public IReadOnlyList<PartSynergyEntry> GetActiveSynergies(
            IReadOnlyList<string> equippedPartIds,
            ShopCatalog           catalog)
        {
            if (catalog == null
                || equippedPartIds == null
                || equippedPartIds.Count == 0)
                return Array.Empty<PartSynergyEntry>();

            if (_entries == null || _entries.Count == 0)
                return Array.Empty<PartSynergyEntry>();

            // Build partId → PartDefinition lookup from the catalog (O(n) cold path).
            var catalogLookup = new Dictionary<string, PartDefinition>(
                catalog.Parts.Count, StringComparer.Ordinal);
            foreach (PartDefinition part in catalog.Parts)
            {
                if (part != null && !string.IsNullOrWhiteSpace(part.PartId))
                    catalogLookup[part.PartId] = part;
            }

            // Resolve each equipped ID to a PartDefinition; skip unknowns.
            var equippedParts = new List<PartDefinition>(equippedPartIds.Count);
            foreach (string id in equippedPartIds)
            {
                if (!string.IsNullOrWhiteSpace(id)
                    && catalogLookup.TryGetValue(id, out PartDefinition def))
                    equippedParts.Add(def);
            }

            if (equippedParts.Count == 0)
                return Array.Empty<PartSynergyEntry>();

            // Evaluate each configured synergy.
            List<PartSynergyEntry> result = null;

            foreach (PartSynergyEntry entry in _entries)
            {
                if (entry == null) continue;
                if (entry.requirements == null || entry.requirements.Count == 0) continue;

                bool allMet = true;
                foreach (PartSynergyRequirement req in entry.requirements)
                {
                    // Count how many equipped parts satisfy this single requirement.
                    int count = 0;
                    foreach (PartDefinition part in equippedParts)
                    {
                        if (part.Category == req.requiredCategory
                            && part.Rarity >= req.minimumRarity)
                            count++;
                    }

                    if (count < req.requiredCount)
                    {
                        allMet = false;
                        break;
                    }
                }

                if (allMet)
                {
                    if (result == null) result = new List<PartSynergyEntry>();
                    result.Add(entry);
                }
            }

            return result ?? (IReadOnlyList<PartSynergyEntry>)Array.Empty<PartSynergyEntry>();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_entries == null) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                PartSynergyEntry e = _entries[i];
                if (e == null) continue;
                if (string.IsNullOrWhiteSpace(e.displayName))
                    Debug.LogWarning(
                        $"[PartSynergyConfig] '{name}': entry [{i}] has an empty displayName.");
                if (e.requirements == null || e.requirements.Count == 0)
                    Debug.LogWarning(
                        $"[PartSynergyConfig] '{name}': entry [{i}] ('{e.displayName}') "
                      + "has no requirements — it will always be active, which is likely unintended.");
            }
        }
#endif
    }
}

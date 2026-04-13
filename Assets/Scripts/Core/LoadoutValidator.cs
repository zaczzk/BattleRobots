using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Result of a <see cref="LoadoutValidator.Validate"/> call.
    ///
    /// <para>
    /// <see cref="IsValid"/> is <c>true</c> when no rule violations were found.
    /// <see cref="Errors"/> is always non-null; it is empty on success and contains
    /// one human-readable string per violation on failure.
    /// </para>
    ///
    /// Use the static factory helpers <see cref="Valid"/> and
    /// <see cref="Invalid(List{string})"/> rather than constructing directly.
    /// </summary>
    public readonly struct LoadoutValidationResult
    {
        /// <summary>True when the loadout passes all checks.</summary>
        public bool IsValid { get; }

        /// <summary>
        /// Human-readable list of violation messages.
        /// Empty when <see cref="IsValid"/> is <c>true</c>.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        private LoadoutValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors  = errors;
        }

        // ── Factories ─────────────────────────────────────────────────────────

        private static readonly IReadOnlyList<string> s_emptyErrors
            = new List<string>(0);

        /// <summary>
        /// Returns a valid result with an empty <see cref="Errors"/> list.
        /// </summary>
        public static LoadoutValidationResult Valid
            => new LoadoutValidationResult(true, s_emptyErrors);

        /// <summary>
        /// Returns an invalid result carrying the supplied <paramref name="errors"/> list.
        /// </summary>
        public static LoadoutValidationResult Invalid(List<string> errors)
            => new LoadoutValidationResult(false, errors ?? new List<string>(0));
    }

    /// <summary>
    /// Stateless validator that checks a <see cref="PlayerLoadout"/> (or a raw
    /// equipped-ID list) against a <see cref="RobotDefinition"/>,
    /// <see cref="PlayerInventory"/>, and <see cref="ShopCatalog"/>.
    ///
    /// ── Rules checked ────────────────────────────────────────────────────────
    ///   1. <b>Null guards</b> — equippedIds and robotDef must be non-null.
    ///   2. <b>Catalog membership</b> — every equipped part ID must exist in the
    ///      catalog (only checked when <paramref name="catalog"/> is non-null).
    ///   3. <b>Ownership</b> — every equipped part ID must be owned by the player
    ///      (only checked when <paramref name="inventory"/> is non-null).
    ///   4. <b>Slot coverage</b> — every unique non-Weapon <see cref="PartCategory"/>
    ///      required by <see cref="RobotDefinition.Slots"/> must have at least one
    ///      equipped part in that category (only checked when <paramref name="catalog"/>
    ///      is non-null, since category lookup requires the catalog).
    ///   5. <b>Exactly-one-Weapon</b> — when the robot definition requires a Weapon
    ///      slot and the catalog is provided, exactly one Weapon-category part must be
    ///      equipped.  0 or 2+ weapon parts each produce a distinct error message.
    ///
    /// ── Partial validation ────────────────────────────────────────────────────
    ///   Passing <c>null</c> for <paramref name="catalog"/> disables rules 2 and 4.
    ///   Passing <c>null</c> for <paramref name="inventory"/> disables rule 3.
    ///   This lets callers run the subset of checks their context supports.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace — no Physics / UI references.
    ///   All methods are static and allocation-free on the happy path once the
    ///   catalog lookup is built.
    /// </summary>
    public static class LoadoutValidator
    {
        // ── Primary validation overload ───────────────────────────────────────

        /// <summary>
        /// Validates a raw list of equipped part IDs.
        ///
        /// <para>Rules are applied in the order listed in the class summary.
        /// All violations are collected before returning so the caller can
        /// display the full list of problems at once.</para>
        /// </summary>
        /// <param name="equippedIds">
        /// List of part IDs currently in the player's loadout.
        /// Passing <c>null</c> immediately returns an invalid result.
        /// </param>
        /// <param name="robotDef">
        /// Defines the required slot categories.
        /// Passing <c>null</c> immediately returns an invalid result.
        /// </param>
        /// <param name="inventory">
        /// Optional — when non-null, ownership of each equipped part is verified.
        /// </param>
        /// <param name="catalog">
        /// Optional — when non-null, catalog membership and slot coverage are verified.
        /// </param>
        public static LoadoutValidationResult Validate(
            IReadOnlyList<string> equippedIds,
            RobotDefinition       robotDef,
            PlayerInventory       inventory,
            ShopCatalog           catalog)
        {
            // ── Null guards ───────────────────────────────────────────────────

            if (equippedIds == null)
                return LoadoutValidationResult.Invalid(
                    new List<string> { "Equipped part list is null." });

            if (robotDef == null)
                return LoadoutValidationResult.Invalid(
                    new List<string> { "No robot definition assigned." });

            var errors = new List<string>();

            // ── Build catalog lookup (partId → PartDefinition) ────────────────

            Dictionary<string, PartDefinition> catalogLookup = null;
            if (catalog != null)
            {
                IReadOnlyList<PartDefinition> allParts = catalog.Parts;
                catalogLookup = new Dictionary<string, PartDefinition>(allParts.Count);
                for (int i = 0; i < allParts.Count; i++)
                {
                    PartDefinition def = allParts[i];
                    if (def == null) continue;
                    // First occurrence wins (catalog OnValidate already warns on dupes).
                    if (!catalogLookup.ContainsKey(def.PartId))
                        catalogLookup[def.PartId] = def;
                }
            }

            // ── Determine required categories ─────────────────────────────────

            var requiredCategories = new HashSet<PartCategory>();
            if (catalog != null)
            {
                IReadOnlyList<PartSlot> slots = robotDef.Slots;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null)
                        requiredCategories.Add(slots[i].category);
                }
            }

            // ── Validate each equipped part ───────────────────────────────────

            var coveredCategories = new HashSet<PartCategory>();
            int weaponCount = 0;

            for (int i = 0; i < equippedIds.Count; i++)
            {
                string id = equippedIds[i];

                // Catalog membership check.
                if (catalogLookup != null)
                {
                    if (catalogLookup.TryGetValue(id, out PartDefinition def))
                    {
                        coveredCategories.Add(def.Category);
                        if (def.Category == PartCategory.Weapon)
                            weaponCount++;
                    }
                    else
                        errors.Add($"Part not found in catalog: '{id}'.");
                }

                // Ownership check.
                if (inventory != null && !inventory.HasPart(id))
                    errors.Add($"Part not owned: '{id}'.");
            }

            // ── Slot coverage check (non-Weapon categories) ───────────────────
            // The Weapon category is handled separately by the exactly-one check.

            if (catalog != null)
            {
                foreach (PartCategory required in requiredCategories)
                {
                    if (required == PartCategory.Weapon) continue;
                    if (!coveredCategories.Contains(required))
                        errors.Add($"No {required} part equipped.");
                }
            }

            // ── Exactly-one-Weapon check ──────────────────────────────────────
            // Enforced only when the robot definition includes a Weapon slot and
            // the catalog is available for category resolution.

            if (catalogLookup != null && requiredCategories.Contains(PartCategory.Weapon)
                && weaponCount != 1)
            {
                errors.Add(weaponCount == 0
                    ? "Exactly one Weapon part required (none equipped)."
                    : $"Exactly one Weapon part required (found {weaponCount}).");
            }

            return errors.Count == 0
                ? LoadoutValidationResult.Valid
                : LoadoutValidationResult.Invalid(errors);
        }

        // ── Convenience overload for PlayerLoadout SO ─────────────────────────

        /// <summary>
        /// Validates a <see cref="PlayerLoadout"/> SO.
        /// Returns an invalid result immediately when <paramref name="loadout"/>
        /// is <c>null</c>; otherwise delegates to
        /// <see cref="Validate(IReadOnlyList{string}, RobotDefinition, PlayerInventory, ShopCatalog)"/>.
        /// </summary>
        public static LoadoutValidationResult Validate(
            PlayerLoadout   loadout,
            RobotDefinition robotDef,
            PlayerInventory inventory,
            ShopCatalog     catalog)
        {
            if (loadout == null)
                return LoadoutValidationResult.Invalid(
                    new List<string> { "No player loadout assigned." });

            return Validate(loadout.EquippedPartIds, robotDef, inventory, catalog);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pure static utility that computes a single composite "build power" score
    /// for the player's currently equipped robot parts.
    ///
    /// ── Scoring formula ───────────────────────────────────────────────────────
    ///   For each equipped part found in the catalog:
    ///     1. Rarity contribution   = config.GetRarityPoints(part.Rarity)
    ///     2. Upgrade contribution  = upgrades.GetTier(part.PartId) × config.UpgradeWeight
    ///        (skipped when <paramref name="upgrades"/> or <paramref name="config"/> is null)
    ///     3. Stat contribution     = (part.Stats.healthBonus
    ///                                 + (part.Stats.speedMultiplier  - 1f) × 50f
    ///                                 + (part.Stats.damageMultiplier - 1f) × 50f
    ///                                 + part.Stats.armorRating) × config.BaseStatWeight
    ///   Plus, for the whole build:
    ///     4. Synergy contribution  = activeSynergies.Count × config.SynergyWeight
    ///        (skipped when <paramref name="synergyConfig"/> or catalog is null)
    ///
    ///   Final = Mathf.Max(0, Mathf.RoundToInt(Σ all contributions))
    ///
    /// ── Null safety ───────────────────────────────────────────────────────────
    ///   • null <paramref name="loadout"/> or <paramref name="catalog"/>     → 0
    ///   • null <paramref name="config"/>                                     → 0
    ///   • null <paramref name="upgrades"/>                                   → upgrade contribution skipped (0)
    ///   • null <paramref name="synergyConfig"/>                              → synergy contribution skipped (0)
    ///   • null PartDefinition entries in catalog / part IDs not in catalog   → skipped
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   Static class — no MonoBehaviour, no Unity serialisation dependency.
    ///   Usable in EditMode tests without a running Player.
    /// </summary>
    public static class RobotBuildRatingCalculator
    {
        /// <summary>
        /// Computes the build power rating for the given loadout.
        /// </summary>
        /// <param name="loadout">The player's current equipped part IDs.</param>
        /// <param name="catalog">Shop catalog used to resolve IDs to PartDefinitions.</param>
        /// <param name="upgrades">
        ///   Per-part upgrade tiers. May be null (upgrade contribution treated as 0).
        /// </param>
        /// <param name="synergyConfig">
        ///   Active-synergy evaluator. May be null (synergy contribution treated as 0).
        /// </param>
        /// <param name="config">
        ///   Weight coefficients. If null, returns 0 immediately.
        /// </param>
        /// <returns>Non-negative integer build power score.</returns>
        public static int Calculate(
            PlayerLoadout          loadout,
            ShopCatalog            catalog,
            PlayerPartUpgrades     upgrades,
            PartSynergyConfig      synergyConfig,
            RobotBuildRatingConfig config)
        {
            // Without a config there are no weights — return 0.
            if (config == null) return 0;

            // Without a loadout or catalog we cannot resolve any parts.
            if (loadout == null || catalog == null) return 0;

            IReadOnlyList<string> equippedIds = loadout.EquippedPartIds;
            if (equippedIds == null || equippedIds.Count == 0) return 0;

            // Build partId → PartDefinition lookup from the catalog (O(n) cold path).
            var catalogLookup = new Dictionary<string, PartDefinition>(
                catalog.Parts.Count, System.StringComparer.Ordinal);
            foreach (PartDefinition part in catalog.Parts)
            {
                if (part != null && !string.IsNullOrWhiteSpace(part.PartId))
                    catalogLookup[part.PartId] = part;
            }

            float total = 0f;

            // Per-part contributions.
            foreach (string id in equippedIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!catalogLookup.TryGetValue(id, out PartDefinition def)) continue;

                // 1. Rarity points.
                total += config.GetRarityPoints(def.Rarity);

                // 2. Upgrade tier contribution.
                if (upgrades != null)
                    total += upgrades.GetTier(id) * config.UpgradeWeight;

                // 3. Stat contribution.
                PartStats s         = def.Stats;
                float statScore     = s.healthBonus
                                    + (s.speedMultiplier  - 1f) * 50f
                                    + (s.damageMultiplier - 1f) * 50f
                                    + s.armorRating;
                total += statScore * config.BaseStatWeight;
            }

            // 4. Synergy contribution.
            if (synergyConfig != null)
            {
                IReadOnlyList<PartSynergyEntry> active =
                    synergyConfig.GetActiveSynergies(equippedIds, catalog);
                total += active.Count * config.SynergyWeight;
            }

            return Mathf.Max(0, Mathf.RoundToInt(total));
        }
    }
}

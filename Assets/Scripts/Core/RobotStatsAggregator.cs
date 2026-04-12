using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Pure static utility that combines a <see cref="RobotDefinition"/>'s base stats
    /// with the <see cref="PartStats"/> of every equipped <see cref="PartDefinition"/>
    /// into a single <see cref="RobotCombatStats"/> snapshot.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   Called once per match start (e.g. from MatchFlowController) after
    ///   RobotAssembler.Assemble() has populated the equipped-parts list.
    ///   The result can be used to initialise HealthSO._maxHealth at runtime
    ///   or to display a stat summary in the pre-match lobby.
    ///
    /// ── Aggregation rules ────────────────────────────────────────────────────
    ///   TotalMaxHealth           = base.MaxHitPoints + Σ healthBonus
    ///   EffectiveSpeed           = base.MoveSpeed    × Π speedMultiplier
    ///   EffectiveDamageMultiplier =                    Π damageMultiplier
    ///   TotalArmorRating         = clamp(Σ armorRating, 0, 100)
    ///
    /// ── Null safety ───────────────────────────────────────────────────────────
    ///   • null robotDefinition  → returns a zero-stat RobotCombatStats
    ///   • null equippedParts    → treated as empty enumerable (base stats only)
    ///   • null PartDefinition entries inside the collection are skipped
    ///
    /// ── Architecture ────────────────────────────────────────────────────────
    ///   Static class — no MonoBehaviour, no heap allocations per call (stack
    ///   only), no Unity serialisation dependency. Usable in EditMode tests.
    /// </summary>
    public static class RobotStatsAggregator
    {
        /// <summary>
        /// Computes the aggregate <see cref="RobotCombatStats"/> for a robot.
        /// </summary>
        /// <param name="robotDefinition">
        /// The robot's chassis definition supplying base MaxHitPoints and MoveSpeed.
        /// May be null (returns default zero stats with a warning in Editor builds).
        /// </param>
        /// <param name="equippedParts">
        /// All currently equipped parts whose <see cref="PartStats"/> contribute.
        /// May be null or empty (returns base stats only).
        /// Null entries in the collection are silently skipped.
        /// </param>
        /// <returns>The resolved <see cref="RobotCombatStats"/>.</returns>
        public static RobotCombatStats Compute(
            RobotDefinition             robotDefinition,
            IEnumerable<PartDefinition> equippedParts)
        {
            if (robotDefinition == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[RobotStatsAggregator] Compute called with null " +
                                 "robotDefinition — returning zero stats.");
#endif
                return new RobotCombatStats(0f, 0f, 0f, 0);
            }

            float baseHealth = robotDefinition.MaxHitPoints;
            float baseSpeed  = robotDefinition.MoveSpeed;

            int   totalHealthBonus      = 0;
            float totalSpeedMult        = 1f;
            float totalDamageMult       = 1f;
            int   totalArmor            = 0;

            if (equippedParts != null)
            {
                foreach (PartDefinition part in equippedParts)
                {
                    if (part == null) continue;

                    PartStats s       = part.Stats;
                    totalHealthBonus += s.healthBonus;
                    totalSpeedMult   *= s.speedMultiplier;
                    totalDamageMult  *= s.damageMultiplier;
                    totalArmor       += s.armorRating;
                }
            }

            float finalHealth = baseHealth + totalHealthBonus;
            float finalSpeed  = baseSpeed  * totalSpeedMult;
            int   clampedArmor = Mathf.Clamp(totalArmor, 0, 100);

            return new RobotCombatStats(finalHealth, finalSpeed, totalDamageMult, clampedArmor);
        }

        /// <summary>
        /// Upgrade-aware overload: same aggregation as <see cref="Compute(RobotDefinition,
        /// IEnumerable{PartDefinition})"/> but scales each part's bonus stats by the
        /// tier multiplier from <paramref name="upgradeConfig"/> at the tier stored in
        /// <paramref name="upgrades"/>.
        ///
        /// ── Upgrade scaling ──────────────────────────────────────────────────
        ///   For a part at tier T with multiplier M = upgradeConfig.GetStatMultiplier(T):
        ///     healthBonus      → scaled additively:  (int)(healthBonus × M)
        ///     speedMultiplier  → bonus above 1.0:    1 + (speedMultiplier  - 1) × M
        ///     damageMultiplier → bonus above 1.0:    1 + (damageMultiplier - 1) × M
        ///     armorRating      → scaled additively:  (int)(armorRating × M)
        ///
        ///   A neutral part (speedMultiplier = 1.0, healthBonus = 0, …) is unaffected
        ///   by any upgrade tier — only non-zero bonus stats are amplified.
        ///
        /// Falls back to the base overload when <paramref name="upgrades"/> or
        /// <paramref name="upgradeConfig"/> is null.
        /// </summary>
        public static RobotCombatStats Compute(
            RobotDefinition             robotDefinition,
            IEnumerable<PartDefinition> equippedParts,
            PlayerPartUpgrades          upgrades,
            PartUpgradeConfig           upgradeConfig)
        {
            // Fall back gracefully when upgrade system is not configured.
            if (upgrades == null || upgradeConfig == null)
                return Compute(robotDefinition, equippedParts);

            if (robotDefinition == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[RobotStatsAggregator] Compute (upgrade) called with null " +
                                 "robotDefinition — returning zero stats.");
#endif
                return new RobotCombatStats(0f, 0f, 0f, 0);
            }

            float baseHealth = robotDefinition.MaxHitPoints;
            float baseSpeed  = robotDefinition.MoveSpeed;

            int   totalHealthBonus = 0;
            float totalSpeedMult   = 1f;
            float totalDamageMult  = 1f;
            int   totalArmor       = 0;

            if (equippedParts != null)
            {
                foreach (PartDefinition part in equippedParts)
                {
                    if (part == null) continue;

                    PartStats s    = part.Stats;
                    float     mult = upgradeConfig.GetStatMultiplier(upgrades.GetTier(part.PartId));

                    // Health and armor scale additively (bonus × multiplier).
                    totalHealthBonus += Mathf.RoundToInt(s.healthBonus * mult);
                    totalArmor       += Mathf.RoundToInt(s.armorRating * mult);

                    // Speed and damage: only the bonus-above-1.0 is amplified, preventing
                    // exponential compounding on neutral (1.0) parts.
                    totalSpeedMult  *= 1f + (s.speedMultiplier  - 1f) * mult;
                    totalDamageMult *= 1f + (s.damageMultiplier - 1f) * mult;
                }
            }

            float finalHealth  = baseHealth + totalHealthBonus;
            float finalSpeed   = baseSpeed  * totalSpeedMult;
            int   clampedArmor = Mathf.Clamp(totalArmor, 0, 100);

            return new RobotCombatStats(finalHealth, finalSpeed, totalDamageMult, clampedArmor);
        }

        /// <summary>
        /// Folds the numeric bonuses of every active synergy entry into an existing
        /// <see cref="RobotCombatStats"/> snapshot and returns an updated snapshot.
        ///
        /// ── Stacking rules ───────────────────────────────────────────────────
        ///   Multiple active synergies stack additively before being applied:
        ///     TotalMaxHealth           += Σ <see cref="PartSynergyEntry.healthBonus"/>
        ///     EffectiveSpeed           ×= (1 + Σ <see cref="PartSynergyEntry.speedMultiplierBonus"/>)
        ///     EffectiveDamageMultiplier×= (1 + Σ <see cref="PartSynergyEntry.damageMultiplierBonus"/>)
        ///     TotalArmorRating         += Σ <see cref="PartSynergyEntry.armorBonus"/>,
        ///                                clamped to [0, 100]
        ///
        /// ── Null safety ──────────────────────────────────────────────────────
        ///   • null or empty <paramref name="activeSynergies"/> → returns
        ///     <paramref name="baseStats"/> unchanged (zero allocation).
        ///   • null entries inside the collection are skipped.
        ///
        /// Called from <see cref="BattleRobots.Physics.CombatStatsApplicator.ApplyStats"/>
        /// after the base <see cref="Compute"/> call, so the synergy bonuses layer on top
        /// of part stats and upgrade tiers.
        /// </summary>
        /// <param name="baseStats">
        ///   The combat stats snapshot produced by one of the <see cref="Compute"/>
        ///   overloads.  Treated as read-only; a new struct is returned.
        /// </param>
        /// <param name="activeSynergies">
        ///   The list returned by
        ///   <see cref="PartSynergyConfig.GetActiveSynergies"/>.
        ///   May be null or empty.
        /// </param>
        public static RobotCombatStats ApplySynergies(
            RobotCombatStats                baseStats,
            IReadOnlyList<PartSynergyEntry> activeSynergies)
        {
            if (activeSynergies == null || activeSynergies.Count == 0)
                return baseStats;

            int   totalHealthBonus = 0;
            float totalSpeedBonus  = 0f;
            float totalDamageBonus = 0f;
            int   totalArmorBonus  = 0;

            foreach (PartSynergyEntry synergy in activeSynergies)
            {
                if (synergy == null) continue;
                totalHealthBonus += synergy.healthBonus;
                totalSpeedBonus  += synergy.speedMultiplierBonus;
                totalDamageBonus += synergy.damageMultiplierBonus;
                totalArmorBonus  += synergy.armorBonus;
            }

            float finalHealth = baseStats.TotalMaxHealth + totalHealthBonus;
            float finalSpeed  = baseStats.EffectiveSpeed * (1f + totalSpeedBonus);
            float finalDamage = baseStats.EffectiveDamageMultiplier * (1f + totalDamageBonus);
            int   finalArmor  = Mathf.Clamp(baseStats.TotalArmorRating + totalArmorBonus, 0, 100);

            return new RobotCombatStats(finalHealth, finalSpeed, finalDamage, finalArmor);
        }
    }
}

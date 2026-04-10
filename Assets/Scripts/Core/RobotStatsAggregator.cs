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
    }
}

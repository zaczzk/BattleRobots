using System;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable aggregate of a robot's final combat stats after all equipped parts
    /// have been factored in.
    ///
    /// Produced by <see cref="RobotStatsAggregator.Compute"/>; consumed by gameplay
    /// systems that need a resolved snapshot (HealthSO initialisation, locomotion
    /// speed, damage scaling, etc.).
    ///
    /// ── Aggregation rules ────────────────────────────────────────────────────
    ///   TotalMaxHealth           = RobotDefinition.MaxHitPoints
    ///                             + Σ PartStats.healthBonus
    ///   EffectiveSpeed           = RobotDefinition.MoveSpeed
    ///                             × Π PartStats.speedMultiplier
    ///   EffectiveDamageMultiplier = Π PartStats.damageMultiplier
    ///   TotalArmorRating         = clamp(Σ PartStats.armorRating, 0, 100)
    /// </summary>
    public readonly struct RobotCombatStats : IEquatable<RobotCombatStats>
    {
        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Base MaxHitPoints from <see cref="RobotDefinition"/> plus the sum of all
        /// equipped parts' <see cref="PartStats.healthBonus"/> values.
        /// Always ≥ 1.
        /// </summary>
        public float TotalMaxHealth { get; }

        /// <summary>
        /// Base MoveSpeed from <see cref="RobotDefinition"/> multiplied by the product
        /// of all equipped parts' <see cref="PartStats.speedMultiplier"/> values.
        /// Always > 0.
        /// </summary>
        public float EffectiveSpeed { get; }

        /// <summary>
        /// Product of all equipped parts' <see cref="PartStats.damageMultiplier"/> values.
        /// 1.0 when no parts are equipped.  Always > 0.
        /// </summary>
        public float EffectiveDamageMultiplier { get; }

        /// <summary>
        /// Sum of all equipped parts' <see cref="PartStats.armorRating"/> values,
        /// clamped to [0, 100].  Represents flat damage reduction (game systems
        /// apply this however appropriate — e.g. subtract from incoming damage).
        /// </summary>
        public int TotalArmorRating { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        public RobotCombatStats(float totalMaxHealth, float effectiveSpeed,
                                float effectiveDamageMultiplier, int totalArmorRating)
        {
            TotalMaxHealth            = totalMaxHealth;
            EffectiveSpeed            = effectiveSpeed;
            EffectiveDamageMultiplier = effectiveDamageMultiplier;
            TotalArmorRating          = totalArmorRating;
        }

        // ── Equality ──────────────────────────────────────────────────────────

        public bool Equals(RobotCombatStats other) =>
            TotalMaxHealth.Equals(other.TotalMaxHealth) &&
            EffectiveSpeed.Equals(other.EffectiveSpeed) &&
            EffectiveDamageMultiplier.Equals(other.EffectiveDamageMultiplier) &&
            TotalArmorRating == other.TotalArmorRating;

        public override bool Equals(object obj) =>
            obj is RobotCombatStats other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TotalMaxHealth.GetHashCode();
                hash = (hash * 397) ^ EffectiveSpeed.GetHashCode();
                hash = (hash * 397) ^ EffectiveDamageMultiplier.GetHashCode();
                hash = (hash * 397) ^ TotalArmorRating;
                return hash;
            }
        }

        public static bool operator ==(RobotCombatStats a, RobotCombatStats b) =>  a.Equals(b);
        public static bool operator !=(RobotCombatStats a, RobotCombatStats b) => !a.Equals(b);

        public override string ToString() =>
            $"RobotCombatStats(HP={TotalMaxHealth}, Speed={EffectiveSpeed}, " +
            $"DmgMult={EffectiveDamageMultiplier}, Armor={TotalArmorRating})";
    }
}

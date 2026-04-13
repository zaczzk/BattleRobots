namespace BattleRobots.Core
{
    /// <summary>
    /// Damage-type taxonomy used by <see cref="DamageInfo"/> and
    /// <see cref="DamageResistanceConfig"/> to model resistances and vulnerabilities.
    ///
    /// ── Type definitions ────────────────────────────────────────────────────────
    ///   Physical — kinetic / impact damage (default for all attacks that don't specify a type).
    ///   Energy   — beam / plasma / laser attacks.
    ///   Thermal  — fire / explosion / burn attacks (distinct from the StatusEffect Burn tick).
    ///   Shock    — electric / EMP attacks; typically countered by heavily-shielded robots.
    ///
    /// ── Usage ───────────────────────────────────────────────────────────────────
    ///   Set on <see cref="DamageInfo.damageType"/> at the damage source.
    ///   <see cref="DamageResistanceConfig.ApplyResistance"/> reads this field to
    ///   reduce incoming damage before it reaches <see cref="HealthSO"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   Integer-backed enum for O(1) array indexing in DamageResistanceConfig.
    ///   New types may be appended without breaking existing assets (additive only).
    ///   BattleRobots.Core namespace; no Physics / UI references.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Kinetic / impact damage. Default type for unspecified attacks.</summary>
        Physical = 0,

        /// <summary>Beam / plasma / laser attacks.</summary>
        Energy = 1,

        /// <summary>Fire / explosion attacks. Distinct from the Burn status-effect tick.</summary>
        Thermal = 2,

        /// <summary>Electric / EMP attacks.</summary>
        Shock = 3,
    }
}

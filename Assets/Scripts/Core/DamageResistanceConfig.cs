using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data SO that configures per-type damage resistance for a robot or part.
    ///
    /// ── Resistance rules ────────────────────────────────────────────────────────
    ///   Each resistance value is a fraction in [0, 0.9] that reduces incoming damage
    ///   of the matching <see cref="DamageType"/>:
    ///     effective damage = raw damage × (1 − resistance)
    ///   A value of 0 (default) means no resistance — full damage passes through.
    ///   A cap of 0.9 prevents total immunity (at most 90 % reduction).
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   Assign to the optional field on <see cref="HealthSO"/> (or any damage
    ///   pipeline stage that accepts a DamageResistanceConfig).
    ///   Call <see cref="ApplyResistance"/> with the raw amount and the
    ///   <see cref="DamageType"/> carried by the <see cref="DamageInfo"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties are read-only; asset is immutable at runtime.
    ///   - Zero allocation on the hot path: switch + float arithmetic only.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ DamageResistanceConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/DamageResistanceConfig")]
    public sealed class DamageResistanceConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Resistances (0 = none, 0.9 = 90 % reduction)")]
        [Tooltip("Fraction of Physical damage absorbed (0 = none, 0.9 = 90 % max).")]
        [SerializeField, Range(0f, 0.9f)] private float _physicalResistance = 0f;

        [Tooltip("Fraction of Energy damage absorbed.")]
        [SerializeField, Range(0f, 0.9f)] private float _energyResistance = 0f;

        [Tooltip("Fraction of Thermal damage absorbed.")]
        [SerializeField, Range(0f, 0.9f)] private float _thermalResistance = 0f;

        [Tooltip("Fraction of Shock damage absorbed.")]
        [SerializeField, Range(0f, 0.9f)] private float _shockResistance = 0f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Fraction of Physical damage absorbed. Range [0, 0.9].</summary>
        public float PhysicalResistance => _physicalResistance;

        /// <summary>Fraction of Energy damage absorbed. Range [0, 0.9].</summary>
        public float EnergyResistance => _energyResistance;

        /// <summary>Fraction of Thermal damage absorbed. Range [0, 0.9].</summary>
        public float ThermalResistance => _thermalResistance;

        /// <summary>Fraction of Shock damage absorbed. Range [0, 0.9].</summary>
        public float ShockResistance => _shockResistance;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the resistance fraction for the given <paramref name="type"/>.
        /// Returns 0 for unknown types (no reduction).
        /// Zero allocation — switch on value-type enum.
        /// </summary>
        public float GetResistance(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalResistance;
                case DamageType.Energy:   return _energyResistance;
                case DamageType.Thermal:  return _thermalResistance;
                case DamageType.Shock:    return _shockResistance;
                default:                  return 0f;
            }
        }

        /// <summary>
        /// Returns <paramref name="rawDamage"/> reduced by the resistance for
        /// <paramref name="type"/>: effective = rawDamage × (1 − resistance).
        /// Returns 0 when <paramref name="rawDamage"/> is zero or negative.
        /// Result is clamped to a minimum of 0 (never negative).
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public float ApplyResistance(float rawDamage, DamageType type)
        {
            if (rawDamage <= 0f) return 0f;
            return Mathf.Max(0f, rawDamage * (1f - GetResistance(type)));
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _physicalResistance = Mathf.Clamp(_physicalResistance, 0f, 0.9f);
            _energyResistance   = Mathf.Clamp(_energyResistance,   0f, 0.9f);
            _thermalResistance  = Mathf.Clamp(_thermalResistance,  0f, 0.9f);
            _shockResistance    = Mathf.Clamp(_shockResistance,    0f, 0.9f);
        }
#endif
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data SO that specifies the cumulative per-type damage threshold
    /// a player must deal (across all matches) to earn a mastery badge for each
    /// <see cref="DamageType"/>.
    ///
    /// ── Design intent ─────────────────────────────────────────────────────────────────
    ///   A mastery badge is a persistent cosmetic indicator that rewards players for
    ///   specialising in a particular damage type over their career.  The default
    ///   threshold is 1 000 cumulative damage per type — tweak per-game-balance need.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────────────
    ///   Assign to <see cref="DamageTypeMasterySO._config"/> in the Inspector.
    ///   <see cref="GetThreshold"/> is called by <see cref="DamageTypeMasterySO.AddDealt"/>
    ///   to decide when to award mastery.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties read-only; asset is immutable at runtime.
    ///   - Zero allocation on the hot path: property reads + switch only.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ DamageTypeMasteryConfig.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Combat/DamageTypeMasteryConfig",
        fileName = "DamageTypeMasteryConfig")]
    public sealed class DamageTypeMasteryConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Mastery Thresholds (cumulative damage dealt)")]
        [Tooltip("Total Physical damage the player must deal to earn the Physical mastery badge.")]
        [SerializeField, Min(1f)] private float _physicalThreshold = 1000f;

        [Tooltip("Total Energy damage the player must deal to earn the Energy mastery badge.")]
        [SerializeField, Min(1f)] private float _energyThreshold = 1000f;

        [Tooltip("Total Thermal damage the player must deal to earn the Thermal mastery badge.")]
        [SerializeField, Min(1f)] private float _thermalThreshold = 1000f;

        [Tooltip("Total Shock damage the player must deal to earn the Shock mastery badge.")]
        [SerializeField, Min(1f)] private float _shockThreshold = 1000f;

        // ── Read-only properties ──────────────────────────────────────────────

        /// <summary>Cumulative Physical damage threshold for mastery. Default 1 000.</summary>
        public float PhysicalThreshold => _physicalThreshold;

        /// <summary>Cumulative Energy damage threshold for mastery. Default 1 000.</summary>
        public float EnergyThreshold   => _energyThreshold;

        /// <summary>Cumulative Thermal damage threshold for mastery. Default 1 000.</summary>
        public float ThermalThreshold  => _thermalThreshold;

        /// <summary>Cumulative Shock damage threshold for mastery. Default 1 000.</summary>
        public float ShockThreshold    => _shockThreshold;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the mastery threshold for <paramref name="type"/>.
        /// Returns 1f for unknown / out-of-range types (effectively a minimal threshold —
        /// never blocks mastery for undefined types).
        /// </summary>
        public float GetThreshold(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalThreshold;
                case DamageType.Energy:   return _energyThreshold;
                case DamageType.Thermal:  return _thermalThreshold;
                case DamageType.Shock:    return _shockThreshold;
                default:                  return 1f;
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _physicalThreshold = Mathf.Max(1f, _physicalThreshold);
            _energyThreshold   = Mathf.Max(1f, _energyThreshold);
            _thermalThreshold  = Mathf.Max(1f, _thermalThreshold);
            _shockThreshold    = Mathf.Max(1f, _shockThreshold);
        }
#endif
    }
}

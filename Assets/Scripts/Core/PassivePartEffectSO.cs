using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// The type of passive stat modifier a <see cref="PassivePartEffectSO"/> applies.
    /// </summary>
    public enum PassiveStatType
    {
        /// <summary>
        /// Adds flat damage reduction (armor rating) to the target
        /// <see cref="BattleRobots.Physics.DamageReceiver"/>.
        /// </summary>
        DamageReduction = 0,

        /// <summary>
        /// Adds a flat bonus to the robot's max health via
        /// <see cref="HealthSO.InitForMatch"/> + <see cref="HealthSO.Reset"/>.
        /// </summary>
        MaxHealthBonus = 1,

        /// <summary>
        /// Adds a flat bonus to the passive recharge rate of the robot's
        /// <see cref="EnergySystemSO"/> via <see cref="EnergySystemSO.SetRechargeRate"/>.
        /// </summary>
        RechargeRateBonus = 2,
    }

    /// <summary>
    /// Immutable data asset describing a passive part modifier.
    ///
    /// ── Design ────────────────────────────────────────────────────────────────
    ///   Passive effects are always-on bonuses applied once at match start by
    ///   <see cref="BattleRobots.Physics.PassiveEffectApplier"/>. They complement the
    ///   active-ability system (AbilityController) and give equipped parts meaning
    ///   beyond their visual slot.
    ///
    ///   Supported modifiers:
    ///   <list type="bullet">
    ///     <item><see cref="PassiveStatType.DamageReduction"/> — flat armor-rating bonus.</item>
    ///     <item><see cref="PassiveStatType.MaxHealthBonus"/> — extra max health added at
    ///       match start (scales HealthSO.InitForMatch).</item>
    ///     <item><see cref="PassiveStatType.RechargeRateBonus"/> — flat addition to the
    ///       robot's passive energy-recharge rate.</item>
    ///   </list>
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • SO asset is immutable at runtime — all fields are read-only properties.
    ///   • Create via Assets ▶ Create ▶ BattleRobots ▶ Physics ▶ PassivePartEffect.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Physics/PassivePartEffect", order = 20)]
    public sealed class PassivePartEffectSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Effect")]
        [Tooltip("Which stat this passive modifies.")]
        [SerializeField] private PassiveStatType _statType = PassiveStatType.DamageReduction;

        [Tooltip("Magnitude of the bonus. Always ≥ 0.")]
        [SerializeField, Min(0f)] private float _value = 5f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Which stat this passive modifies.</summary>
        public PassiveStatType StatType => _statType;

        /// <summary>Magnitude of the bonus. Always ≥ 0.</summary>
        public float Value => _value;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_value == 0f)
                Debug.LogWarning(
                    $"[PassivePartEffectSO] '{name}' has a Value of 0 — " +
                    "this passive effect will have no gameplay impact.", this);
        }
#endif
    }
}

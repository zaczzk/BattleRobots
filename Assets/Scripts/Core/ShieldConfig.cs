using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable configuration ScriptableObject for the robot shield system.
    ///
    /// Defines maximum shield capacity, recharge rate, and the idle delay before
    /// recharge resumes after the last hit. Assign one instance per robot variant
    /// to <see cref="BattleRobots.Physics.ShieldController"/>.
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace. No Physics or UI references.
    ///   Treated as immutable at runtime — never write to fields from gameplay code.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ ShieldConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/ShieldConfig", order = 15)]
    public sealed class ShieldConfig : ScriptableObject
    {
        [Header("Capacity")]
        [Tooltip("Maximum shield hit-points. The shield recharges back to this value.")]
        [SerializeField, Min(0f)] private float _maxShieldHP = 50f;

        [Header("Recharge")]
        [Tooltip("Hit-points restored per second once recharge begins.")]
        [SerializeField, Min(0f)] private float _rechargeRate = 10f;

        [Tooltip("Seconds after the last hit before recharge starts.")]
        [SerializeField, Min(0f)] private float _rechargeDelay = 3f;

        /// <summary>Maximum shield hit-points.</summary>
        public float MaxShieldHP   => _maxShieldHP;

        /// <summary>Hit-points restored per second during recharge.</summary>
        public float RechargeRate  => _rechargeRate;

        /// <summary>Seconds of inactivity required before recharge begins.</summary>
        public float RechargeDelay => _rechargeDelay;
    }
}

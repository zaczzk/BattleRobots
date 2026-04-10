using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO containing AI tuning parameters for a named difficulty preset.
    ///
    /// Assign to <see cref="BattleRobots.Physics.RobotAIController._difficultyConfig"/>
    /// to override the component's inspector defaults at Awake time, enabling designers
    /// to create Easy / Normal / Hard variants without touching code.
    ///
    /// Preset authoring guide (suggested values):
    ///   Easy   — detectionRange 8,  attackRange 2, attackDamage 5,  cooldown 2.0, speed 0.7
    ///   Normal — detectionRange 15, attackRange 3, attackDamage 10, cooldown 1.0, speed 1.0
    ///   Hard   — detectionRange 22, attackRange 4, attackDamage 18, cooldown 0.5, speed 1.5
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ AI ▶ BotDifficultyConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/AI/BotDifficultyConfig", order = 0)]
    public sealed class BotDifficultyConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Detection")]
        [Tooltip("Distance at which the AI notices the player and begins chasing.")]
        [SerializeField, Min(0f)] private float _detectionRange = 15f;

        [Tooltip("Distance at which the AI stops moving and begins attacking.")]
        [SerializeField, Min(0f)] private float _attackRange = 3f;

        [Header("Attack")]
        [Tooltip("Damage broadcast per attack cycle via DamageGameEvent.")]
        [SerializeField, Min(0f)] private float _attackDamage = 10f;

        [Tooltip("Seconds between successive attacks. Lower = more aggressive.")]
        [SerializeField, Min(0.1f)] private float _attackCooldown = 1f;

        [Header("Steering")]
        [Tooltip("Angle (degrees) within which the AI considers itself 'facing' the target " +
                 "and applies full forward input. Outside this cone it turns in place.")]
        [SerializeField, Min(1f)] private float _facingThreshold = 20f;

        [Header("Movement")]
        [Tooltip("Multiplied against the locomotion controller's base move and turn speeds. " +
                 "1.0 = unchanged; 0.7 = 70 % speed (Easy); 1.5 = 150 % speed (Hard).")]
        [SerializeField, Range(0.1f, 3f)] private float _moveSpeedMultiplier = 1f;

        // ── Read-only properties (immutable at runtime) ───────────────────────

        /// <summary>Distance at which the AI detects and begins chasing the player.</summary>
        public float DetectionRange      => _detectionRange;

        /// <summary>Distance at which the AI switches from Chase to Attack state.</summary>
        public float AttackRange         => _attackRange;

        /// <summary>Damage broadcast per attack via DamageGameEvent.</summary>
        public float AttackDamage        => _attackDamage;

        /// <summary>Minimum seconds between successive attacks.</summary>
        public float AttackCooldown      => _attackCooldown;

        /// <summary>Facing-threshold angle (degrees) for full-forward input.</summary>
        public float FacingThreshold     => _facingThreshold;

        /// <summary>
        /// Scale factor applied to the locomotion controller's base move and turn speed.
        /// Applied once at Awake via <see cref="BattleRobots.Physics.RobotLocomotionController.SetSpeedMultiplier"/>.
        /// </summary>
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
    }
}

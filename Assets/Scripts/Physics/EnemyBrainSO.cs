using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Read-only ScriptableObject that configures an enemy robot's AI behaviour.
    /// All fields are immutable at runtime; EnemyController reads them each FixedUpdate.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Physics ▶ EnemyBrainSO.
    /// Tune per-robot archetype by creating multiple SO assets (e.g. AggressiveBrain, CautiousBrain).
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Physics/EnemyBrainSO", order = 0)]
    public sealed class EnemyBrainSO : ScriptableObject
    {
        [Header("Chase")]
        [Tooltip("Linear force (N) applied to the root ArticulationBody toward the player.")]
        [SerializeField, Min(0f)] private float _chaseForce = 500f;

        [Tooltip("Yaw torque (N·m) applied to rotate the root body to face the player.")]
        [SerializeField, Min(0f)] private float _turnTorque = 200f;

        [Tooltip("Distance (m) at which the AI transitions from Chase to Attack state.")]
        [SerializeField, Min(0f)] private float _attackRange = 2f;

        [Header("Attack")]
        [Tooltip("Target velocity (degrees/s) set on weapon HingeJointABs during Attack.")]
        [SerializeField] private float _weaponSpinSpeedDegPerSec = 720f;

        // ── Runtime accessors (read-only; SO immutable at runtime) ────────────

        /// <summary>Force in Newtons applied toward the player during Chase.</summary>
        public float ChaseForce => _chaseForce;

        /// <summary>Yaw torque in N·m used to steer toward the player during Chase.</summary>
        public float TurnTorque => _turnTorque;

        /// <summary>Distance threshold (m) for Chase → Attack transition.</summary>
        public float AttackRange => _attackRange;

        /// <summary>Weapon joint target velocity (degrees/s) during Attack.</summary>
        public float WeaponSpinSpeedDegPerSec => _weaponSpinSpeedDegPerSec;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_attackRange <= 0f)
                Debug.LogWarning($"[EnemyBrainSO] '{name}': attackRange should be > 0.");
        }
#endif
    }
}

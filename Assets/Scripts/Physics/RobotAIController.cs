using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Finite-state-machine AI controller for enemy robots.
    ///
    /// States
    ///   Idle   — stands still; transitions to Chase when target enters detection range.
    ///   Chase  — steers toward target; transitions to Attack when in attack range,
    ///            or back to Idle when target leaves detection range.
    ///   Attack — fires a <see cref="DamageGameEvent"/> on cooldown; transitions back
    ///            to Chase when target moves out of attack range.
    ///
    /// ARCHITECTURE RULES enforced here:
    ///   • Movement delegated entirely to <see cref="RobotLocomotionController"/> —
    ///     this class never touches ArticulationBody directly.
    ///   • Damage delivered via SO event channel (DamageGameEvent) — no direct
    ///     reference to the target's DamageReceiver or HealthSO.
    ///   • No heap allocations in FixedUpdate — struct ops only;
    ///     _robotId string cached once in Awake.
    ///   • BattleRobots.UI must NOT reference this class.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotAIController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Difficulty Override")]
        [Tooltip("Optional SO preset (e.g. Easy / Normal / Hard). When assigned, all Detection, " +
                 "Attack, and Steering inspector fields below are overridden at Awake time. " +
                 "Leave null to use the per-component inspector values directly.")]
        [SerializeField] private BotDifficultyConfig _difficultyConfig;

        [Header("References")]
        [Tooltip("Locomotion controller on this robot's root. Required.")]
        [SerializeField] private RobotLocomotionController _locomotion;

        [Tooltip("Transform to chase and attack. Assign the player's robot root.")]
        [SerializeField] private Transform _target;

        [Header("Ranges")]
        [Tooltip("Distance at which the AI notices the target and begins chasing.")]
        [SerializeField, Min(0f)] private float _detectionRange = 15f;

        [Tooltip("Distance at which the AI stops moving and begins attacking.")]
        [SerializeField, Min(0f)] private float _attackRange = 3f;

        [Header("Attack")]
        [Tooltip("Damage amount broadcast each attack cycle.")]
        [SerializeField, Min(0f)] private float _attackDamage = 10f;

        [Tooltip("Seconds between attacks.")]
        [SerializeField, Min(0f)] private float _attackCooldown = 1f;

        [Tooltip("SO event channel used to deliver damage. Must be the same channel " +
                 "that the target robot's DamageGameEventListener subscribes to.")]
        [SerializeField] private DamageGameEvent _damageEvent;

        [Header("Steering")]
        [Tooltip("Within this angle (degrees) the AI considers itself 'facing' the target " +
                 "and applies full forward input. Outside this cone it turns in place.")]
        [SerializeField, Min(1f)] private float _facingThreshold = 20f;

        // ── Private state (value types — zero alloc) ──────────────────────────

        private AIState _state           = AIState.Idle;
        private float   _cooldown        = 0f;
        private string  _robotId         = string.Empty;   // cached in Awake; used in DamageInfo
        private float   _damageMultiplier = 1f;             // set by CombatStatsApplicator

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache name once so FixedUpdate never allocates a string.
            _robotId = name;

            // Apply difficulty preset — overrides inspector tuning if assigned.
            // All assignments are simple field writes; no heap allocation.
            if (_difficultyConfig != null)
            {
                _detectionRange  = _difficultyConfig.DetectionRange;
                _attackRange     = _difficultyConfig.AttackRange;
                _attackDamage    = _difficultyConfig.AttackDamage;
                _attackCooldown  = _difficultyConfig.AttackCooldown;
                _facingThreshold = _difficultyConfig.FacingThreshold;

                // Locomotion speed: _locomotion is serialised so it's valid at Awake time
                // as long as it's assigned via Inspector (same-scene reference).
                _locomotion?.SetSpeedMultiplier(_difficultyConfig.MoveSpeedMultiplier);
            }
        }

        private void FixedUpdate()
        {
            if (_locomotion == null || _target == null) return;

            float dist = Vector3.Distance(transform.position, _target.position);

            switch (_state)
            {
                case AIState.Idle:
                    _locomotion.SetInputs(0f, 0f);
                    if (dist <= _detectionRange)
                        _state = AIState.Chase;
                    break;

                case AIState.Chase:
                    if (dist > _detectionRange)
                    {
                        _locomotion.SetInputs(0f, 0f);
                        _state = AIState.Idle;
                        break;
                    }
                    if (dist <= _attackRange)
                    {
                        _locomotion.SetInputs(0f, 0f);
                        _cooldown = 0f;             // first attack fires immediately
                        _state    = AIState.Attack;
                        break;
                    }
                    SteerToward();
                    break;

                case AIState.Attack:
                    if (dist > _attackRange)
                    {
                        _state = AIState.Chase;
                        break;
                    }
                    _locomotion.SetInputs(0f, 0f);  // stand still while attacking
                    _cooldown -= Time.fixedDeltaTime;
                    if (_cooldown <= 0f)
                    {
                        FireAttack();
                        _cooldown = _attackCooldown;
                    }
                    break;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Computes steering inputs to drive toward the target.
        /// Turns in place when outside the facing threshold; moves forward when aligned.
        /// No heap allocation — all vector operations are struct copies.
        /// </summary>
        private void SteerToward()
        {
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;    // ignore vertical offset — ground robot

            float angle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);

            // Turn input proportional to angle; clamped to [-1, 1] by SetInputs.
            float turnInput = angle / _facingThreshold;

            // Move forward only when sufficiently facing the target.
            float moveInput = Mathf.Abs(angle) <= _facingThreshold ? 1f : 0f;

            _locomotion.SetInputs(moveInput, turnInput);
        }

        /// <summary>
        /// Broadcasts a <see cref="DamageInfo"/> on the SO damage channel.
        /// No allocation — DamageInfo is a struct; _robotId string is pre-cached.
        /// </summary>
        private void FireAttack()
        {
            if (_damageEvent == null) return;

            var info = new DamageInfo(_attackDamage * _damageMultiplier, _robotId, _target.position);
            _damageEvent.Raise(info);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Override the chase/attack target at runtime (e.g., from MatchManager).</summary>
        public void SetTarget(Transform target) => _target = target;

        /// <summary>Current FSM state — read-only, exposed for debug UI and Editor tools.</summary>
        public AIState CurrentState => _state;

        /// <summary>
        /// Current damage output multiplier from equipped parts.
        /// Set by <c>CombatStatsApplicator</c> from <c>RobotCombatStats.EffectiveDamageMultiplier</c>.
        /// </summary>
        public float DamageMultiplier => _damageMultiplier;

        /// <summary>
        /// Applies the part-derived damage multiplier at match start.
        /// Multiplied into <see cref="_attackDamage"/> each time the AI fires.
        /// Values below 0.01 are clamped to 0.01. Allocation-free.
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            _damageMultiplier = Mathf.Max(0.01f, multiplier);
        }

        /// <summary>Force the AI into Idle and halt movement (e.g., when match ends).</summary>
        public void Disable()
        {
            _state = AIState.Idle;
            _locomotion?.Halt();
        }
    }

    /// <summary>FSM states for <see cref="RobotAIController"/>.</summary>
    public enum AIState
    {
        Idle,
        Chase,
        Attack
    }
}

using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// AI states for an opponent robot.
    /// </summary>
    public enum RobotAIState
    {
        /// <summary>Robot stands still; enters Approach when target comes within range.</summary>
        Idle,

        /// <summary>Robot drives toward the target.</summary>
        Approach,

        /// <summary>Robot drives toward the target and spins the weapon at full speed.</summary>
        Attack,
    }

    /// <summary>
    /// Simple finite-state machine for an AI robot opponent.
    ///
    /// States:  Idle → Approach → Attack → Approach (hysteresis)
    ///
    /// Locomotion uses differential steering via two <see cref="HingeJointAB"/> wheel
    /// joints (left / right). A third <see cref="HingeJointAB"/> drives the weapon.
    ///
    /// Architecture rules observed:
    ///   - Namespace <c>BattleRobots.Physics</c> (references Core and Physics).
    ///   - No heap allocations in FixedUpdate (all value-type locals).
    ///   - Cross-component comms via SO event channels; caller wires the channels.
    ///   - ArticulationBody only — no Rigidbody.
    ///   - HealthSO reference lives in Core; allowed from Physics layer.
    ///
    /// Wire-up (Inspector):
    ///   1. Assign Target transform (the player robot root).
    ///   2. Assign OwnHealth SO.
    ///   3. Assign LeftWheel, RightWheel, WeaponJoint HingeJointAB refs (all optional).
    ///   4. Tune ApproachRange, AttackRange, DriveSpeedRadPerSec.
    ///   5. Optionally assign state-entry event channels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotFSM : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Target")]
        [Tooltip("Transform of the robot this AI should fight (typically the player root).")]
        [SerializeField] private Transform _target;

        [Header("Health")]
        [Tooltip("HealthSO for this AI robot. FSM stops when IsAlive is false.")]
        [SerializeField] private HealthSO _ownHealth;

        [Header("Drive Joints")]
        [Tooltip("Left wheel / track HingeJointAB. Used for differential steering.")]
        [SerializeField] private HingeJointAB _leftWheel;

        [Tooltip("Right wheel / track HingeJointAB. Used for differential steering.")]
        [SerializeField] private HingeJointAB _rightWheel;

        [Tooltip("Weapon spinner HingeJointAB. Driven at full speed in Attack state.")]
        [SerializeField] private HingeJointAB _weaponJoint;

        [Header("Ranges")]
        [Tooltip("Distance at which the AI transitions from Idle to Approach.")]
        [SerializeField, Min(0.1f)] private float _approachRange = 6f;

        [Tooltip("Distance at which the AI transitions from Approach to Attack. " +
                 "Must be < ApproachRange.")]
        [SerializeField, Min(0.1f)] private float _attackRange = 2.5f;

        [Header("Drive Parameters")]
        [Tooltip("Forward drive speed applied to the wheel joints (rad/s).")]
        [SerializeField, Min(0f)] private float _driveSpeedRadPerSec = 8f;

        [Tooltip("Steering sharpness — higher values turn faster relative to forward speed. " +
                 "Range [0, 1].")]
        [SerializeField, Range(0f, 1f)] private float _steeringGain = 0.6f;

        [Tooltip("Weapon spin speed in Approach state (rad/s). 0 = weapon off.")]
        [SerializeField, Min(0f)] private float _idleWeaponSpeedRadPerSec = 0f;

        [Tooltip("Weapon spin speed in Attack state (rad/s).")]
        [SerializeField, Min(0f)] private float _attackWeaponSpeedRadPerSec = 30f;

        [Header("Difficulty (optional)")]
        [Tooltip("When assigned, scales AI detection ranges and drive speed. " +
                 "Leave null to use the raw inspector values.")]
        [SerializeField] private DifficultySO _difficulty;

        [Header("State Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEnterIdle;
        [SerializeField] private VoidGameEvent _onEnterApproach;
        [SerializeField] private VoidGameEvent _onEnterAttack;

        // ── Runtime State ─────────────────────────────────────────────────────

        private RobotAIState _state = RobotAIState.Idle;

        /// <summary>Current AI state. Read-only; transition via <see cref="ForceState"/>.</summary>
        public RobotAIState CurrentState => _state;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Overrides the current state from an external controller (e.g. MatchManager).
        /// Fires the appropriate entry event.
        /// </summary>
        public void ForceState(RobotAIState newState) => TransitionTo(newState);

        /// <summary>
        /// Assigns the target at runtime (e.g. after spawn).
        /// Safe to call from any MonoBehaviour.
        /// </summary>
        public void SetTarget(Transform target) => _target = target;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void FixedUpdate()
        {
            // Cease all activity if dead.
            if (_ownHealth != null && !_ownHealth.IsAlive)
            {
                AllStop();
                return;
            }

            // No target — stay idle and stop drives.
            if (_target == null)
            {
                AllStop();
                return;
            }

            // Compute distance once — no allocation (Vector3 subtract = stack struct).
            float dist = Vector3.Distance(transform.position, _target.position);

            TickState(dist);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_attackRange >= _approachRange)
                Debug.LogWarning(
                    $"[RobotFSM] '{name}': AttackRange ({_attackRange}) must be less than " +
                    $"ApproachRange ({_approachRange}). Check Inspector values.");
        }
#endif

        // ── FSM Tick ──────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the current state, drives joints, and transitions as needed.
        /// All locals are value types — zero heap allocations.
        /// </summary>
        private void TickState(float dist)
        {
            // Apply difficulty scaling — all float ops, no allocation.
            // Range formula: effectiveRange = base × (0.5 + aggressionScale).
            // At aggressionScale=0.5 (Medium default) the multiplier is 1.0 (identity).
            float aggrMult       = _difficulty != null ? (0.5f + _difficulty.AiAggressionScale) : 1f;
            float approachRange  = _approachRange * aggrMult;
            float attackRange    = _attackRange   * aggrMult;
            float driveSpeed     = _driveSpeedRadPerSec * (_difficulty != null ? _difficulty.AiDriveSpeedScale : 1f);

            switch (_state)
            {
                // ── Idle ──────────────────────────────────────────────────────
                case RobotAIState.Idle:
                    AllStop();

                    if (dist < approachRange)
                        TransitionTo(RobotAIState.Approach);
                    break;

                // ── Approach ──────────────────────────────────────────────────
                case RobotAIState.Approach:
                    DriveTowardTarget(driveSpeed);
                    _weaponJoint?.SetTargetVelocity(_idleWeaponSpeedRadPerSec);

                    if (dist < attackRange)
                        TransitionTo(RobotAIState.Attack);
                    else if (dist > approachRange * 1.25f) // hysteresis band
                        TransitionTo(RobotAIState.Idle);
                    break;

                // ── Attack ────────────────────────────────────────────────────
                case RobotAIState.Attack:
                    DriveTowardTarget(driveSpeed);
                    _weaponJoint?.SetTargetVelocity(_attackWeaponSpeedRadPerSec);

                    if (dist > attackRange * 1.5f) // hysteresis band
                        TransitionTo(RobotAIState.Approach);
                    break;
            }
        }

        // ── Drive helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Differential steering: compute left/right wheel velocities to steer toward target.
        /// <paramref name="driveSpeed"/> is the pre-scaled effective drive speed (rad/s).
        /// All arithmetic uses stack-allocated value types — no heap allocations.
        /// </summary>
        private void DriveTowardTarget(float driveSpeed)
        {
            // Project the direction-to-target onto the robot's local forward and right axes.
            Vector3 toTarget = _target.position - transform.position; // no normalise needed for Dot
            float   fwdDot   = Vector3.Dot(transform.forward, toTarget.normalized); //  -1..1
            float   rightDot = Vector3.Dot(transform.right,   toTarget.normalized); //  -1..1 (steering)

            // Base speed proportional to how well we're facing the target.
            // clamp fwdDot so we always drive forward even when strafed.
            float baseSpeed  = Mathf.Max(0.2f, fwdDot) * driveSpeed;
            float steerDelta = rightDot * _steeringGain * driveSpeed;

            float leftVel  = baseSpeed - steerDelta;
            float rightVel = baseSpeed + steerDelta;

            _leftWheel?.SetTargetVelocity(leftVel);
            _rightWheel?.SetTargetVelocity(rightVel);
        }

        /// <summary>Stops all drive joints immediately.</summary>
        private void AllStop()
        {
            _leftWheel?.SetTargetVelocity(0f);
            _rightWheel?.SetTargetVelocity(0f);
            _weaponJoint?.SetTargetVelocity(0f);
        }

        // ── State transitions ─────────────────────────────────────────────────

        private void TransitionTo(RobotAIState newState)
        {
            if (_state == newState) return;

            _state = newState;

            switch (newState)
            {
                case RobotAIState.Idle:
                    _onEnterIdle?.Raise();
                    break;
                case RobotAIState.Approach:
                    _onEnterApproach?.Raise();
                    break;
                case RobotAIState.Attack:
                    _onEnterAttack?.Raise();
                    break;
            }
        }
    }
}

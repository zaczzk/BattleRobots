using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Wraps an <see cref="ArticulationBody"/> as a single-axis revolute hinge.
    ///
    /// Usage:
    ///   1. Attach to a child GameObject that has (or will have) an ArticulationBody.
    ///   2. Set limits and max torque in the Inspector.
    ///   3. Call <see cref="SetTargetVelocity"/> for velocity-driven control,
    ///      or <see cref="SetTargetAngle"/> for position-driven control.
    ///
    /// Architecture rules observed:
    ///   - ArticulationBody only; no Rigidbody.
    ///   - Drive structs are written via local copies (stack) — no heap allocs in hot path.
    ///   - No new allocations in Update / FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    [DisallowMultipleComponent]
    public sealed class HingeJointAB : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Hinge Limits (degrees)")]
        [Tooltip("Lower angular limit of the revolute joint in degrees.")]
        [SerializeField, Range(-180f, 0f)] private float _lowerLimit = -90f;

        [Tooltip("Upper angular limit of the revolute joint in degrees.")]
        [SerializeField, Range(0f, 180f)]  private float _upperLimit =  90f;

        [Header("Drive Parameters")]
        [Tooltip("Maximum force (N·m) the drive may apply. 0 = unlimited.")]
        [SerializeField, Min(0f)] private float _maxTorque = 200f;

        [Tooltip("Velocity damping coefficient. Higher values reduce oscillation.")]
        [SerializeField, Min(0f)] private float _damping = 10f;

        [Tooltip("Positional stiffness. Set to 0 for pure velocity drive; >0 for position drive.")]
        [SerializeField, Min(0f)] private float _stiffness = 0f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private ArticulationBody _ab;

        // Effective torque cap for this match session (base + any bonus applied at spawn).
        // Initialised in ConfigureJoint; modified by ApplyTorqueBonus.
        private float _effectiveTorque;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _ab = GetComponent<ArticulationBody>();
            ConfigureJoint();
        }

#if UNITY_EDITOR
        // Reapply configuration when Inspector values change in Edit mode.
        private void OnValidate()
        {
            if (_ab == null)
                _ab = GetComponent<ArticulationBody>();
            if (_ab != null)
                ConfigureJoint();
        }
#endif

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the drive's target angular velocity (rad/s).
        /// Call from a controller's FixedUpdate — no heap allocs.
        /// </summary>
        /// <param name="velocityRadPerSec">Positive = counter-clockwise around the X axis.</param>
        public void SetTargetVelocity(float velocityRadPerSec)
        {
            ArticulationDrive drive = _ab.xDrive;   // struct copy — stack only
            drive.targetVelocity = velocityRadPerSec;
            _ab.xDrive = drive;
        }

        /// <summary>
        /// Sets the drive's target angle (degrees). Requires <see cref="_stiffness"/> > 0.
        /// Angle is clamped to [<see cref="_lowerLimit"/>, <see cref="_upperLimit"/>].
        /// </summary>
        public void SetTargetAngle(float angleDegrees)
        {
            ArticulationDrive drive = _ab.xDrive;
            drive.target = Mathf.Clamp(angleDegrees, _lowerLimit, _upperLimit);
            _ab.xDrive = drive;
        }

        /// <summary>
        /// Adds <paramref name="bonusTorque"/> to the drive's force limit for this match.
        /// Called once by <c>RobotSpawner</c> after spawn — not in the hot path.
        /// </summary>
        /// <param name="bonusTorque">Additive N·m bonus from equipped parts (must be ≥ 0).</param>
        public void ApplyTorqueBonus(float bonusTorque)
        {
            if (bonusTorque <= 0f) return;

            _effectiveTorque += bonusTorque;

            // Patch only the force limit — leaves all other drive settings intact.
            ArticulationDrive drive = _ab.xDrive;
            drive.forceLimit  = _effectiveTorque;
            _ab.xDrive = drive;
        }

        /// <summary>Current hinge angle in degrees (read from ArticulationBody joint position).</summary>
        public float CurrentAngleDegrees =>
            _ab.jointPosition.dofCount > 0
                ? _ab.jointPosition[0] * Mathf.Rad2Deg
                : 0f;

        // ── Private ───────────────────────────────────────────────────────────

        private void ConfigureJoint()
        {
            // Lock all DOFs except the revolute axis (X rotation).
            _ab.jointType = ArticulationJointType.RevoluteJoint;

            _ab.linearLockX = ArticulationDofLock.LockedMotion;
            _ab.linearLockY = ArticulationDofLock.LockedMotion;
            _ab.linearLockZ = ArticulationDofLock.LockedMotion;
            _ab.twistLock   = ArticulationDofLock.LimitedMotion;
            _ab.swingYLock  = ArticulationDofLock.LockedMotion;
            _ab.swingZLock  = ArticulationDofLock.LockedMotion;

            // Apply drive parameters.
            ArticulationDrive drive = _ab.xDrive;
            drive.lowerLimit  = _lowerLimit;
            drive.upperLimit  = _upperLimit;
            _effectiveTorque  = _maxTorque > 0f ? _maxTorque : float.MaxValue;
            drive.forceLimit  = _effectiveTorque;
            drive.damping     = _damping;
            drive.stiffness   = _stiffness;
            // Drive mode: stiffness=0 → velocity drive (caller sets targetVelocity);
            //             stiffness>0 → position drive (caller calls SetTargetAngle).
            _ab.xDrive = drive;
        }
    }
}

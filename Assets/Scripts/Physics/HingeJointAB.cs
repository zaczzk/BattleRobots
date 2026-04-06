using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Configures an ArticulationBody as a motorised revolute (hinge) joint.
    ///
    /// ARCHITECTURE RULES enforced here:
    ///   • ArticulationBody only — never Rigidbody.
    ///   • No heap allocations in FixedUpdate; drive structs are value-copied on the stack.
    ///   • SetTargetVelocity / ApplyTorque are the only runtime entry points.
    ///
    /// Attach this to the same GameObject as an ArticulationBody.
    /// The parent chain must have its root ArticulationBody marked isRoot = true (Unity handles this).
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    [DisallowMultipleComponent]
    public sealed class HingeJointAB : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Drive")]
        [Tooltip("Proportional gain. 0 = velocity-only control.")]
        [SerializeField, Min(0f)] private float _stiffness = 0f;

        [Tooltip("Damping coefficient. Higher values resist motion more.")]
        [SerializeField, Min(0f)] private float _damping = 100f;

        [Tooltip("Maximum force (N) the drive may apply per physics step.")]
        [SerializeField, Min(0f)] private float _forceLimit = 2000f;

        [Header("Limits")]
        [Tooltip("Constrain rotation to [lowerLimit, upperLimit] degrees.")]
        [SerializeField] private bool _useLimits = false;

        [SerializeField] private float _lowerLimit = -90f;
        [SerializeField] private float _upperLimit =  90f;

        // ── Private state ─────────────────────────────────────────────────────

        private ArticulationBody _ab;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _ab = GetComponent<ArticulationBody>();
            ApplyJointConfiguration();
        }

        // ── Configuration ──────────────────────────────────────────────────────

        /// <summary>
        /// Writes joint type, DOF lock, and drive parameters to the ArticulationBody.
        /// Safe to call from Awake or from the Editor via OnValidate.
        /// </summary>
        private void ApplyJointConfiguration()
        {
            _ab.jointType = ArticulationJointType.RevoluteJoint;

            _ab.twistLock = _useLimits
                ? ArticulationDofLock.LimitedMotion
                : ArticulationDofLock.FreeMotion;

            // Build a base drive; limits are baked in if enabled.
            var drive = new ArticulationDrive
            {
                stiffness    = _stiffness,
                damping      = _damping,
                forceLimit   = _forceLimit,
                lowerLimit   = _lowerLimit,
                upperLimit   = _upperLimit,
                driveType    = ArticulationDriveType.Force
            };

            // RevoluteJoint drives on the X (twist) axis.
            _ab.xDrive = drive;
        }

        // ── Runtime API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the velocity target on the hinge drive (degrees per second).
        ///
        /// Call from FixedUpdate only.
        /// No heap allocation — ArticulationDrive is a value type.
        /// </summary>
        public void SetTargetVelocity(float degreesPerSecond)
        {
            ArticulationDrive drive = _ab.xDrive;   // struct copy — no alloc
            drive.targetVelocity   = degreesPerSecond;
            _ab.xDrive             = drive;
        }

        /// <summary>
        /// Sets the position target on the hinge drive (degrees, within limits).
        ///
        /// Call from FixedUpdate only. No allocation.
        /// </summary>
        public void SetTargetAngle(float degrees)
        {
            ArticulationDrive drive = _ab.xDrive;
            drive.target           = degrees;
            _ab.xDrive             = drive;
        }

        /// <summary>
        /// Applies a continuous torque along the hinge axis using ArticulationBody.AddTorque.
        /// Uses ForceMode.Force — scale by Time.fixedDeltaTime if calling each frame.
        ///
        /// No Rigidbody involved.
        /// </summary>
        public void ApplyTorque(float torqueNm)
        {
            _ab.AddTorque(transform.right * torqueNm, ForceMode.Force);
        }

        /// <summary>
        /// Reports the current joint angle in degrees (read from xDrive.target reflects
        /// the set point; for measured angle use jointPosition[0] * Mathf.Rad2Deg).
        /// </summary>
        public float GetMeasuredAngleDeg()
        {
            return _ab.jointPosition.dofCount > 0
                ? _ab.jointPosition[0] * Mathf.Rad2Deg
                : 0f;
        }

        // ── Editor helpers ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_lowerLimit > _upperLimit)
            {
                Debug.LogWarning(
                    $"[HingeJointAB] '{name}': lowerLimit ({_lowerLimit}°) > upperLimit ({_upperLimit}°). " +
                    "Swap the values to avoid physics misbehaviour.");
            }
        }
#endif
    }
}

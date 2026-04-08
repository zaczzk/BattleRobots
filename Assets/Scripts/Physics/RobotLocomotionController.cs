using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Tank-style locomotion for a robot whose chassis root is an ArticulationBody.
    ///
    /// ARCHITECTURE RULES enforced here:
    ///   • ArticulationBody only — never Rigidbody.
    ///   • No heap allocations in FixedUpdate — only struct / value-type operations.
    ///   • Player input read in FixedUpdate via Input.GetAxis (allocation-free).
    ///   • AI or network code pushes inputs via SetInputs() when _isPlayerControlled is false.
    ///
    /// Attach to the root GameObject of a robot articulation chain.
    /// The ArticulationBody on this GameObject must be the root of the chain
    /// (Unity automatically assigns isRoot = true to it).
    /// </summary>
    [RequireComponent(typeof(ArticulationBody))]
    [DisallowMultipleComponent]
    public sealed class RobotLocomotionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("Root ArticulationBody of the robot chassis. Auto-fetched from this GameObject if left null.")]
        [SerializeField] private ArticulationBody _rootBody;

        [Header("Tuning")]
        [Tooltip("Maximum linear speed in metres per second.")]
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;

        [Tooltip("Maximum turn speed in degrees per second.")]
        [SerializeField, Min(0f)] private float _turnSpeed = 120f;

        [Header("Control Mode")]
        [Tooltip("When true, reads Unity Input axes each FixedUpdate. " +
                 "Set false for AI / network controlled robots.")]
        [SerializeField] private bool _isPlayerControlled = true;

        // ── Public input state ─────────────────────────────────────────────────
        // Range −1..1.  Written by AI/network; read in FixedUpdate.
        // No properties — public fields are intentional to keep FixedUpdate allocation-free.

        /// <summary>Forward/backward input in the range [−1, 1].</summary>
        public float MoveInput;

        /// <summary>Left/right steering input in the range [−1, 1]. Positive = turn right.</summary>
        public float TurnInput;

        // ── Cached axis names (avoid string literals in FixedUpdate) ───────────
        private const string k_AxisVertical   = "Vertical";
        private const string k_AxisHorizontal = "Horizontal";

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_rootBody == null)
                _rootBody = GetComponent<ArticulationBody>();
        }

        private void FixedUpdate()
        {
            if (_isPlayerControlled)
            {
                // Input.GetAxis is allocation-free — returns a float from native side.
                MoveInput = Input.GetAxis(k_AxisVertical);
                TurnInput = Input.GetAxis(k_AxisHorizontal);
            }

            ApplyLocomotion(MoveInput, TurnInput);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplyLocomotion(float move, float turn)
        {
            // Linear velocity along local-forward — value-type vector ops, no alloc.
            _rootBody.linearVelocity = transform.forward * (move * _moveSpeed);

            // Angular velocity around world-up; convert deg/s → rad/s.
            float turnRad = turn * (_turnSpeed * Mathf.Deg2Rad);
            _rootBody.angularVelocity = new Vector3(0f, turnRad, 0f);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Push normalised [−1, 1] inputs from external code (AI, network, tests).
        /// Inputs are clamped to [-1, 1].  Only effective when
        /// <see cref="_isPlayerControlled"/> is false.
        /// </summary>
        public void SetInputs(float move, float turn)
        {
            MoveInput = Mathf.Clamp(move, -1f, 1f);
            TurnInput = Mathf.Clamp(turn, -1f, 1f);
        }

        /// <summary>
        /// Immediately zero all velocity and clear inputs.
        /// Call when the robot is destroyed, stunned, or the match ends.
        /// </summary>
        public void Halt()
        {
            _rootBody.linearVelocity  = Vector3.zero;
            _rootBody.angularVelocity = Vector3.zero;
            MoveInput = 0f;
            TurnInput = 0f;
        }

        /// <summary>Expose speed for UI / debug without referencing ArticulationBody directly.</summary>
        public float CurrentSpeedMs => _rootBody != null ? _rootBody.linearVelocity.magnitude : 0f;
    }
}

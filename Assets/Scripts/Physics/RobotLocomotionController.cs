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

        // ── Speed fields (set at match start; inspector values are preserved) ────
        // _runtimeMoveSpeed: base-speed override from CombatStatsApplicator (-1 = use inspector).
        // _speedMultiplier:  fractional modifier from BotDifficultyConfig (1 = no change).
        // _statusSlowFactor: runtime slow multiplier from StatusEffectController (1 = no slowdown).
        private float _runtimeMoveSpeed = -1f;
        private float _speedMultiplier  = 1f;
        private float _statusSlowFactor = 1f;

        // ── Stun override (set by StatusEffectController) ─────────────────────
        // When true, FixedUpdate zeros velocities and skips ApplyLocomotion so
        // neither player input nor AI SetInputs calls have any locomotion effect.
        private bool _isStunned;

        // ── Effective base speed (respects runtime override) ─────────────────
        // Not a hot-path property — only referenced inside ApplyLocomotion (cold path).
        private float EffectiveMoveSpeed => _runtimeMoveSpeed > 0f ? _runtimeMoveSpeed : _moveSpeed;

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
            // While stunned: freeze in place and suppress all inputs.
            // StatusEffectController sets _isStunned via SetStunned() each FixedUpdate
            // tick so this state is always current.
            if (_isStunned)
            {
                if (_rootBody != null)
                {
                    _rootBody.linearVelocity  = Vector3.zero;
                    _rootBody.angularVelocity = Vector3.zero;
                }
                return;
            }

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
            // Three independent multipliers stack: base speed × difficulty × status slow.
            _rootBody.linearVelocity = transform.forward *
                (move * EffectiveMoveSpeed * _speedMultiplier * _statusSlowFactor);

            // Angular velocity around world-up; convert deg/s → rad/s.
            // Status slow also reduces turn speed proportionally.
            float turnRad = turn * (_turnSpeed * _speedMultiplier * _statusSlowFactor * Mathf.Deg2Rad);
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

        /// <summary>
        /// Overrides the base move speed for this match session.
        /// Intended to be called by <c>CombatStatsApplicator</c> with
        /// <c>RobotCombatStats.EffectiveSpeed</c> after parts are assembled.
        /// Stored separately so the inspector's <c>_moveSpeed</c> value is preserved;
        /// calling multiple times sets (not compounds) the override.
        /// Values below 0.01 are clamped to 0.01. Allocation-free.
        /// </summary>
        public void SetBaseSpeed(float speed)
        {
            _runtimeMoveSpeed = Mathf.Max(0.01f, speed);
        }

        /// <summary>
        /// Applies a one-time speed multiplier from a <c>BotDifficultyConfig</c> SO.
        /// Stores the value separately so the inspector's base speeds are preserved;
        /// calling this method multiple times sets (not compounds) the multiplier.
        /// Allocation-free — pure field write.
        /// </summary>
        /// <param name="multiplier">Values &lt; 0.01 are clamped to 0.01 to prevent zeroing motion.</param>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.01f, multiplier);
        }

        /// <summary>
        /// Enables or disables the stun override applied by <c>StatusEffectController</c>.
        /// While stunned, <see cref="FixedUpdate"/> zeros all velocities and skips
        /// locomotion so neither player input nor AI <c>SetInputs</c> calls move the robot.
        /// Called every FixedUpdate tick by <c>StatusEffectController</c> — resets to
        /// false automatically when the Stun effect expires.
        /// Allocation-free — pure field write.
        /// </summary>
        public void SetStunned(bool stunned)
        {
            _isStunned = stunned;
        }

        /// <summary>
        /// Applies a speed multiplier from an active <c>StatusEffectType.Slow</c> effect.
        /// Stored separately from the difficulty multiplier so both stack correctly
        /// (difficulty × status slow). Calling this method multiple times sets
        /// (not compounds) the slow factor.
        /// Called every FixedUpdate tick by <c>StatusEffectController</c> — resets to
        /// 1.0 automatically when the Slow effect expires.
        /// </summary>
        /// <param name="factor">Clamped to [0.01, 1]. 1.0 = no slowdown.</param>
        public void SetSlowFactor(float factor)
        {
            _statusSlowFactor = Mathf.Clamp(factor, 0.01f, 1f);
        }

        /// <summary>
        /// Effective base move speed (runtime override if set, inspector value otherwise).
        /// Used by <c>CombatStatsApplicatorTests</c> to verify stat application.
        /// </summary>
        public float BaseSpeed => EffectiveMoveSpeed;

        /// <summary>Expose speed for UI / debug without referencing ArticulationBody directly.</summary>
        public float CurrentSpeedMs => _rootBody != null ? _rootBody.linearVelocity.magnitude : 0f;
    }
}

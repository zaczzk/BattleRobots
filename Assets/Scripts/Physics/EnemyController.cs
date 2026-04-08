using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Three-state AI controller for enemy robots: Idle → Chase → Attack.
    ///
    /// ── State machine ─────────────────────────────────────────────────────────
    ///   Idle    — robot is stationary; entered at spawn and after MatchEnded.
    ///   Chase   — root body steered toward the player via AddForce + yaw torque.
    ///   Attack  — weapon HingeJointABs spun at max velocity; locomotion paused.
    ///   Transitions: MatchStarted → Chase; within attackRange → Attack;
    ///                outside attackRange → Chase; MatchEnded → Idle.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   - ArticulationBody root locomotion via AddForce/AddTorque — no Rigidbody.
    ///   - Zero heap allocation in FixedUpdate: only float/Vector3 math + enum compare.
    ///   - EnemyBrainSO configures parameters; immutable at runtime.
    ///   - Cross-component signals via VoidGameEvent SO channels (MatchStarted/Ended).
    ///   - BattleRobots.Physics namespace; references Core only for SO event types.
    ///
    /// Scene wiring:
    ///   1. Add EnemyController to the root of the enemy robot hierarchy.
    ///   2. Assign _brain (EnemyBrainSO), _rootBody (root ArticulationBody),
    ///      _weaponJoints (HingeJointABs on spinners/blades), and _playerRoot.
    ///   3. Assign _onMatchStarted and _onMatchEnded SO channels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyController : MonoBehaviour
    {
        // ── State machine ─────────────────────────────────────────────────────

        private enum AIState { Idle, Chase, Attack }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Brain Config")]
        [Tooltip("SO defining chase force, turn torque, attack range, and weapon speed.")]
        [SerializeField] private EnemyBrainSO _brain;

        [Header("Locomotion")]
        [Tooltip("Root ArticulationBody of this robot. AddForce/AddTorque move it during Chase.")]
        [SerializeField] private ArticulationBody _rootBody;

        [Header("Weapons")]
        [Tooltip("HingeJointABs driven at weapon spin speed during Attack state.")]
        [SerializeField] private HingeJointAB[] _weaponJoints;

        [Header("Target")]
        [Tooltip("Transform of the player robot root. Used to compute direction and distance.")]
        [SerializeField] private Transform _playerRoot;

        [Header("Match Gate — Event Channels")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private AIState _state = AIState.Idle;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(HandleMatchStarted);
            _onMatchEnded?.RegisterCallback(HandleMatchEnded);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(HandleMatchStarted);
            _onMatchEnded?.UnregisterCallback(HandleMatchEnded);
        }

        private void FixedUpdate()
        {
            if (_state == AIState.Idle) return;
            if (_rootBody == null || _playerRoot == null || _brain == null) return;

            // ── Compute direction and distance ────────────────────────────────

            Vector3 toPlayer = _playerRoot.position - _rootBody.transform.position;
            toPlayer.y = 0f;                                     // project onto horizontal plane

            // Compute magnitude once; derive squared distance without a second sqrt.
            float dist      = toPlayer.magnitude;
            float distSq    = dist * dist;
            float rangeSq   = _brain.AttackRange * _brain.AttackRange;

            // ── State transitions ─────────────────────────────────────────────

            if (_state == AIState.Chase && distSq <= rangeSq)
                _state = AIState.Attack;
            else if (_state == AIState.Attack && distSq > rangeSq)
                _state = AIState.Chase;

            // ── Chase: steer toward player ────────────────────────────────────

            if (_state == AIState.Chase)
            {
                // Normalise direction; guard against zero-length vector.
                Vector3 dir = dist > 0.001f ? toPlayer / dist : Vector3.zero;

                // Apply forward force — all value-type arithmetic, zero alloc.
                _rootBody.AddForce(dir * _brain.ChaseForce, ForceMode.Force);

                // Yaw correction: Y component of (forward × dir) gives signed rotation error.
                // Positive → target is to the right → apply positive Y torque to turn right.
                Vector3 fwd   = transform.forward;
                float   cross = fwd.x * dir.z - fwd.z * dir.x;
                _rootBody.AddTorque(new Vector3(0f, cross * _brain.TurnTorque, 0f), ForceMode.Force);
            }

            // ── Attack: spin weapon joints ────────────────────────────────────

            if (_state == AIState.Attack && _weaponJoints != null)
            {
                float spinSpeed = _brain.WeaponSpinSpeedDegPerSec;
                for (int i = 0; i < _weaponJoints.Length; i++)
                    _weaponJoints[i]?.SetTargetVelocity(spinSpeed);
            }
        }

        // ── Event callbacks ───────────────────────────────────────────────────

        private void HandleMatchStarted() => _state = AIState.Chase;

        private void HandleMatchEnded()
        {
            _state = AIState.Idle;

            // Stop weapon joints gracefully on match end.
            if (_weaponJoints == null) return;
            for (int i = 0; i < _weaponJoints.Length; i++)
                _weaponJoints[i]?.SetTargetVelocity(0f);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_brain == null)
                Debug.LogWarning($"[EnemyController] '{name}': _brain EnemyBrainSO not assigned.");
            if (_rootBody == null)
                Debug.LogWarning($"[EnemyController] '{name}': _rootBody ArticulationBody not assigned.");
            if (_playerRoot == null)
                Debug.LogWarning($"[EnemyController] '{name}': _playerRoot Transform not assigned.");
        }
#endif
    }
}

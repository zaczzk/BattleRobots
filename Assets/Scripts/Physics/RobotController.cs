using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Translates player input (keyboard WASD / gamepad left-stick) into differential
    /// steering commands for the two drive <see cref="HingeJointAB"/> wheel joints,
    /// and optionally spins a weapon joint on a separate input axis.
    ///
    /// Architecture rules observed:
    ///   - Namespace <c>BattleRobots.Physics</c>; references Core only.
    ///   - ArticulationBody only — drives via HingeJointAB which wraps AB.
    ///   - Zero heap allocations in FixedUpdate (Input.GetAxis returns float; all locals
    ///     are value types; no LINQ, no delegate creation per frame).
    ///   - Control disabled automatically when the robot's HealthSO reports it dead.
    ///
    /// Setup:
    ///   1. Attach to the robot root GameObject.
    ///   2. Assign <c>_leftWheel</c>, <c>_rightWheel</c> HingeJointAB references.
    ///   3. (Optional) Assign <c>_weaponJoint</c> and set <c>_weaponSpeedRadPerSec</c>.
    ///   4. (Optional) Assign <c>_ownHealth</c> to disable input on death.
    ///   5. Tune <c>_driveSpeedRadPerSec</c> and <c>_steeringGain</c>.
    ///
    /// Input axes (Unity's legacy Input Manager):
    ///   Vertical   → forward / reverse  (W/S or gamepad left-stick Y)
    ///   Horizontal → turning            (A/D or gamepad left-stick X)
    ///   Fire1      → weapon spin        (Space / gamepad right trigger)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Drive Joints")]
        [Tooltip("Left wheel / track HingeJointAB.")]
        [SerializeField] private HingeJointAB _leftWheel;

        [Tooltip("Right wheel / track HingeJointAB.")]
        [SerializeField] private HingeJointAB _rightWheel;

        [Header("Weapon Joint (optional)")]
        [Tooltip("Weapon spinner HingeJointAB. Leave empty if robot has no active weapon.")]
        [SerializeField] private HingeJointAB _weaponJoint;

        [Tooltip("Angular velocity (rad/s) applied to the weapon while Fire1 is held.")]
        [SerializeField, Min(0f)] private float _weaponSpeedRadPerSec = 20f;

        [Header("Drive Parameters")]
        [Tooltip("Max wheel speed in rad/s at full input.")]
        [SerializeField, Min(0f)] private float _driveSpeedRadPerSec = 15f;

        [Tooltip("Fraction of drive speed added/subtracted per unit of horizontal input to turn.")]
        [SerializeField, Range(0f, 2f)] private float _steeringGain = 0.8f;

        [Header("Health (optional)")]
        [Tooltip("When assigned, controller disables itself when IsAlive is false.")]
        [SerializeField] private HealthSO _ownHealth;

        [Header("Settings (optional)")]
        [Tooltip("When assigned, the InvertControls flag is read each FixedUpdate to flip the vertical axis.")]
        [SerializeField] private SettingsSO _settings;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void FixedUpdate()
        {
            // Disable control if robot is dead.
            if (_ownHealth != null && !_ownHealth.IsAlive)
            {
                AllStop();
                return;
            }

            // Read axes — float returns; zero GC.
            float fwd  = Input.GetAxis("Vertical");      //  1 = forward, -1 = reverse
            float turn = Input.GetAxis("Horizontal");    //  1 = right,   -1 = left

            // Invert vertical axis when the player has opted in via SettingsSO.
            if (_settings != null && _settings.InvertControls)
                fwd = -fwd;

            ApplyDifferentialSteering(fwd, turn);
            UpdateWeapon();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Converts normalised forward/turn inputs into left-right wheel velocities.
        /// Pure value-type arithmetic — no allocations.
        /// </summary>
        private void ApplyDifferentialSteering(float fwd, float turn)
        {
            // Scale base speed by the forward input.
            float baseVel   = fwd * _driveSpeedRadPerSec;
            // Steering delta: positive turn → right side faster → robot turns left.
            float steerDelta = turn * _steeringGain * _driveSpeedRadPerSec;

            float leftVel  = baseVel + steerDelta;
            float rightVel = baseVel - steerDelta;

            _leftWheel?.SetTargetVelocity(leftVel);
            _rightWheel?.SetTargetVelocity(rightVel);
        }

        /// <summary>
        /// Spins the weapon at full speed while Fire1 is held; brakes when released.
        /// No allocations — Input.GetButton returns bool.
        /// </summary>
        private void UpdateWeapon()
        {
            if (_weaponJoint == null) return;

            if (Input.GetButton("Fire1"))
                _weaponJoint.SetTargetVelocity(_weaponSpeedRadPerSec);
            else
                _weaponJoint.SetTargetVelocity(0f);
        }

        /// <summary>Zeroes all joint velocities — called when dead or disabled.</summary>
        private void AllStop()
        {
            _leftWheel?.SetTargetVelocity(0f);
            _rightWheel?.SetTargetVelocity(0f);
            _weaponJoint?.SetTargetVelocity(0f);
        }

        private void OnDisable()
        {
            AllStop();
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Smooth-follow camera rig that tracks a target Transform.
    ///
    /// Position: follows the target with a configurable world-space offset,
    ///           smoothed via <see cref="Vector3.SmoothDamp"/>.
    /// Rotation: smoothly rotates to face the target by interpolating the
    ///           camera's forward direction with <see cref="Vector3.SmoothDamp"/>.
    ///
    /// ARCHITECTURE RULES enforced here:
    ///   • No heap allocations in LateUpdate —
    ///     Vector3.SmoothDamp and Quaternion.LookRotation are value-type operations.
    ///   • No BattleRobots.Physics or BattleRobots.UI references.
    ///   • Target assigned at runtime via <see cref="SetTarget"/> or Inspector.
    ///
    /// Place on the main Camera (or a rig parent). Call <see cref="SnapToTarget"/>
    /// after scene load to prevent a lerp-in from the world origin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraRig : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Target")]
        [Tooltip("Transform to follow. Assign the player robot's root transform.")]
        [SerializeField] private Transform _target;

        [Tooltip("Offset from the target expressed in the target's local space. " +
                 "Positive Z = behind target; positive Y = above.")]
        [SerializeField] private Vector3 _localOffset = new Vector3(0f, 4f, -8f);

        [Header("Position Smoothing")]
        [Tooltip("Approximate time in seconds to reach the desired position. " +
                 "Lower = snappier; higher = floatier.")]
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.15f;

        [Header("Rotation Smoothing")]
        [Tooltip("When true, the rig continuously looks at the target.")]
        [SerializeField] private bool _lookAtTarget = true;

        [Tooltip("Approximate time in seconds to rotate toward the target. " +
                 "Lower = snappier; higher = laggy-cinematic.")]
        [SerializeField, Min(0.01f)] private float _rotationSmoothTime = 0.1f;

        // ── Private state (value types — no alloc) ────────────────────────────

        private Vector3 _posVelocity;   // ref param for position SmoothDamp
        private Vector3 _lookVelocity;  // ref param for look-direction SmoothDamp

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_target == null) return;

            // ── Position ──────────────────────────────────────────────────────
            // Convert local offset to world-space desired position.
            // TransformPoint is a pure matrix multiply — no allocation.
            Vector3 desiredPos = _target.TransformPoint(_localOffset);
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref _posVelocity, _positionSmoothTime);

            // ── Rotation ──────────────────────────────────────────────────────
            if (_lookAtTarget)
            {
                // Smoothly interpolate the camera's forward direction toward the target.
                // Vector3.SmoothDamp on a direction vector is zero-alloc.
                Vector3 targetDir   = (_target.position - transform.position);
                if (targetDir.sqrMagnitude < 0.0001f) return;   // coincident — skip

                targetDir.Normalize();  // value-type normalise — no alloc
                Vector3 smoothDir = Vector3.SmoothDamp(
                    transform.forward, targetDir, ref _lookVelocity, _rotationSmoothTime);

                if (smoothDir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(smoothDir);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Switch to a new target (e.g., spectating a different robot after death).
        /// </summary>
        public void SetTarget(Transform target) => _target = target;

        /// <summary>
        /// Immediately snap position and rotation to the desired follow pose,
        /// bypassing all smoothing.  Call this after loading a new scene to prevent
        /// the camera lerping in from the world origin.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            transform.position = _target.TransformPoint(_localOffset);

            if (_lookAtTarget)
            {
                Vector3 dir = _target.position - transform.position;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            // Zero the SmoothDamp velocity accumulators so there's no residual drift.
            _posVelocity  = Vector3.zero;
            _lookVelocity = Vector3.zero;
        }
    }
}

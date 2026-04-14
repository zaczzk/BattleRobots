using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Listens to <see cref="RobotRespawnSO._onRespawnReady"/> and teleports
    /// <see cref="_robotTransform"/> to the nearest <see cref="_respawnAnchors"/>
    /// entry when a respawn cooldown expires.
    ///
    /// ── Teleport strategy ────────────────────────────────────────────────────
    ///   1. If <c>_respawnAnchors</c> is null or empty, teleport to this
    ///      GameObject's own position (acts as a single unnamed anchor).
    ///   2. Otherwise find the anchor with the smallest squared-distance to the
    ///      robot's current position and teleport there.
    ///   Null entries in <c>_respawnAnchors</c> are skipped silently.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Delegate cached in Awake — zero alloc on subscribe / unsubscribe.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - For ArticulationBody robots: wire a VoidGameEventListener to also call
    ///     ArticulationBody.TeleportRoot() so the physics body stays in sync
    ///     with the Transform teleport performed here.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this MB to any persistent in-match GameObject.
    ///   2. Assign <c>_onRespawnReady</c> — the same VoidGameEvent wired to
    ///      <see cref="RobotRespawnSO._onRespawnReady"/>.
    ///   3. Assign <c>_robotTransform</c> — the robot's root Transform.
    ///   4. Populate <c>_respawnAnchors</c> with scene anchor Transforms.
    ///   5. Optionally assign <c>_zone</c> for Gizmo visualization.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnZoneController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("RespawnZoneSO for Gizmo visualization of anchor radii. " +
                 "Has no effect on gameplay logic.")]
        [SerializeField] private RespawnZoneSO _zone;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by RobotRespawnSO when the cooldown expires. " +
                 "Triggers the teleport.")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        [Header("Scene References")]
        [Tooltip("Root Transform of the robot to teleport.")]
        [SerializeField] private Transform _robotTransform;

        [Tooltip("Array of possible respawn destination Transforms. " +
                 "The nearest non-null anchor is chosen each time a respawn fires. " +
                 "Leave empty to use this GameObject's own position as the single anchor.")]
        [SerializeField] private Transform[] _respawnAnchors;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _respawnDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _respawnDelegate = OnRespawnReady;
        }

        private void OnEnable()
        {
            _onRespawnReady?.RegisterCallback(_respawnDelegate);
        }

        private void OnDisable()
        {
            _onRespawnReady?.UnregisterCallback(_respawnDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="RespawnZoneSO"/> config. May be null.</summary>
        public RespawnZoneSO Zone => _zone;

        /// <summary>The robot Transform that will be teleported. May be null.</summary>
        public Transform RobotTransform => _robotTransform;

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnRespawnReady()
        {
            if (_robotTransform == null) return;

            Transform anchor = SelectNearestAnchor();
            _robotTransform.position = anchor != null
                ? anchor.position
                : transform.position;
        }

        /// <summary>
        /// Returns the non-null anchor in <see cref="_respawnAnchors"/> with the
        /// smallest squared distance to <see cref="_robotTransform"/>.
        /// Returns <c>null</c> when <c>_respawnAnchors</c> is null, empty, or
        /// contains only null entries.
        /// </summary>
        private Transform SelectNearestAnchor()
        {
            if (_respawnAnchors == null || _respawnAnchors.Length == 0) return null;

            Transform nearest    = null;
            float     nearestSqr = float.MaxValue;

            Vector3 robotPos = _robotTransform != null
                ? _robotTransform.position
                : transform.position;

            foreach (Transform anchor in _respawnAnchors)
            {
                if (anchor == null) continue;

                float sqr = (anchor.position - robotPos).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest    = anchor;
                }
            }

            return nearest;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_respawnAnchors == null) return;
            float radius = _zone != null ? _zone.AnchorRadius : 1f;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            foreach (Transform anchor in _respawnAnchors)
            {
                if (anchor != null)
                    Gizmos.DrawSphere(anchor.position, radius);
            }
        }
#endif
    }
}

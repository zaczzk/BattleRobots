using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Rotates through a set of respawn anchors on every respawn-ready event so that
    /// consecutive respawns land at different positions.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   When <c>_onRespawnReady</c> fires (i.e. the respawn cooldown expires):
    ///   1. Selects the next anchor using <see cref="RespawnZoneRotatorSO.SelectionMode"/>:
    ///      • <see cref="AnchorSelectionMode.RoundRobin"/>: cycles 0 → 1 → … → (n-1) → 0.
    ///      • <see cref="AnchorSelectionMode.Random"/>: picks a random index.
    ///   2. Teleports <see cref="_robotTransform"/> to the selected anchor's position.
    ///      Falls back to this GameObject's own position when <c>_respawnAnchors</c>
    ///      is null or empty, or when the selected anchor is null.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Intended to replace <see cref="RespawnZoneController"/> when rotating-anchor
    ///     behaviour is desired.
    ///   - Delegate cached in Awake — zero alloc on subscribe / unsubscribe.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - DisallowMultipleComponent — one rotator per respawn context.
    ///   - <see cref="SelectNext"/> is public for direct calls and test driving.
    ///
    /// Scene wiring:
    ///   _config         → RespawnZoneRotatorSO (selection mode + Gizmo radius).
    ///   _onRespawnReady → VoidGameEvent raised by RobotRespawnSO when cooldown expires.
    ///   _robotTransform → The robot's root Transform to teleport.
    ///   _respawnAnchors → Array of possible respawn destination Transforms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnZoneRotatorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("Defines anchor selection mode and Gizmo radius. " +
                 "When null defaults to RoundRobin with no Gizmo.")]
        [SerializeField] private RespawnZoneRotatorSO _config;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by RobotRespawnSO when the cooldown expires. " +
                 "Triggers anchor selection and robot teleport.")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        [Header("Scene References")]
        [Tooltip("Root Transform of the robot to teleport.")]
        [SerializeField] private Transform _robotTransform;

        [Tooltip("Array of possible respawn destination Transforms. " +
                 "Leave empty to use this GameObject's own position as the single anchor.")]
        [SerializeField] private Transform[] _respawnAnchors;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int    _currentIndex;
        private Action _respawnDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _respawnDelegate = OnRespawnReady;
            _currentIndex    = 0;
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

        /// <summary>The assigned <see cref="RespawnZoneRotatorSO"/> config. May be null.</summary>
        public RespawnZoneRotatorSO Config => _config;

        /// <summary>The robot Transform that will be teleported. May be null.</summary>
        public Transform RobotTransform => _robotTransform;

        /// <summary>
        /// Zero-based index of the anchor that will be used on the next RoundRobin respawn.
        /// In Random mode this reflects the last randomly chosen index.
        /// </summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>
        /// Selects the next anchor and teleports <see cref="_robotTransform"/> to it.
        /// Exposed as public for direct wiring and test driving.
        /// No-op when <see cref="_robotTransform"/> is null.
        /// Falls back to this GameObject's position when <c>_respawnAnchors</c> is
        /// null or empty, or when the selected anchor entry is null.
        /// </summary>
        public void SelectNext()
        {
            if (_robotTransform == null) return;

            if (_respawnAnchors == null || _respawnAnchors.Length == 0)
            {
                _robotTransform.position = transform.position;
                return;
            }

            Transform anchor = ChooseAnchor();
            _robotTransform.position = anchor != null
                ? anchor.position
                : transform.position;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnRespawnReady()
        {
            SelectNext();
        }

        private Transform ChooseAnchor()
        {
            AnchorSelectionMode mode = _config != null
                ? _config.SelectionMode
                : AnchorSelectionMode.RoundRobin;

            int index;

            if (mode == AnchorSelectionMode.Random)
            {
                index         = UnityEngine.Random.Range(0, _respawnAnchors.Length);
                _currentIndex = index;
            }
            else
            {
                // RoundRobin: use current index, then advance for the next call.
                index         = _currentIndex;
                _currentIndex = (index + 1) % _respawnAnchors.Length;
            }

            return _respawnAnchors[index];
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_respawnAnchors == null) return;
            float radius = _config != null ? _config.AnchorRadius : 1f;
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            foreach (Transform anchor in _respawnAnchors)
            {
                if (anchor != null)
                    Gizmos.DrawSphere(anchor.position, radius);
            }
        }
#endif
    }
}

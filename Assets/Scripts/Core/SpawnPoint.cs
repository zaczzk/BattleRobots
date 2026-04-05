using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Marker MonoBehaviour that designates a robot spawn location in an arena scene.
    ///
    /// Place on an empty GameObject in the Arena scene. The MatchManager reads
    /// <see cref="Position"/> and <see cref="Rotation"/> to teleport robots at round start.
    ///
    /// A Gizmo (colour-coded by <see cref="_teamIndex"/>) is drawn in the Editor so
    /// designers can see placement without entering Play mode.
    ///
    /// Namespace: BattleRobots.Core — no Physics or UI dependency.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("0 = Player, 1 = Opponent. Used to colour the Editor Gizmo and " +
                 "to let MatchManager pick the correct spawn per team.")]
        [SerializeField, Min(0)] private int _teamIndex = 0;

        [Tooltip("Gizmo icon radius in world-space metres.")]
        [SerializeField, Min(0.1f)] private float _gizmoRadius = 0.4f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>World-space spawn position.</summary>
        public Vector3 Position => transform.position;

        /// <summary>World-space spawn orientation.</summary>
        public Quaternion Rotation => transform.rotation;

        /// <summary>Forward direction robots will face when spawned here.</summary>
        public Vector3 Forward => transform.forward;

        /// <summary>Team this spawn point belongs to (0 = player, 1 = opponent, etc.).</summary>
        public int TeamIndex => _teamIndex;

        // ── Gizmo ──────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private static readonly Color[] TeamColours =
        {
            new Color(0.2f, 0.6f, 1f, 0.85f),   // team 0 — blue
            new Color(1f,   0.3f, 0.3f, 0.85f),  // team 1 — red
            new Color(0.3f, 1f,   0.3f, 0.85f),  // team 2 — green
            new Color(1f,   0.8f, 0.2f, 0.85f),  // team 3 — yellow
        };

        private void OnDrawGizmos()
        {
            Color c = _teamIndex < TeamColours.Length
                ? TeamColours[_teamIndex]
                : Color.white;

            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, _gizmoRadius);

            // Draw a forward-facing arrow so spawn orientation is visible.
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            Gizmos.DrawRay(transform.position, transform.forward * (_gizmoRadius * 2.5f));
        }

        private void OnDrawGizmosSelected()
        {
            // Slightly larger wire sphere when selected to aid precision placement.
            Color c = _teamIndex < TeamColours.Length
                ? TeamColours[_teamIndex]
                : Color.white;
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius * 1.5f);
        }
#endif
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Lightweight MonoBehaviour marker placed in a scene at a robot spawn location.
    ///
    /// Purpose:
    ///   - Provides a visible Gizmo so designers can see and drag spawn positions
    ///     in the Unity Scene view.
    ///   - Acts as the authoring source when a designer wants to sync positions
    ///     back into the ArenaConfig SO (tool-assisted; not done at runtime).
    ///
    /// Runtime behaviour:
    ///   ArenaManager reads spawn data from ArenaConfig SO directly, not from this
    ///   component.  This component carries no Update logic and makes no allocations.
    ///
    /// Inspector index must match the corresponding index in ArenaConfig.SpawnPoints
    /// so the Gizmo label matches the spawn slot order.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnPointMarker : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Label displayed in the Scene Gizmo.  Should match ArenaConfig.SpawnPoints[spawnIndex].label.")]
        [SerializeField] private string _label = "Spawn";

        [Tooltip("Zero-based index into ArenaConfig.SpawnPoints this marker corresponds to.")]
        [SerializeField, Min(0)] private int _spawnIndex = 0;

        [Tooltip("Gizmo sphere colour for this marker.")]
        [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 1f, 0.8f);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Human-readable label for this spawn slot.</summary>
        public string Label      => _label;

        /// <summary>Index into ArenaConfig.SpawnPoints.</summary>
        public int    SpawnIndex => _spawnIndex;

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private static readonly Vector3 _gizmoSphereOffset = Vector3.zero;
        private const float GizmoSphereRadius = 0.35f;
        private const float GizmoForwardLength = 1.2f;

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;

            // Sphere at spawn position
            Gizmos.DrawSphere(transform.position + _gizmoSphereOffset, GizmoSphereRadius);

            // Arrow showing facing direction
            Vector3 forward = transform.forward * GizmoForwardLength;
            Gizmos.DrawLine(transform.position, transform.position + forward);
            Gizmos.DrawSphere(transform.position + forward, GizmoSphereRadius * 0.4f);

            // Label above
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (GizmoSphereRadius + 0.25f),
                $"[{_spawnIndex}] {_label}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, GizmoSphereRadius + 0.05f);
        }
#endif
    }
}

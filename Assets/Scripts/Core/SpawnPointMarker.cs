using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that marks a spawn position in the arena scene.
    ///
    /// Placement:
    ///   1. Create an empty GameObject in the Arena scene.
    ///   2. Attach this component and set <see cref="SpawnIndex"/> to match the
    ///      corresponding index in <see cref="ArenaConfig.SpawnPoints"/>.
    ///   3. Position and rotate the GameObject to the desired spawn pose.
    ///
    /// At runtime the component self-registers with <see cref="ArenaManager"/> so
    /// the manager can resolve scene Transforms instead of baked SO positions.
    /// If no ArenaManager is present in the scene, the marker still works as a
    /// visual reference; ArenaManager will fall back to SO data instead.
    ///
    /// Namespace: BattleRobots.Core (pure scene marker — no Physics / UI deps).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnPointMarker : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Index into ArenaConfig.SpawnPoints. Must be unique per arena scene.")]
        [SerializeField, Min(0)] private int _spawnIndex;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Index into <see cref="ArenaConfig.SpawnPoints"/> this marker represents.</summary>
        public int SpawnIndex => _spawnIndex;

        /// <summary>World-space spawn position (this Transform's position).</summary>
        public Vector3 Position => transform.position;

        /// <summary>Spawn rotation (this Transform's rotation).</summary>
        public Quaternion Rotation => transform.rotation;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (ArenaManager.Instance != null)
                ArenaManager.Instance.RegisterMarker(this);
        }

        private void OnDisable()
        {
            if (ArenaManager.Instance != null)
                ArenaManager.Instance.UnregisterMarker(this);
        }

        // ── Editor Gizmos ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private static readonly Color _gizmoColor   = new Color(0.2f, 0.8f, 0.2f, 0.85f);
        private static readonly Color _labelColor   = new Color(0.1f, 0.9f, 0.1f, 1f);
        private const float           _gizmoRadius  = 0.4f;
        private const float           _arrowLength  = 1.2f;

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius);

            // Forward arrow showing facing direction
            Gizmos.DrawLine(transform.position,
                            transform.position + transform.forward * _arrowLength);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoRadius * 0.5f);

            UnityEditor.Handles.color = _labelColor;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (_gizmoRadius + 0.15f),
                $"Spawn [{_spawnIndex}]");
        }
#endif
    }
}

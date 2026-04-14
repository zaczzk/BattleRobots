using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configuration ScriptableObject for a robot respawn zone.
    ///
    /// Paired with a <see cref="RespawnZoneController"/> MonoBehaviour that owns the
    /// scene anchor Transforms; the controller teleports the robot to the nearest
    /// anchor when <see cref="RobotRespawnSO"/> raises <c>_onRespawnReady</c>.
    ///
    /// ── Design notes ──────────────────────────────────────────────────────────
    ///   • <see cref="AnchorRadius"/> is used for editor visualization (Gizmos) of
    ///     the safe landing zone around each anchor — it does not affect gameplay.
    ///   • Assets are immutable at runtime — all mutable state (e.g. which anchor
    ///     is currently active) lives in <see cref="RespawnZoneController"/>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RespawnZone.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/RespawnZone")]
    public sealed class RespawnZoneSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Settings")]
        [Tooltip("Radius (in world units) drawn around each respawn anchor as a Gizmo. " +
                 "Visual only — does not constrain spawn position.")]
        [SerializeField, Min(0f)] private float _anchorRadius = 1f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Visual radius drawn around each respawn anchor in the editor.
        /// Does not affect gameplay.
        /// </summary>
        public float AnchorRadius => _anchorRadius;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_anchorRadius == 0f)
                Debug.LogWarning($"[RespawnZoneSO] '{name}': _anchorRadius is 0 — " +
                                 "the Gizmo will be invisible in the Scene view.");
        }
#endif
    }
}

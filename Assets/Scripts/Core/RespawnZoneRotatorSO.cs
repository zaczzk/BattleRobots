using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Determines how <see cref="RespawnZoneRotatorController"/> selects the next
    /// respawn anchor when a robot respawns.
    /// </summary>
    public enum AnchorSelectionMode
    {
        /// <summary>
        /// Cycles through anchors in a fixed order (0 → 1 → 2 → … → 0).
        /// Guarantees that consecutive respawns always land at a different position
        /// (when more than one anchor exists).
        /// </summary>
        RoundRobin = 0,

        /// <summary>
        /// Picks an anchor at random on every respawn.
        /// Consecutive respawns may land at the same position.
        /// </summary>
        Random = 1,
    }

    /// <summary>
    /// Configuration ScriptableObject for <see cref="RespawnZoneRotatorController"/>.
    /// Defines the anchor selection strategy and the visual radius used for Scene Gizmos.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Assets are immutable at runtime — mutable state (current round-robin index)
    ///     lives entirely in <see cref="RespawnZoneRotatorController"/>.
    ///   - <see cref="AnchorRadius"/> is purely visual (Gizmo); it does not constrain
    ///     the teleport position.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RespawnZoneRotator.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/RespawnZoneRotator")]
    public sealed class RespawnZoneRotatorSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Selection")]
        [Tooltip("How the next respawn anchor is chosen. " +
                 "RoundRobin cycles through all anchors in order; " +
                 "Random picks one at random each time.")]
        [SerializeField] private AnchorSelectionMode _selectionMode = AnchorSelectionMode.RoundRobin;

        [Header("Gizmo")]
        [Tooltip("Radius (in world units) drawn around each respawn anchor as a Gizmo. " +
                 "Visual only — does not constrain the spawn position.")]
        [SerializeField, Min(0f)] private float _anchorRadius = 1f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>How the next respawn anchor is chosen.</summary>
        public AnchorSelectionMode SelectionMode => _selectionMode;

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
                Debug.LogWarning($"[RespawnZoneRotatorSO] '{name}': _anchorRadius is 0 — " +
                                 "the Gizmo will be invisible in the Scene view.");
        }
#endif
    }
}

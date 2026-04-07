using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject describing the physical layout of a battle arena:
    /// ground dimensions, wall height, and the ordered list of robot spawn points.
    ///
    /// Spawn point data is stored here (not in the scene) so ArenaManager can read it
    /// without scene-side Transform lookups.  SpawnPointMarker MonoBehaviours in the
    /// scene mirror these values for authoring convenience and Gizmo visualisation.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Arena ▶ ArenaConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaConfig", order = 0)]
    public sealed class ArenaConfig : ScriptableObject
    {
        // ── Ground ────────────────────────────────────────────────────────────

        [Header("Ground")]
        [Tooltip("Width of the arena floor along the X axis (metres).")]
        [SerializeField, Min(1f)] private float _groundWidth = 20f;

        [Tooltip("Depth of the arena floor along the Z axis (metres).")]
        [SerializeField, Min(1f)] private float _groundDepth = 20f;

        // ── Walls ─────────────────────────────────────────────────────────────

        [Header("Walls")]
        [Tooltip("Height of the boundary walls (metres).")]
        [SerializeField, Min(0.5f)] private float _wallHeight = 3f;

        [Tooltip("Thickness of each boundary wall (metres).")]
        [SerializeField, Min(0.1f)] private float _wallThickness = 0.5f;

        // ── Spawn Points ──────────────────────────────────────────────────────

        [Header("Spawn Points")]
        [Tooltip("Ordered list of robot spawn positions/orientations. Index 0 = player, index 1+ = AI robots.")]
        [SerializeField] private List<SpawnPointData> _spawnPoints = new List<SpawnPointData>();

        // ── Public API ────────────────────────────────────────────────────────

        public float GroundWidth   => _groundWidth;
        public float GroundDepth   => _groundDepth;
        public float WallHeight    => _wallHeight;
        public float WallThickness => _wallThickness;

        /// <summary>Read-only ordered spawn point list. Never mutate at runtime.</summary>
        public IReadOnlyList<SpawnPointData> SpawnPoints => _spawnPoints;

        /// <summary>Zero-based arena identifier used in MatchRecord serialisation.</summary>
        [Header("Identity")]
        [SerializeField] private int _arenaIndex = 0;
        public int ArenaIndex => _arenaIndex;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_spawnPoints == null || _spawnPoints.Count < 2)
                Debug.LogWarning($"[ArenaConfig] '{name}': Arena should have at least 2 spawn points (player + one enemy).");
        }
#endif
    }

    // ── SpawnPointData ────────────────────────────────────────────────────────

    /// <summary>
    /// Serialisable value type that stores one spawn point's world-space
    /// position and orientation.  Kept as a plain struct so it survives
    /// domain reload without any MonoBehaviour/Transform dependency.
    /// </summary>
    [Serializable]
    public sealed class SpawnPointData
    {
        [Tooltip("World-space position where the robot will be placed.")]
        public Vector3 position;

        [Tooltip("Spawn orientation expressed as Euler angles (degrees).")]
        public Vector3 eulerAngles;

        [Tooltip("Human-readable label shown in Scene Gizmos, e.g. 'Player', 'Enemy1'.")]
        public string label = "Spawn";
    }
}

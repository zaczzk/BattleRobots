using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    // ── Spawn descriptor (plain-data; round-trips through JsonUtility) ────────

    /// <summary>
    /// Serialisable description of one spawn position inside an arena.
    /// Uses plain <see cref="Vector3"/> / <see cref="Quaternion"/> so it can be
    /// written into a <see cref="MatchRecord"/> without Unity-specific types.
    /// </summary>
    [Serializable]
    public sealed class SpawnDescriptor
    {
        [Tooltip("Team this descriptor belongs to (matches SpawnPoint.TeamIndex).")]
        public int teamIndex;

        [Tooltip("World-space spawn position. Set by the Arena designer.")]
        public Vector3 position;

        [Tooltip("World-space spawn rotation. Set by the Arena designer.")]
        public Quaternion rotation = Quaternion.identity;
    }

    // ── ArenaConfig SO ────────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject that describes a battle arena: its display name,
    /// spawn positions, and any per-arena tuning values.
    ///
    /// Designers author one ArenaConfig asset per arena and wire it to the
    /// MatchManager SO reference. MatchManager reads spawn positions from here;
    /// the actual scene uses <see cref="SpawnPoint"/> MonoBehaviours which should
    /// match these values.
    ///
    /// Assets are immutable at runtime — all fields exposed via read-only properties.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaConfig
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaConfig", order = 0)]
    public sealed class ArenaConfig : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Display name used on the arena select screen.")]
        [SerializeField] private string _arenaName = "Unnamed Arena";

        [Tooltip("Zero-based index saved into MatchRecord.arenaIndex for stats tracking.")]
        [SerializeField, Min(0)] private int _arenaIndex = 0;

        [Tooltip("Preview thumbnail for the arena select screen.")]
        [SerializeField] private Sprite _thumbnail;

        // ── Spawn Points ──────────────────────────────────────────────────────

        [Header("Spawn Points")]
        [Tooltip("One entry per team. MatchManager reads these to place robots. " +
                 "Order must match the SpawnPoint.TeamIndex values in the scene.")]
        [SerializeField] private List<SpawnDescriptor> _spawnPoints = new List<SpawnDescriptor>();

        // ── Arena Rules ───────────────────────────────────────────────────────

        [Header("Arena Rules")]
        [Tooltip("Round time limit in seconds. 0 = no limit.")]
        [SerializeField, Min(0f)] private float _timeLimitSeconds = 180f;

        [Tooltip("Currency bonus awarded to the winner beyond the base match reward.")]
        [SerializeField, Min(0)] private int _winBonusCurrency = 100;

        // ── Public API (read-only at runtime) ─────────────────────────────────

        public string ArenaName        => _arenaName;
        public int    ArenaIndex       => _arenaIndex;
        public Sprite Thumbnail        => _thumbnail;
        public float  TimeLimitSeconds => _timeLimitSeconds;
        public int    WinBonusCurrency => _winBonusCurrency;

        /// <summary>Read-only view of spawn descriptors.</summary>
        public IReadOnlyList<SpawnDescriptor> SpawnPoints => _spawnPoints;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the spawn descriptor for the given team index,
        /// or null if none is configured for that team.
        /// </summary>
        public SpawnDescriptor GetSpawnForTeam(int teamIndex)
        {
            foreach (SpawnDescriptor sp in _spawnPoints)
            {
                if (sp.teamIndex == teamIndex)
                    return sp;
            }
            return null;
        }

        /// <summary>
        /// Validates that at least two spawn points exist (one per team) and that
        /// they are not co-located. Returns true if the config is arena-ready.
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            if (_spawnPoints == null || _spawnPoints.Count < 2)
            {
                errorMessage = "ArenaConfig requires at least 2 spawn points (one per team).";
                return false;
            }

            // Check for accidental co-location (within 0.1 m).
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                for (int j = i + 1; j < _spawnPoints.Count; j++)
                {
                    if (Vector3.Distance(_spawnPoints[i].position, _spawnPoints[j].position) < 0.1f)
                    {
                        errorMessage = $"Spawn points [{i}] and [{j}] are co-located " +
                                       $"(distance < 0.1 m). Robots would overlap.";
                        return false;
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}

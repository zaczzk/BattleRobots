using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Serializable struct that describes the location and type of one power-up pickup
    /// within an arena scene.
    ///
    /// Used by <see cref="PowerUpSpawnConfig"/> to define the full roster of
    /// spawnable pickups for a given arena.  Each entry pairs a world-space
    /// <see cref="Position"/> / <see cref="EulerAngles"/> with the
    /// <see cref="PowerUpSO"/> asset that should appear there.
    /// </summary>
    [Serializable]
    public struct PowerUpSpawnPoint
    {
        /// <summary>World-space position of the pickup.</summary>
        public Vector3   Position;

        /// <summary>World-space rotation (Euler angles in degrees) of the pickup.</summary>
        public Vector3   EulerAngles;

        /// <summary>The <see cref="PowerUpSO"/> that governs this pickup's effect.</summary>
        public PowerUpSO PowerUp;
    }

    /// <summary>
    /// Scene-level configuration SO that lists every power-up spawn point in an arena
    /// and the shared respawn delay applied to all pickups in that arena.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • <see cref="SpawnPoints"/> is an immutable ordered list of
    ///     <see cref="PowerUpSpawnPoint"/> structs (position + PowerUpSO reference).
    ///     A <see cref="BattleRobots.Physics.PowerUpController"/> reads this list at
    ///     scene initialisation and instantiates one pickup per entry.
    ///   • <see cref="RespawnDelay"/> is the number of seconds before a collected
    ///     pickup re-activates (default 15 s; can be 0 for instant respawn).
    ///   • <c>OnValidate</c> logs a warning for any entry whose <c>PowerUp</c>
    ///     reference is null — null entries are skipped at runtime (not treated as
    ///     errors).
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace. No Physics or UI references.
    ///   This SO is immutable at runtime — only the scene wiring may differ per arena.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ PowerUpSpawnConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/PowerUpSpawnConfig", order = 31)]
    public sealed class PowerUpSpawnConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Spawn Points")]
        [Tooltip("Ordered list of power-up spawn points for this arena. " +
                 "Each entry pairs a world-space position with a PowerUpSO asset.")]
        [SerializeField] private List<PowerUpSpawnPoint> _spawnPoints = new List<PowerUpSpawnPoint>();

        [Header("Timing")]
        [Tooltip("Seconds before a collected pickup reappears. " +
                 "0 = instant; values < 0 are clamped to 0.")]
        [SerializeField, Min(0f)] private float _respawnDelay = 15f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Read-only ordered list of spawn points for this arena.
        /// Entries with a null <c>PowerUp</c> reference are skipped at runtime.
        /// </summary>
        public IReadOnlyList<PowerUpSpawnPoint> SpawnPoints => _spawnPoints;

        /// <summary>
        /// Seconds before a collected pickup becomes active again. Always ≥ 0.
        /// </summary>
        public float RespawnDelay => _respawnDelay;

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_spawnPoints == null) return;
            for (int i = 0; i < _spawnPoints.Count; i++)
            {
                if (_spawnPoints[i].PowerUp == null)
                    Debug.LogWarning(
                        $"[PowerUpSpawnConfig] SpawnPoint[{i}] has no PowerUpSO assigned.", this);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Manages arena-level setup: positions robot root GameObjects at the spawn
    /// points defined in an ArenaConfig SO when a match begins.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Assign <see cref="ArenaConfig"/> SO in the Inspector.
    ///   2. Populate <c>_robotRoots</c> with each robot's root GameObject
    ///      in spawn-slot order (index 0 = player, index 1+ = AI).
    ///   3. Add a <see cref="VoidGameEventListener"/> component to the same
    ///      GameObject.  Set its <c>Event</c> to the MatchStarted VoidGameEvent
    ///      SO and its <c>Response</c> to <c>ArenaManager.HandleMatchStarted()</c>.
    ///
    /// ── Spawn logic ───────────────────────────────────────────────────────────
    ///   Robot[i] → ArenaConfig.SpawnPoints[i].
    ///   Extra robots without a matching spawn point remain at their current
    ///   transform position; a warning is logged.
    ///   Robots at indices beyond the spawn-point list are ignored gracefully.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - No direct references to Physics or UI namespaces.
    ///   - No heap allocations in hot paths (Awake/Update/FixedUpdate).
    ///   - HandleMatchStarted is called via VoidGameEventListener SO channel;
    ///     ArenaManager itself has no Update logic.
    /// </summary>
    public sealed class ArenaManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("ArenaConfig SO that defines ground dimensions, wall height, and spawn points.")]
        [SerializeField] private ArenaConfig _arenaConfig;

        [Header("Robots")]
        [Tooltip("Root GameObjects of each combatant, in spawn-slot order (index 0 = player).")]
        [SerializeField] private List<GameObject> _robotRoots = new List<GameObject>();

        // ── Public API (called by VoidGameEventListener response) ─────────────

        /// <summary>
        /// Teleports each robot to its assigned spawn point.
        /// Wire this to a VoidGameEventListener → Response targeting the
        /// MatchStarted VoidGameEvent SO.
        /// </summary>
        public void HandleMatchStarted()
        {
            if (_arenaConfig == null)
            {
                Debug.LogError("[ArenaManager] ArenaConfig is not assigned — cannot spawn robots.");
                return;
            }

            var spawnPoints = _arenaConfig.SpawnPoints;

            if (_robotRoots.Count > spawnPoints.Count)
            {
                Debug.LogWarning(
                    $"[ArenaManager] {_robotRoots.Count} robots but only {spawnPoints.Count} spawn points. " +
                    "Extra robots will not be repositioned.");
            }

            int count = Mathf.Min(_robotRoots.Count, spawnPoints.Count);
            for (int i = 0; i < count; i++)
            {
                if (_robotRoots[i] == null)
                {
                    Debug.LogWarning($"[ArenaManager] Robot root at index {i} is null — skipping.");
                    continue;
                }

                SpawnPointData sp = spawnPoints[i];
                _robotRoots[i].transform.SetPositionAndRotation(
                    sp.position,
                    Quaternion.Euler(sp.eulerAngles));
            }

            Debug.Log($"[ArenaManager] Positioned {count} robot(s) at spawn points.");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_arenaConfig == null)
                Debug.LogWarning("[ArenaManager] ArenaConfig is not assigned.");

            if (_robotRoots == null || _robotRoots.Count == 0)
                Debug.LogWarning("[ArenaManager] No robot roots assigned.");
        }
#endif
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Resets arena hazard state to a clean baseline whenever a match starts, ensuring
    /// there are no stale active zones or group flags left over from a previous match
    /// (or from editor-time scene authoring).
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   On <see cref="HandleMatchStarted"/>:
    ///     1. Iterates <c>_hazards[]</c> and sets <c>IsActive = false</c> on each.
    ///     2. Iterates <c>_groups[]</c> and calls <c>Reset()</c> on each.
    ///        (<see cref="HazardZoneGroupSO.Reset"/> is silent — no events fired.)
    ///   Both arrays are optional and fully null-safe; null entries are skipped.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references HazardZoneGroupSO (Core) and
    ///     HazardZoneController (Physics).
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one reset controller per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_hazards</c>       → all HazardZoneControllers in the arena.
    ///   2. Assign <c>_groups</c>        → all HazardZoneGroupSOs used in the arena.
    ///   3. Assign <c>_onMatchStarted</c>→ shared match-start VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaHazardResetController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Hazards (optional)")]
        [Tooltip("All HazardZoneControllers in the arena. Each will have IsActive set " +
                 "to false when a match starts.")]
        [SerializeField] private HazardZoneController[] _hazards;

        [Header("Groups (optional)")]
        [Tooltip("All HazardZoneGroupSOs used in the arena. Reset() is called on each " +
                 "when a match starts (silent — no events fired).")]
        [SerializeField] private HazardZoneGroupSO[] _groups;

        [Header("Event Channel — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _resetDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _resetDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_resetDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_resetDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Deactivates all managed <see cref="HazardZoneController"/> instances and
        /// silently resets all managed <see cref="HazardZoneGroupSO"/> instances.
        /// Null entries in either array are skipped.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            if (_hazards != null)
            {
                foreach (HazardZoneController hazard in _hazards)
                {
                    if (hazard != null)
                        hazard.IsActive = false;
                }
            }

            if (_groups != null)
            {
                foreach (HazardZoneGroupSO group in _groups)
                {
                    group?.Reset();
                }
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The managed <see cref="HazardZoneController"/> array. May be null.</summary>
        public HazardZoneController[] Hazards => _hazards;

        /// <summary>The managed <see cref="HazardZoneGroupSO"/> array. May be null.</summary>
        public HazardZoneGroupSO[] Groups => _groups;
    }
}

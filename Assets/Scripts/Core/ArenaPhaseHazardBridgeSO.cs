using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Serializable entry that pairs a VoidGameEvent fired by an arena phase with
    /// the <see cref="HazardZoneGroupSO"/> that should become active when that phase begins.
    /// </summary>
    [Serializable]
    public struct ArenaPhaseHazardBridgeEntry
    {
        [Tooltip("The VoidGameEvent assigned to the arena phase (from ArenaPhaseControllerSO). " +
                 "When this event fires, the paired group will be activated.")]
        public VoidGameEvent phaseEvent;

        [Tooltip("The HazardZoneGroupSO to activate when this phase's event fires. " +
                 "All other groups in this config will be deactivated atomically.")]
        public HazardZoneGroupSO group;
    }

    /// <summary>
    /// Configuration ScriptableObject that wires each arena-phase event to a
    /// <see cref="HazardZoneGroupSO"/>. When a phase fires its event the bridge
    /// controller activates the matching group and deactivates every other group
    /// listed in this config.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • Pure config — no runtime state. All mutable state lives in the controller.
    ///   • <see cref="GetGroup(VoidGameEvent)"/> performs a linear scan and returns
    ///     the first matching group (null when no match).
    ///   • <see cref="EntryCount"/> exposes the number of bridge entries.
    ///   • OnValidate warns about null event or null group entries to aid authoring.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaPhaseHazardBridge.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaPhaseHazardBridge", order = 17)]
    public sealed class ArenaPhaseHazardBridgeSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bridge Entries")]
        [Tooltip("One entry per arena phase. Each entry maps a phase VoidGameEvent to " +
                 "the HazardZoneGroupSO that becomes exclusively active when that phase fires.")]
        [SerializeField] private ArenaPhaseHazardBridgeEntry[] _entries;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of bridge entries (including null-event / null-group slots).</summary>
        public int EntryCount => _entries?.Length ?? 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="HazardZoneGroupSO"/> whose <c>phaseEvent</c> matches
        /// <paramref name="phaseEvent"/>. Returns null when no match is found, the entries
        /// array is null, or <paramref name="phaseEvent"/> is null.
        /// Linear scan — for inspector-sized arrays this is negligible.
        /// </summary>
        public HazardZoneGroupSO GetGroup(VoidGameEvent phaseEvent)
        {
            if (phaseEvent == null || _entries == null) return null;

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].phaseEvent == phaseEvent)
                    return _entries[i].group;
            }

            return null;
        }

        /// <summary>
        /// Returns the <see cref="ArenaPhaseHazardBridgeEntry"/> at <paramref name="index"/>.
        /// Returns a default entry for a null entries array or an out-of-range index.
        /// </summary>
        public ArenaPhaseHazardBridgeEntry GetEntry(int index)
        {
            if (_entries == null || index < 0 || index >= _entries.Length)
                return default;

            return _entries[index];
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_entries == null || _entries.Length == 0)
            {
                Debug.LogWarning($"[ArenaPhaseHazardBridgeSO] '{name}': " +
                                 "_entries is null or empty — no phase-to-group bridges will fire.");
                return;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].phaseEvent == null)
                    Debug.LogWarning($"[ArenaPhaseHazardBridgeSO] '{name}': " +
                                     $"Entry [{i}] phaseEvent is null — this entry will never fire.");

                if (_entries[i].group == null)
                    Debug.LogWarning($"[ArenaPhaseHazardBridgeSO] '{name}': " +
                                     $"Entry [{i}] group is null — no group will activate for this phase.");
            }
        }
#endif
    }
}

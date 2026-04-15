using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Bridges the arena phase lifecycle to the hazard-group layer: when a phase's
    /// <see cref="VoidGameEvent"/> fires, this controller activates the paired
    /// <see cref="HazardZoneGroupSO"/> and deactivates every other group listed in
    /// the <see cref="ArenaPhaseHazardBridgeSO"/> config.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. OnEnable: for each entry in <c>_config</c>, subscribe to its
    ///      <c>phaseEvent</c> with a captured-index delegate that calls
    ///      <see cref="ActivateGroup(int)"/>.
    ///   2. <see cref="ActivateGroup(int entryIndex)"/>:
    ///        a. Looks up the target group via <c>_config.GetEntry(entryIndex).group</c>.
    ///        b. Iterates all entries and calls <c>Deactivate()</c> on every group
    ///           that is NOT the target.
    ///        c. Calls <c>Activate()</c> on the target group.
    ///        d. All operations are null-safe.
    ///   3. OnDisable: unsubscribes all cached delegates.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics or UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates are pre-allocated per entry in Awake to avoid closure allocations
    ///     in OnEnable.
    ///   - DisallowMultipleComponent — one bridge per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_config</c> → an ArenaPhaseHazardBridgeSO asset.
    ///   2. Wire each entry's <c>phaseEvent</c> to the matching ArenaPhaseControllerSO
    ///      phase's VoidGameEvent.
    ///   3. Wire each entry's <c>group</c> to the HazardZoneGroupSO that should become
    ///      active for that phase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaPhaseHazardBridgeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("SO that defines phase-event → HazardZoneGroup bindings.")]
        [SerializeField] private ArenaPhaseHazardBridgeSO _config;

        // ── Cached delegates ──────────────────────────────────────────────────

        // One Action per entry index; allocated in Awake so OnEnable/OnDisable are
        // zero-alloc.
        private Action[] _entryDelegates;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            RebuildDelegates();
        }

        private void OnEnable()
        {
            if (_config == null) return;

            for (int i = 0; i < _config.EntryCount; i++)
            {
                VoidGameEvent evt = _config.GetEntry(i).phaseEvent;
                if (evt != null && _entryDelegates != null && i < _entryDelegates.Length)
                    evt.RegisterCallback(_entryDelegates[i]);
            }
        }

        private void OnDisable()
        {
            if (_config == null) return;

            for (int i = 0; i < _config.EntryCount; i++)
            {
                VoidGameEvent evt = _config.GetEntry(i).phaseEvent;
                if (evt != null && _entryDelegates != null && i < _entryDelegates.Length)
                    evt.UnregisterCallback(_entryDelegates[i]);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Activates the group at <paramref name="entryIndex"/> in the config and
        /// deactivates all other groups listed in the config.
        /// No-op when <c>_config</c> is null or <paramref name="entryIndex"/> is
        /// out of range.
        /// </summary>
        public void ActivateGroup(int entryIndex)
        {
            if (_config == null) return;

            HazardZoneGroupSO targetGroup = _config.GetEntry(entryIndex).group;

            // Deactivate every other group first, then activate the target.
            for (int i = 0; i < _config.EntryCount; i++)
            {
                HazardZoneGroupSO g = _config.GetEntry(i).group;
                if (g == null) continue;

                if (g != targetGroup)
                    g.Deactivate();
            }

            targetGroup?.Activate();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ArenaPhaseHazardBridgeSO"/>. May be null.</summary>
        public ArenaPhaseHazardBridgeSO Config => _config;

        /// <summary>Total number of bridge entries in <see cref="Config"/>. 0 when config is null.</summary>
        public int EntryCount => _config?.EntryCount ?? 0;

        // ── Private helpers ───────────────────────────────────────────────────

        private void RebuildDelegates()
        {
            if (_config == null || _config.EntryCount == 0)
            {
                _entryDelegates = Array.Empty<Action>();
                return;
            }

            _entryDelegates = new Action[_config.EntryCount];
            for (int i = 0; i < _config.EntryCount; i++)
            {
                int capturedIndex = i;
                _entryDelegates[capturedIndex] = () => ActivateGroup(capturedIndex);
            }
        }
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Bridges a <see cref="HazardZoneGroupSO"/> to an array of
    /// <see cref="HazardZoneController"/> instances, toggling all of them atomically
    /// whenever the group's event channels fire.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • OnEnable subscribes <see cref="_group"/>.<see cref="HazardZoneGroupSO.OnGroupActivated"/>
    ///     → <see cref="HandleActivate"/> and <see cref="HazardZoneGroupSO.OnGroupDeactivated"/>
    ///     → <see cref="HandleDeactivate"/>.
    ///   • <see cref="HandleActivate"/> sets every <see cref="HazardZoneController.IsActive"/>
    ///     in <see cref="_hazards"/> to true.
    ///   • <see cref="HandleDeactivate"/> sets them all to false.
    ///   • <see cref="Activate"/> / <see cref="Deactivate"/> / <see cref="Toggle"/> are
    ///     convenience pass-throughs to the SO; null-safe.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references HazardZoneController.
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake — zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one group controller per managed group.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_group</c> → a HazardZoneGroupSO asset.
    ///   2. Assign <c>_hazards</c> → all HazardZoneControllers in this arena section.
    ///   3. Wire the SO's event channels to any external trigger (e.g. match timer,
    ///      VoidGameEventListener) that should activate / deactivate the section.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardZoneGroupController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Group SO that owns IsGroupActive state and event channels.")]
        [SerializeField] private HazardZoneGroupSO _group;

        [Header("Hazards (optional)")]
        [Tooltip("All HazardZoneControllers belonging to this group. " +
                 "Toggled atomically when the group activates or deactivates.")]
        [SerializeField] private HazardZoneController[] _hazards;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _activateDelegate;
        private Action _deactivateDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _activateDelegate   = HandleActivate;
            _deactivateDelegate = HandleDeactivate;
        }

        private void OnEnable()
        {
            if (_group == null) return;
            _group.OnGroupActivated?.RegisterCallback(_activateDelegate);
            _group.OnGroupDeactivated?.RegisterCallback(_deactivateDelegate);
        }

        private void OnDisable()
        {
            if (_group == null) return;
            _group.OnGroupActivated?.UnregisterCallback(_activateDelegate);
            _group.OnGroupDeactivated?.UnregisterCallback(_deactivateDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets all managed <see cref="HazardZoneController"/> instances to active.
        /// Wired to <see cref="HazardZoneGroupSO.OnGroupActivated"/>.
        /// Null-safe: skips null entries in <see cref="_hazards"/>.
        /// </summary>
        public void HandleActivate()
        {
            if (_hazards == null) return;
            foreach (HazardZoneController hazard in _hazards)
            {
                if (hazard != null)
                    hazard.IsActive = true;
            }
        }

        /// <summary>
        /// Sets all managed <see cref="HazardZoneController"/> instances to inactive.
        /// Wired to <see cref="HazardZoneGroupSO.OnGroupDeactivated"/>.
        /// Null-safe: skips null entries in <see cref="_hazards"/>.
        /// </summary>
        public void HandleDeactivate()
        {
            if (_hazards == null) return;
            foreach (HazardZoneController hazard in _hazards)
            {
                if (hazard != null)
                    hazard.IsActive = false;
            }
        }

        /// <summary>Convenience pass-through — calls <see cref="HazardZoneGroupSO.Activate"/>.</summary>
        public void Activate() => _group?.Activate();

        /// <summary>Convenience pass-through — calls <see cref="HazardZoneGroupSO.Deactivate"/>.</summary>
        public void Deactivate() => _group?.Deactivate();

        /// <summary>Convenience pass-through — calls <see cref="HazardZoneGroupSO.Toggle"/>.</summary>
        public void Toggle() => _group?.Toggle();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="HazardZoneGroupSO"/>. May be null.</summary>
        public HazardZoneGroupSO Group => _group;

        /// <summary>
        /// Reflects <see cref="HazardZoneGroupSO.IsGroupActive"/>; false when <see cref="_group"/> is null.
        /// </summary>
        public bool IsGroupActive => _group != null && _group.IsGroupActive;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Applies stacked flat damage bonuses from a <see cref="StaggeredWeaponBonusCatalogSO"/>
    /// to <see cref="WeaponAttachmentController"/> at match start, then resets them at match end.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   Subscribes to <c>_onMatchStarted</c> → calls <see cref="Apply"/> once per match.
    ///   Subscribes to <c>_onMatchEnded</c>   → calls <see cref="ResetBonus"/> to prevent
    ///   stacking across consecutive matches.
    ///
    ///   Apply():
    ///   <list type="bullet">
    ///     <item>No-op when <c>_catalog</c> or <c>_weaponController</c> is null.</item>
    ///     <item>Iterates every non-null <see cref="WeaponCombatStatsBonusSO"/> entry in
    ///       <c>_catalog.Bonuses</c>.</item>
    ///     <item>For each entry whose <see cref="WeaponCombatStatsBonusSO.RequiredWeaponType"/>
    ///       matches <c>_weaponController.CurrentDamageType</c>, calls
    ///       <see cref="WeaponAttachmentController.AddDamageBonus"/> with the entry's
    ///       <see cref="WeaponCombatStatsBonusSO.FlatDamageBonus"/>.</item>
    ///     <item>Multiple matching entries stack additively on the same weapon.</item>
    ///   </list>
    ///
    ///   ResetBonus():
    ///   <list type="bullet">
    ///     <item>Calls <see cref="WeaponAttachmentController.ResetDamageBonus"/> to clear
    ///       all accumulated runtime bonuses.</item>
    ///     <item>No-op when <c>_weaponController</c> is null.</item>
    ///   </list>
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Create a <c>StaggeredWeaponBonusCatalog</c> asset and populate its bonus list.
    ///   2. Add this MB to the player robot root (or any convenient GO).
    ///   3. Assign <c>_catalog</c> → the catalog asset.
    ///   4. Assign <c>_weaponController</c> → the robot's WeaponAttachmentController.
    ///   5. Assign <c>_onMatchStarted</c> → the shared match-start VoidGameEvent.
    ///   6. Assign <c>_onMatchEnded</c>   → the shared match-end VoidGameEvent.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — may reference WeaponAttachmentController.
    ///   • No Rigidbody — ArticulationBody only.
    ///   • Action delegates cached in Awake — zero heap alloc in OnEnable / OnDisable.
    ///   • DisallowMultipleComponent — one staggered applier per robot.
    ///   • Apply() and ResetBonus() are public for direct EditMode testing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StaggeredWeaponBonusApplier : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog of bonus entries to stack. Leave null to disable this applier.")]
        [SerializeField] private StaggeredWeaponBonusCatalogSO _catalog;

        [Header("Target")]
        [Tooltip("WeaponAttachmentController on the player robot. " +
                 "Receives AddDamageBonus for each matching catalog entry.")]
        [SerializeField] private WeaponAttachmentController _weaponController;

        [Header("Event Channels — In")]
        [Tooltip("Fired by MatchManager at match start. Triggers Apply(). " +
                 "Leave null to suppress automatic application.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Fired by MatchManager at match end. Triggers ResetBonus() so the " +
                 "stacked bonuses do not carry into the next match. " +
                 "Leave null to suppress automatic reset.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _applyDelegate;
        private Action _resetDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = Apply;
            _resetDelegate = ResetBonus;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_applyDelegate);
            _onMatchEnded?.RegisterCallback(_resetDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_applyDelegate);
            _onMatchEnded?.UnregisterCallback(_resetDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Iterates every entry in the catalog and adds each matching flat damage bonus
        /// to the weapon controller.  Multiple matching entries stack additively.
        ///
        /// No-op when <c>_catalog</c> or <c>_weaponController</c> is null.
        /// Null catalog entries are silently skipped.
        /// Allocation-free — property reads and arithmetic only.
        /// </summary>
        public void Apply()
        {
            if (_catalog == null || _weaponController == null) return;

            DamageType equippedType = _weaponController.CurrentDamageType;
            IReadOnlyList<WeaponCombatStatsBonusSO> bonuses = _catalog.Bonuses;

            for (int i = 0; i < bonuses.Count; i++)
            {
                WeaponCombatStatsBonusSO bonus = bonuses[i];
                if (bonus == null) continue;
                if (bonus.RequiredWeaponType != equippedType) continue;

                _weaponController.AddDamageBonus(bonus.FlatDamageBonus);
            }
        }

        /// <summary>
        /// Resets the runtime damage bonus on the weapon controller back to zero.
        /// Call at match end to prevent stacked bonuses from carrying into the next match.
        /// No-op when <c>_weaponController</c> is null.
        /// </summary>
        public void ResetBonus()
        {
            _weaponController?.ResetDamageBonus();
        }

        // ── Public properties (for testing) ───────────────────────────────────

        /// <summary>The assigned bonus catalog. May be null.</summary>
        public StaggeredWeaponBonusCatalogSO Catalog => _catalog;

        /// <summary>The assigned weapon controller. May be null.</summary>
        public WeaponAttachmentController WeaponController => _weaponController;
    }
}

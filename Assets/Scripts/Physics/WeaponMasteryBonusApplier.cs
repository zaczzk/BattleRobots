using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Applies a mastery-gated flat damage bonus to <see cref="WeaponAttachmentController"/>
    /// when the equipped weapon's <see cref="DamageType"/> has been mastered according to
    /// <see cref="DamageTypeMasterySO"/>.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   Subscribes to <c>_onMatchStarted</c> → calls <see cref="Apply"/> once per match.
    ///   Subscribes to <c>_onMatchEnded</c>   → calls <see cref="ResetBonus"/> to prevent
    ///   stacking across consecutive matches.
    ///
    ///   <see cref="Apply"/>:
    ///   <list type="bullet">
    ///     <item>No-op when <c>_bonusSO</c>, <c>_mastery</c>, or <c>_weaponController</c> is null.</item>
    ///     <item>No-op when the equipped weapon type is NOT mastered.</item>
    ///     <item>Otherwise calls <see cref="WeaponAttachmentController.AddDamageBonus"/>
    ///       with <see cref="WeaponMasteryBonusSO.FlatDamageBonus"/>.</item>
    ///   </list>
    ///
    /// ── Key difference from WeaponCombatStatsBonusApplier ───────────────────
    ///   WeaponCombatStatsBonusApplier gates on a fixed required type.
    ///   This applier gates on runtime mastery state — any mastered type qualifies.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the player robot root (or any convenient GO).
    ///   2. Assign <c>_bonusSO</c>          → a WeaponMasteryBonusSO asset.
    ///   3. Assign <c>_mastery</c>           → shared DamageTypeMasterySO.
    ///   4. Assign <c>_weaponController</c>  → the robot's WeaponAttachmentController.
    ///   5. Assign <c>_onMatchStarted</c>    → the shared match-start VoidGameEvent.
    ///   6. Assign <c>_onMatchEnded</c>      → the shared match-end VoidGameEvent.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — may reference WeaponAttachmentController.
    ///   • No Rigidbody — ArticulationBody only.
    ///   • Action delegates cached in Awake — zero heap alloc in OnEnable / OnDisable.
    ///   • DisallowMultipleComponent — one applier per robot.
    ///   • <see cref="Apply"/> and <see cref="ResetBonus"/> are public for EditMode testing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponMasteryBonusApplier : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Bonus definition SO. Leave null to disable this applier.")]
        [SerializeField] private WeaponMasteryBonusSO _bonusSO;

        [Header("Mastery Source")]
        [Tooltip("Runtime mastery SO used to check whether the equipped type is mastered. " +
                 "Leave null to disable (bonus never applies).")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        [Header("Target")]
        [Tooltip("WeaponAttachmentController on the player robot. " +
                 "Receives AddDamageBonus when the equipped weapon type is mastered.")]
        [SerializeField] private WeaponAttachmentController _weaponController;

        [Header("Event Channels — In")]
        [Tooltip("Fired by MatchManager at match start. Triggers Apply(). " +
                 "Leave null to suppress automatic application.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Fired by MatchManager at match end. Triggers ResetBonus() so the " +
                 "bonus does not stack into the next match. " +
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
        /// Checks whether the equipped weapon type is mastered and, if so, adds the flat
        /// damage bonus to the weapon controller.
        ///
        /// No-op when <c>_bonusSO</c>, <c>_mastery</c>, or <c>_weaponController</c> is null,
        /// or when the equipped weapon type is not mastered.
        /// Allocation-free — property reads and one arithmetic operation only.
        /// </summary>
        public void Apply()
        {
            if (_bonusSO == null || _mastery == null || _weaponController == null) return;
            if (!_mastery.IsTypeMastered(_weaponController.CurrentDamageType)) return;

            _weaponController.AddDamageBonus(_bonusSO.FlatDamageBonus);
        }

        /// <summary>
        /// Resets the runtime damage bonus on the weapon controller back to zero.
        /// Call at match end to prevent bonuses from stacking across matches.
        /// No-op when <c>_weaponController</c> is null.
        /// </summary>
        public void ResetBonus()
        {
            _weaponController?.ResetDamageBonus();
        }

        // ── Public properties (for testing) ───────────────────────────────────

        /// <summary>The assigned bonus config SO. May be null.</summary>
        public WeaponMasteryBonusSO BonusSO => _bonusSO;

        /// <summary>The assigned mastery SO. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;

        /// <summary>The assigned weapon controller. May be null.</summary>
        public WeaponAttachmentController WeaponController => _weaponController;
    }
}

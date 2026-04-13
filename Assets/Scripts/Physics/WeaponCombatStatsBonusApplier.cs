using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Applies a data-driven flat damage bonus to <see cref="WeaponAttachmentController"/>
    /// when the equipped weapon's <see cref="DamageType"/> matches the bonus config.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   Subscribes to <c>_onMatchStarted</c> → calls <see cref="Apply"/> once per match.
    ///   Subscribes to <c>_onMatchEnded</c>   → calls <see cref="ResetBonus"/> to prevent
    ///   stacking across consecutive matches.
    ///
    ///   Apply():
    ///   <list type="bullet">
    ///     <item>No-op when <c>_bonusConfig</c> or <c>_weaponController</c> is null.</item>
    ///     <item>No-op when <c>_weaponController.CurrentDamageType</c> does not match
    ///       <c>_bonusConfig.RequiredWeaponType</c>.</item>
    ///     <item>Otherwise calls <see cref="WeaponAttachmentController.AddDamageBonus"/>
    ///       with <see cref="WeaponCombatStatsBonusSO.FlatDamageBonus"/>.</item>
    ///   </list>
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the player robot root (or any convenient GO).
    ///   2. Assign <c>_bonusConfig</c> → a WeaponCombatStatsBonusSO asset.
    ///   3. Assign <c>_weaponController</c> → the robot's WeaponAttachmentController.
    ///   4. Assign <c>_onMatchStarted</c> → the shared match-start VoidGameEvent.
    ///   5. Assign <c>_onMatchEnded</c>   → the shared match-end VoidGameEvent.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — may reference WeaponAttachmentController.
    ///   • No Rigidbody — ArticulationBody only.
    ///   • Action delegates cached in Awake — zero heap alloc in OnEnable / OnDisable.
    ///   • DisallowMultipleComponent — one applier per robot.
    ///   • Apply() and ResetBonus() are public for direct EditMode testing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponCombatStatsBonusApplier : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Bonus definition. Leave null to disable this applier.")]
        [SerializeField] private WeaponCombatStatsBonusSO _bonusConfig;

        [Header("Target")]
        [Tooltip("WeaponAttachmentController on the player robot. " +
                 "Receives AddDamageBonus when the weapon type matches.")]
        [SerializeField] private WeaponAttachmentController _weaponController;

        [Header("Event Channels — In")]
        [Tooltip("Fired by MatchManager at match start. Triggers Apply(). " +
                 "Leave null to suppress automatic application (call Apply() manually).")]
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
        /// Evaluates whether the equipped weapon type matches the config's required type
        /// and, if so, adds the flat damage bonus to the weapon controller.
        ///
        /// No-op when <c>_bonusConfig</c> or <c>_weaponController</c> is null,
        /// or when the weapon's current damage type does not match the config's
        /// <see cref="WeaponCombatStatsBonusSO.RequiredWeaponType"/>.
        /// Allocation-free — property reads and one arithmetic operation only.
        /// </summary>
        public void Apply()
        {
            if (_bonusConfig == null || _weaponController == null) return;
            if (_weaponController.CurrentDamageType != _bonusConfig.RequiredWeaponType) return;

            _weaponController.AddDamageBonus(_bonusConfig.FlatDamageBonus);
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

        /// <summary>The assigned bonus config. May be null.</summary>
        public WeaponCombatStatsBonusSO BonusConfig => _bonusConfig;

        /// <summary>The assigned weapon controller. May be null.</summary>
        public WeaponAttachmentController WeaponController => _weaponController;
    }
}

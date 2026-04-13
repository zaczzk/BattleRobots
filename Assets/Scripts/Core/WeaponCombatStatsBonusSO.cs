using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable data asset that defines a flat damage bonus granted to the player's
    /// weapon when its <see cref="DamageType"/> matches <see cref="RequiredWeaponType"/>.
    ///
    /// ── Design ────────────────────────────────────────────────────────────────
    ///   Applied once at match start by <see cref="BattleRobots.Physics.WeaponCombatStatsBonusApplier"/>.
    ///   The bonus is cleared at match end so it does not stack across matches.
    ///   Modelled on the <see cref="PassivePartEffectSO"/> pattern — data-only SO;
    ///   all runtime logic lives in the MonoBehaviour applier.
    ///
    /// ── Use cases ─────────────────────────────────────────────────────────────
    ///   • Grant +20 flat damage when the player equips a Thermal weapon.
    ///   • Give an Energy-specialist build a hidden damage edge.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • SO asset is immutable at runtime — all fields are read-only properties.
    ///   • Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ WeaponCombatStatsBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/WeaponCombatStatsBonus")]
    public sealed class WeaponCombatStatsBonusSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Trigger")]
        [Tooltip("The equipped weapon must deal this DamageType for the bonus to apply.")]
        [SerializeField] private DamageType _requiredWeaponType = DamageType.Physical;

        [Header("Bonus")]
        [Tooltip("Flat damage added to WeaponAttachmentController on each fire when the " +
                 "weapon type matches. Must be ≥ 0.")]
        [SerializeField, Min(0f)] private float _flatDamageBonus = 10f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// The weapon <see cref="DamageType"/> that must be equipped for the bonus to apply.
        /// </summary>
        public DamageType RequiredWeaponType => _requiredWeaponType;

        /// <summary>
        /// Flat damage bonus added per hit. Always ≥ 0.
        /// </summary>
        public float FlatDamageBonus => _flatDamageBonus;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_flatDamageBonus == 0f)
                Debug.LogWarning(
                    $"[WeaponCombatStatsBonusSO] '{name}' has a FlatDamageBonus of 0 — " +
                    "this bonus will have no gameplay impact.", this);
        }
#endif
    }
}
